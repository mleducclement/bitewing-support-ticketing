namespace bitewing.Data;

public class SpotCheck
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public string AgentId { get; set; } = string.Empty;
    public ApplicationUser Agent { get; set; } = null!;

    public bool AreaConfirmed { get; set; }
    public bool TypeConfirmed { get; set; }

    public DateTime RespondedAt { get; set; }
}