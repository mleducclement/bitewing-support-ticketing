using bitewing.Data;

namespace bitewing.Dtos.Tickets;

public record TicketQueueRequest(TicketStatus? Status, TicketPriority? Priority, string? AssigneeId, bool? HandoffFlag);