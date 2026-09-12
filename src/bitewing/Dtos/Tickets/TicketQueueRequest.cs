using bitewing.Data;

namespace bitewing.Dtos.Tickets;

public record TicketQueueRequest(
    TicketStatus? Status,
    TicketPriority? Priority,
    string? AssigneeId,
    bool? HandoffFlag,
    TicketArea? Area,
    // When true and Status is unset, includes Resolved/Cancelled tickets.
    // Ignored when Status is set, since an explicit status is already unambiguous.
    bool? AllStatuses,
    // Filters to tickets with no assignee. Ignored when AssigneeId is set.
    bool? Unassigned
    );