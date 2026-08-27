using bitewing.Data;
using bitewing.Dtos.Tickets;

namespace bitewing.Services;

public interface ITicketService
{
    Task<Ticket> CreateAsync(CreateTicketRequest request);
    Task<Ticket?> ClaimAsync(Guid ticketId, string agentId);
}