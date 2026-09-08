using bitewing.Data;

namespace bitewing.Services;

public interface IClassifier
{
    Task<ClassificationResult> ClassifyTicket(string title, string body, CancellationToken cancellationToken = default);
}