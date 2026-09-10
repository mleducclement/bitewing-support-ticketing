using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using bitewing.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

// Slice 2: the worker that drains classification_jobs. Uses a fake IClassifier
// instead of DI's real ClaudeClassifier so these tests need no network call and
// no API key, per the IClassifier seam described in the decision log.
public class ClassificationWorkerTests : TicketsTestBase
{
    public ClassificationWorkerTests(TicketsApiFactory factory) : base(factory)
    {
    }

    private class FakeClassifier : IClassifier
    {
        private readonly Func<ClassificationResult> _behavior;

        private FakeClassifier(Func<ClassificationResult> behavior)
        {
            _behavior = behavior;
        }

        public static FakeClassifier Succeeding(TicketArea area = TicketArea.Claims,
            TicketType type = TicketType.Broken) =>
            new(() => new ClassificationResult(area, type, "test-prompt", "test-model"));

        public static FakeClassifier Failing(string message = "model unavailable") =>
            new(() => throw new ClassificationFailedException(message));

        public Task<ClassificationResult> ClassifyTicket(string title, string body,
            CancellationToken cancellationToken = default) => Task.FromResult(_behavior());
    }

    private async Task<Guid> CreateTicketAsync(string subject)
    {
        var request = new CreateTicketRequest("Maple Dental", "Jordan Reyes", "jordan@mapledental.example",
            subject, "Body text for " + subject);
        var response = await Client.PostAsJsonAsync("/api/tickets", request);
        var body = await response.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);
        return body!.Id;
    }

    private async Task<ClassificationJob> JobForAsync(AppDbContext db, Guid ticketId) =>
        await db.ClassificationJobs.SingleAsync(j => j.TicketId == ticketId);

    [Fact]
    public async Task ProcessPendingJobs_Success_WritesClassificationAndMarksSucceeded()
    {
        var ticketId = await CreateTicketAsync();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new ClassificationService(db, FakeClassifier.Succeeding(TicketArea.Reminders, TicketType.HowTo));

        // Not asserting the exact processed count: the shared test container may
        // still hold Pending jobs left Pending by earlier tests in this class.
        await service.ProcessPendingJobsAsync();

        var job = await JobForAsync(db, ticketId);
        Assert.Equal(ClassificationJobStatus.Succeeded, job.Status);

        var classification = await db.Classifications.SingleAsync(c => c.TicketId == ticketId);
        Assert.Equal(TicketArea.Reminders, classification.Area);
        Assert.Equal(ClassificationSource.Model, classification.AreaSource);
        Assert.Equal(TicketType.HowTo, classification.Type);
        Assert.Equal(ClassificationSource.Model, classification.TypeSource);
        Assert.Equal("test-prompt", classification.PromptVersion);
        Assert.Equal("test-model", classification.ModelName);
        Assert.NotNull(classification.ClassifiedAt);
    }

    [Fact]
    public async Task ProcessPendingJobs_Failure_IncrementsAttemptCountAndStaysPending()
    {
        var ticketId = await CreateTicketAsync();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new ClassificationService(db, FakeClassifier.Failing("timeout"));

        await service.ProcessPendingJobsAsync();

        var job = await JobForAsync(db, ticketId);
        Assert.Equal(ClassificationJobStatus.Pending, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal("timeout", job.LastError);
        Assert.False(await db.Classifications.AnyAsync(c => c.TicketId == ticketId));
    }

    [Fact]
    public async Task ProcessPendingJobs_ThirdFailure_MarksFailed()
    {
        var ticketId = await CreateTicketAsync();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new ClassificationService(db, FakeClassifier.Failing());

        await service.ProcessPendingJobsAsync();
        await service.ProcessPendingJobsAsync();
        await service.ProcessPendingJobsAsync();

        var job = await JobForAsync(db, ticketId);
        Assert.Equal(ClassificationJobStatus.Failed, job.Status);
        Assert.Equal(3, job.AttemptCount);
    }

    [Fact]
    public async Task ProcessPendingJobs_OneFailingJob_DoesNotBlockAnother()
    {
        var failingTicketId = await CreateTicketAsync("FAIL-ME");
        var succeedingTicketId = await CreateTicketAsync("classify me fine");

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Behavior keyed off the ticket's own subject rather than call order,
        // since ProcessPendingJobsAsync doesn't guarantee processing order.
        var byTitle = new TitleKeyedClassifier(title => title == "FAIL-ME"
            ? throw new ClassificationFailedException("boom")
            : new ClassificationResult(TicketArea.Other, TicketType.FeatureRequest, "test-prompt", "test-model"));

        var service = new ClassificationService(db, byTitle);

        // Not asserting the exact processed count: the shared test container may
        // still hold Pending jobs left Pending by earlier tests in this class.
        await service.ProcessPendingJobsAsync();

        var failingJob = await JobForAsync(db, failingTicketId);
        var succeedingJob = await JobForAsync(db, succeedingTicketId);
        Assert.Equal(ClassificationJobStatus.Pending, failingJob.Status);
        Assert.Equal(1, failingJob.AttemptCount);
        Assert.Equal(ClassificationJobStatus.Succeeded, succeedingJob.Status);
    }

    private class TitleKeyedClassifier : IClassifier
    {
        private readonly Func<string, ClassificationResult> _behavior;

        public TitleKeyedClassifier(Func<string, ClassificationResult> behavior)
        {
            _behavior = behavior;
        }

        public Task<ClassificationResult> ClassifyTicket(string title, string body,
            CancellationToken cancellationToken = default) => Task.FromResult(_behavior(title));
    }
}
