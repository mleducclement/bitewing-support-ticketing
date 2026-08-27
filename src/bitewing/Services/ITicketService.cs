using bitewing.Data;
using bitewing.Dtos.Tickets;

namespace bitewing.Services;

public interface ITicketService
{
    Task<Ticket> CreateAsync(CreateTicketRequest request);
    Task<Ticket?> ClaimAsync(Guid ticketId, string agentId);
    Task<Ticket?> ReleaseAsync(Guid ticketId, string agentId, bool isTeamLead);
    Task<Ticket?> BlockAsync(Guid ticketId, string agentId, bool isTeamLead);
    Task<Ticket?> UnblockAsync(Guid ticketId, string agentId, bool isTeamLead);
    Task<Ticket?> ResolveAsync(Guid ticketId, string agentId, bool isTeamLead);
    Task<Ticket?> CancelAsync(Guid ticketId, string agentId, bool isTeamLead, CancellationReason reason);
    Task<List<Ticket>> GetQueueAsync(TicketQueueRequest filter);
}