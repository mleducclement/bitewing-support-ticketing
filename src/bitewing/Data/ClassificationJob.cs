namespace bitewing.Data;

public class ClassificationJob
{
    public Guid Id { get; set; }
    
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    
    public ClassificationJobStatus Status { get; set; } = ClassificationJobStatus.Pending;
    public int AttemptCount { get; set; } = 0;
    public string? LastError { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}