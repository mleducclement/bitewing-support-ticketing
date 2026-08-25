namespace bitewing.Data;

public class Classification
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    // Nullable: an unclassified ticket is a valid state, not an error.
    public TicketArea? Area { get; set; }
    public ClassificationSource? AreaSource { get; set; }

    public TicketType? Type { get; set; }
    public ClassificationSource? TypeSource { get; set; }

    // Which prompt produced the model's answer. Null until a model call succeeds.
    public string? PromptVersion { get; set; }
    public string? ModelName { get; set; }

    public DateTime? ClassifiedAt { get; set; }
}