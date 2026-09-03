using bitewing.Data;

namespace bitewing.Dtos.Tickets;

public record TicketEventResponse(
    Guid Id,
    string? ActorName,
    TicketEventType EventType,
    TicketStatus? FromStatus,
    TicketStatus? ToStatus,
    TicketPriority? FromPriority,
    TicketPriority? ToPriority,
    string? Reason,
    DateTime OccurredAt
);