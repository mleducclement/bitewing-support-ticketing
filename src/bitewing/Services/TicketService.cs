using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.EntityFrameworkCore;

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

        if (ticket.Status is TicketStatus.InProgress or TicketStatus.Blocked)
            throw new TicketAlreadyClaimedException(ticket.Id);

        if (!TicketTransitions.IsLegal(ticket.Status, TicketStatus.InProgress))
            throw new InvalidTicketTransitionException(ticket.Status, TicketStatus.InProgress);

        var previousStatus = ticket.Status;
        ticket.Status = TicketStatus.InProgress;
        ticket.AssigneeId = agentId;
        ticket.HandoffFlag = false;
        ticket.UpdatedAt = DateTime.UtcNow;

        AddEvent(ticket.Id, agentId, TicketEventType.StatusChanged, null, ticket.UpdatedAt, previousStatus, ticket.Status);

        await _db.SaveChangesAsync();
        await _db.Entry(ticket).Reference(t => t.Assignee).LoadAsync();

        return ticket;
    }

    public async Task<Ticket?> ReleaseAsync(Guid ticketId, string agentId, bool isTeamLead)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket is null) return null;

        if (!TicketTransitions.IsLegal(ticket.Status, TicketStatus.Open))
            throw new InvalidTicketTransitionException(ticket.Status, TicketStatus.Open);

        EnsureOwnerOrTeamLead(ticket, agentId, isTeamLead);

        var previousStatus = ticket.Status;
        ticket.Status = TicketStatus.Open;
        ticket.AssigneeId = null;
        ticket.HandoffFlag = true;
        ticket.BlockedSince = null;
        ticket.UpdatedAt = DateTime.UtcNow;

        AddEvent(ticket.Id, agentId, TicketEventType.StatusChanged, null, ticket.UpdatedAt, previousStatus, ticket.Status);

        await _db.SaveChangesAsync();
        await _db.Entry(ticket).Reference(t => t.Assignee).LoadAsync();

        return ticket;
    }

    public async Task<Ticket?> BlockAsync(Guid ticketId, string agentId, bool isTeamLead)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket is null) return null;

        if (!TicketTransitions.IsLegal(ticket.Status, TicketStatus.Blocked))
            throw new InvalidTicketTransitionException(ticket.Status, TicketStatus.Blocked);

        EnsureOwnerOrTeamLead(ticket, agentId, isTeamLead);

        var previousStatus = ticket.Status;
        ticket.Status = TicketStatus.Blocked;
        ticket.BlockedSince = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        AddEvent(ticket.Id, agentId, TicketEventType.StatusChanged, null, ticket.UpdatedAt, previousStatus, ticket.Status);

        await _db.SaveChangesAsync();
        await _db.Entry(ticket).Reference(t => t.Assignee).LoadAsync();

        return ticket;
    }

    public async Task<Ticket?> UnblockAsync(Guid ticketId, string agentId, bool isTeamLead)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket is null) return null;

        if (ticket.Status != TicketStatus.Blocked)
            throw new InvalidTicketTransitionException(ticket.Status, TicketStatus.InProgress);

        EnsureOwnerOrTeamLead(ticket, agentId, isTeamLead);

        var previousStatus = ticket.Status;
        ticket.Status = TicketStatus.InProgress;
        ticket.BlockedSince = null;
        ticket.UpdatedAt = DateTime.UtcNow;

        AddEvent(ticket.Id, agentId, TicketEventType.StatusChanged, null, ticket.UpdatedAt, previousStatus, ticket.Status);

        await _db.SaveChangesAsync();
        await _db.Entry(ticket).Reference(t => t.Assignee).LoadAsync();

        return ticket;
    }

    public async Task<Ticket?> ResolveAsync(Guid ticketId, string agentId, bool isTeamLead)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket is null) return null;

        if (!TicketTransitions.IsLegal(ticket.Status, TicketStatus.Resolved))
            throw new InvalidTicketTransitionException(ticket.Status, TicketStatus.Resolved);

        EnsureOwnerOrTeamLead(ticket, agentId, isTeamLead);

        var previousStatus = ticket.Status;
        ticket.Status = TicketStatus.Resolved;
        ticket.UpdatedAt = DateTime.UtcNow;

        AddEvent(ticket.Id, agentId, TicketEventType.StatusChanged, null, ticket.UpdatedAt, previousStatus, ticket.Status);

        await _db.SaveChangesAsync();
        await _db.Entry(ticket).Reference(t => t.Assignee).LoadAsync();

        return ticket;
    }

    public async Task<Ticket?> CancelAsync(Guid ticketId, string agentId, bool isTeamLead, CancellationReason reason)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket is null) return null;

        if (!TicketTransitions.IsLegal(ticket.Status, TicketStatus.Cancelled))
            throw new InvalidTicketTransitionException(ticket.Status, TicketStatus.Cancelled);

        if (ticket.Status != TicketStatus.Open)
            EnsureOwnerOrTeamLead(ticket, agentId, isTeamLead);

        var previousStatus = ticket.Status;
        ticket.Status = TicketStatus.Cancelled;
        ticket.CancellationReason = reason;
        ticket.BlockedSince = null;
        ticket.HandoffFlag = false;
        ticket.UpdatedAt = DateTime.UtcNow;

        AddEvent(ticket.Id, agentId, TicketEventType.StatusChanged, reason.ToString(), ticket.UpdatedAt, previousStatus, ticket.Status);

        await _db.SaveChangesAsync();
        await _db.Entry(ticket).Reference(t => t.Assignee).LoadAsync();

        return ticket;
    }

    public async Task<int> AutoCancelExpiredTicketsAsync()
    {
        var settings = await _db.Settings.FirstAsync();
        var threshold = DateTime.UtcNow.AddDays(-settings.BlockedExpiryDays);

        var expiredTickets = await _db.Tickets
            .Where(t => t.Status == TicketStatus.Blocked && t.BlockedSince != null && t.BlockedSince < threshold)
            .ToListAsync();

        foreach (var ticket in expiredTickets)
        {
            var previousStatus = ticket.Status;
            ticket.Status = TicketStatus.Cancelled;
            ticket.CancellationReason = CancellationReason.Expired;
            ticket.BlockedSince = null;
            ticket.HandoffFlag = false;
            ticket.UpdatedAt = DateTime.UtcNow;

            AddEvent(ticket.Id, null, TicketEventType.StatusChanged, CancellationReason.Expired.ToString(), ticket.UpdatedAt, previousStatus, ticket.Status);
        }

        if (expiredTickets.Count > 0)
            await _db.SaveChangesAsync();

        return expiredTickets.Count;
    }

    public async Task<List<Ticket>> GetQueueAsync(TicketQueueRequest filter)
    {
        var query = _db.Tickets.Include(t => t.Assignee).AsQueryable();

        if (filter.Status is not null)
            query = query.Where(t => t.Status == filter.Status);

        if (filter.Priority is not null)
            query = query.Where(t => t.Priority == filter.Priority);

        if (filter.AssigneeId is not null)
            query = query.Where(t => t.AssigneeId == filter.AssigneeId);

        if (filter.HandoffFlag is not null)
            query = query.Where(t => t.HandoffFlag == filter.HandoffFlag);

        return await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();
    }

    private static void EnsureOwnerOrTeamLead(Ticket ticket, string agentId, bool isTeamLead)
    {
        if (!isTeamLead && ticket.AssigneeId != agentId)
            throw new UnauthorizedTicketActionException(ticket.Id);
    }

    private void AddEvent(Guid ticketId, string? actorId, TicketEventType eventType, string? reason, DateTime occurredAt,
        TicketStatus? fromStatus = null, TicketStatus? toStatus = null)
    {
        _db.TicketEvents.Add(new TicketEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ActorId = actorId,
            EventType = eventType,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Reason = reason,
            OccurredAt = occurredAt
        });
    }
}