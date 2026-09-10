using bitewing.Data;
using Microsoft.EntityFrameworkCore;

namespace bitewing.Services;

public class ClassificationService : IClassificationService
{
    // Spec §6: retried a bounded number of times (3 at most), no backoff.
    private const int MaxAttempts = 3;

    private readonly AppDbContext _db;
    private readonly IClassifier _classifier;

    public ClassificationService(AppDbContext db, IClassifier classifier)
    {
        _db = db;
        _classifier = classifier;
    }

    public async Task<int> ProcessPendingJobsAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _db.ClassificationJobs
            .Include(j => j.Ticket)
            .Where(j => j.Status == ClassificationJobStatus.Pending)
            .ToListAsync(cancellationToken);

        var processed = 0;

        foreach (var job in jobs)
        {
            await ProcessJobAsync(job, cancellationToken);
            processed++;
        }

        return processed;
    }

    private async Task ProcessJobAsync(ClassificationJob job, CancellationToken cancellationToken)
    {
        var ticket = job.Ticket!;
        var now = DateTime.UtcNow;

        try
        {
            var result = await _classifier.ClassifyTicket(ticket.Subject, ticket.Body, cancellationToken);

            var classification = await _db.Classifications.FirstOrDefaultAsync(c => c.TicketId == ticket.Id,
                cancellationToken);
            if (classification is null)
            {
                classification = new Classification { Id = Guid.NewGuid(), TicketId = ticket.Id };
                _db.Classifications.Add(classification);
            }

            classification.Area = result.Area;
            classification.AreaSource = ClassificationSource.Model;
            classification.Type = result.Type;
            classification.TypeSource = ClassificationSource.Model;
            classification.PromptVersion = result.PromptVersion;
            classification.ModelName = result.ModelName;
            classification.ClassifiedAt = now;

            job.Status = ClassificationJobStatus.Succeeded;
            job.UpdatedAt = now;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            job.AttemptCount++;
            job.LastError = ex.Message;
            job.UpdatedAt = now;
            job.Status = job.AttemptCount >= MaxAttempts ? ClassificationJobStatus.Failed : ClassificationJobStatus.Pending;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}