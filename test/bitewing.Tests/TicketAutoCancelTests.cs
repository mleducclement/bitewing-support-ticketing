using bitewing.Data;
using bitewing.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

public class TicketAutoCancelTests : TicketsTestBase
{
    public TicketAutoCancelTests(TicketsApiFactory factory) : base(factory)
    {
    }

    private async Task<Guid> CreateBlockedTicketAsync(TimeSpan blockedAge)
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ticket = await db.Tickets.FindAsync(ticketId);
        ticket!.Status = TicketStatus.Blocked;
        ticket.BlockedSince = DateTime.UtcNow - blockedAge;
        await db.SaveChangesAsync();

        return ticketId;
    }

    [Fact]
    public async Task AutoCancel_ExpiredBlockedTicket_CancelsWithExpiredReason()
    {
        var ticketId = await CreateBlockedTicketAsync(TimeSpan.FromDays(15));

        using var scope = Factory.Services.CreateScope();
        var ticketService = scope.ServiceProvider.GetRequiredService<ITicketService>();
        var cancelledCount = await ticketService.AutoCancelExpiredTicketsAsync();

        Assert.Equal(1, cancelledCount);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(persisted);
        Assert.Equal(TicketStatus.Cancelled, persisted!.Status);
        Assert.Equal(CancellationReason.Expired, persisted.CancellationReason);
        Assert.Null(persisted.BlockedSince);
        Assert.False(persisted.HandoffFlag);

        var events = await db.TicketEvents.Where(e => e.TicketId == ticketId).OrderBy(e => e.OccurredAt).ToListAsync();
        var autoCancelEvent = events.Last();
        Assert.Null(autoCancelEvent.ActorId);
        Assert.Equal(TicketStatus.Blocked, autoCancelEvent.FromStatus);
        Assert.Equal(TicketStatus.Cancelled, autoCancelEvent.ToStatus);
        Assert.Equal("Expired", autoCancelEvent.Reason);
    }

    [Fact]
    public async Task AutoCancel_RecentlyBlockedTicket_NotCancelled()
    {
        var ticketId = await CreateBlockedTicketAsync(TimeSpan.FromDays(1));

        using var scope = Factory.Services.CreateScope();
        var ticketService = scope.ServiceProvider.GetRequiredService<ITicketService>();
        await ticketService.AutoCancelExpiredTicketsAsync();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(persisted);
        Assert.Equal(TicketStatus.Blocked, persisted!.Status);
    }

    [Fact]
    public async Task AutoCancel_NonBlockedTicket_NotCancelled()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        using var scope = Factory.Services.CreateScope();
        var ticketService = scope.ServiceProvider.GetRequiredService<ITicketService>();
        await ticketService.AutoCancelExpiredTicketsAsync();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(persisted);
        Assert.Equal(TicketStatus.InProgress, persisted!.Status);
    }

    [Fact]
    public async Task AutoCancel_MultipleExpiredTickets_CancelsAll()
    {
        var firstTicketId = await CreateBlockedTicketAsync(TimeSpan.FromDays(20));
        var secondTicketId = await CreateBlockedTicketAsync(TimeSpan.FromDays(15));

        using var scope = Factory.Services.CreateScope();
        var ticketService = scope.ServiceProvider.GetRequiredService<ITicketService>();
        var cancelledCount = await ticketService.AutoCancelExpiredTicketsAsync();

        Assert.Equal(2, cancelledCount);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var first = await db.Tickets.FindAsync(firstTicketId);
        var second = await db.Tickets.FindAsync(secondTicketId);
        Assert.Equal(TicketStatus.Cancelled, first!.Status);
        Assert.Equal(TicketStatus.Cancelled, second!.Status);
    }
}
