namespace bitewing.Services;

public class UnauthorizedTicketActionException : Exception
{
    public Guid TicketId { get; }

    public UnauthorizedTicketActionException(Guid ticketId)
        : base($"Agent is not authorized to act on ticket {ticketId}.")
    {
        TicketId = ticketId;
    }
}
