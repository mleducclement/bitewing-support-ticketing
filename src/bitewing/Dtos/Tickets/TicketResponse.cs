using bitewing.Data;

namespace bitewing.Dtos.Tickets;

public record TicketResponse(
    Guid Id,
    string DisplayId,
    string Subject,
    string Body,
    string CustomerName,
    string CustomerEmail,
    string ClinicName,
    TicketStatus Status,
    TicketPriority Priority,
    DateTime CreatedAt,
    string? AssigneeName,
    bool HandoffFlag,
    DateTime? BlockedSince,
    CancellationReason? CancellationReason
);