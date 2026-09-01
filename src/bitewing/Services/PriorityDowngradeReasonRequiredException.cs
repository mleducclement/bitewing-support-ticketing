using bitewing.Data;

namespace bitewing.Services;

public class PriorityDowngradeReasonRequiredException : Exception
{
    public TicketPriority From { get; }
    public TicketPriority To { get; }
    
    public PriorityDowngradeReasonRequiredException(TicketPriority from, TicketPriority to):
        base($"Cannot change priority from {from} to {to} without a reason")
    {
        From = from;
        To = to;
    }
}