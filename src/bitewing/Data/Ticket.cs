namespace bitewing.Data;

public class Ticket
{
    public Guid Id { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    // Null when Status is Open; required for InProgress/Blocked.
    public string? AssigneeId { get; set; }
    public ApplicationUser? Assignee { get; set; }

    // Set on release (InProgress/Blocked -> Open), cleared on claim.
    public bool HandoffFlag { get; set; }

    // Set when entering Blocked, cleared on leaving it. Drives auto-cancel.
    public DateTime? BlockedSince { get; set; }

    public CancellationReason? CancellationReason { get; set; }

    // Points at the prior ticket when a customer returns after Resolved/Cancelled.
    public Guid? RelatedTicketId { get; set; }
    public Ticket? RelatedTicket { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Classification? Classification { get; set; }
    public SpotCheck? SpotCheck { get; set; }
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<TicketEvent> Events { get; set; } = new List<TicketEvent>();
}