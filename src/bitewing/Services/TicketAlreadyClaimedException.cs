namespace bitewing.Services;

public class TicketAlreadyClaimedException : Exception
{
    public Guid TicketId { get; }

    public TicketAlreadyClaimedException(Guid ticketId)
        : base("This ticket has already been claimed.")
    {
        TicketId = ticketId;
    }
}