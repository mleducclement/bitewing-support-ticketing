using bitewing.Data;
using bitewing.Dtos.Tickets;

namespace bitewing.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _db;

    public TicketService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Ticket> CreateAsync(CreateTicketRequest request)
    {
        var now = DateTime.UtcNow;

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Subject = request.Subject,
            Body = request.Body,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            ClinicName = request.ClinicName,
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        return ticket;
    }

    public async Task<Ticket?> ClaimAsync(Guid ticketId, string agentId)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket is null) return null;

        if (!TicketTransitions.IsLegal(ticket.Status, TicketStatus.InProgress))
            throw new InvalidTicketTransitionException(ticket.Status, TicketStatus.InProgress);

        ticket.Status = TicketStatus.InProgress;
        ticket.AssigneeId = agentId;
        ticket.HandoffFlag = false;
        ticket.UpdatedAt = DateTime.UtcNow;

        AddEvent(ticket.Id, agentId, TicketEventType.StatusChanged, null, ticket.UpdatedAt);

        await _db.SaveChangesAsync();

        return ticket;
    }

    private void AddEvent(Guid ticketId, string? actorId, TicketEventType eventType, string? reason, DateTime occurredAt)
    {
        _db.TicketEvents.Add(new TicketEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ActorId = actorId,
            EventType = eventType,
            Reason = reason,
            OccurredAt = occurredAt
        });
    }
}