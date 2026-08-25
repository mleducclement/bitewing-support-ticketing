namespace bitewing.Data;

// Append-only history: status changes, assignment changes, priority changes,
// and classification corrections. ActorId is null for system-triggered events
// (e.g. automatic expiry cancellation).
public class TicketEvent
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public string? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }

    public TicketEventType EventType { get; set; }

    // Required by the spec for priority downgrades; optional for other event types.
    public string? Reason { get; set; }

    public DateTime OccurredAt { get; set; }
}