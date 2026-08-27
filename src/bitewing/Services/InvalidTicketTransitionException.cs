using bitewing.Data;

namespace bitewing.Services;

public class InvalidTicketTransitionException : Exception
{
    public TicketStatus From { get; }
    public TicketStatus To { get; }

    public InvalidTicketTransitionException(TicketStatus from, TicketStatus to)
        : base($"Cannot transition a ticket from {from} to {to}.")
    {
        From = from;
        To = to;
    }
}
