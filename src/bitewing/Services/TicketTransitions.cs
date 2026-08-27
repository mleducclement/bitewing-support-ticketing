using bitewing.Data;

namespace bitewing.Services;

// Canonical legal transitions from spec §3. Nothing should move a ticket
// between statuses without checking this table first.
public static class TicketTransitions
{
    private static readonly HashSet<(TicketStatus From, TicketStatus To)> Legal = new()
    {
        (TicketStatus.Open, TicketStatus.InProgress),
        (TicketStatus.Open, TicketStatus.Cancelled),
        (TicketStatus.InProgress, TicketStatus.Blocked),
        (TicketStatus.InProgress, TicketStatus.Open),
        (TicketStatus.InProgress, TicketStatus.Resolved),
        (TicketStatus.InProgress, TicketStatus.Cancelled),
        (TicketStatus.Blocked, TicketStatus.InProgress),
        (TicketStatus.Blocked, TicketStatus.Open),
        (TicketStatus.Blocked, TicketStatus.Cancelled),
    };

    public static bool IsLegal(TicketStatus from, TicketStatus to) => Legal.Contains((from, to));
}
