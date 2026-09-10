namespace bitewing.Services;

public interface IClassificationService
{
    Task<int> ProcessPendingJobsAsync(CancellationToken cancellationToken = default);
}