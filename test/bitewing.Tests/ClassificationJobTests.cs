using bitewing.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

// Slice 1: creating a ticket enqueues a classification work-table row. No worker
// runs during tests (TicketsApiFactory strips hosted services), so jobs stay Pending.
public class ClassificationJobTests : TicketsTestBase
{
    public ClassificationJobTests(TicketsApiFactory factory) : base(factory)
    {
    }

    private async Task<List<ClassificationJob>> JobsForAsync(Guid ticketId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ClassificationJobs.Where(j => j.TicketId == ticketId).ToListAsync();
    }

    [Fact]
    public async Task Create_EnqueuesExactlyOnePendingJob()
    {
        var ticketId = await CreateTicketAsync();

        var job = Assert.Single(await JobsForAsync(ticketId));
        Assert.Equal(ClassificationJobStatus.Pending, job.Status);
        Assert.Equal(0, job.AttemptCount);
        Assert.Null(job.LastError);
    }

    [Fact]
    public async Task Create_TwoTickets_EachGetsItsOwnJob()
    {
        var firstId = await CreateTicketAsync();
        var secondId = await CreateTicketAsync();

        Assert.Single(await JobsForAsync(firstId));
        Assert.Single(await JobsForAsync(secondId));
    }

    [Fact]
    public async Task Create_DoesNotWriteClassificationRow()
    {
        var ticketId = await CreateTicketAsync();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Classifications.AnyAsync(c => c.TicketId == ticketId));
    }
}