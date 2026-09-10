namespace bitewing.Services;

public class ClassificationFailedException : Exception
{
    public ClassificationFailedException(string message) : base(message)
    {
    }

    public ClassificationFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}