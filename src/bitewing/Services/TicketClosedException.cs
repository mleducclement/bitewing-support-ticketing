using bitewing.Data;

namespace bitewing.Services;

public class TicketClosedException : Exception
{
    public TicketClosedException(TicketStatus status) : base($"Cannot change priority. ticket is already {status}") { }
}