using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

public class TicketUnblockTests : TicketsTestBase
{
    public TicketUnblockTests(TicketsApiFactory factory) : base(factory)
    {
    }

    private async Task<Guid> CreateBlockedTicketAsync()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ticket = await db.Tickets.FindAsync(ticketId);
        ticket!.Status = TicketStatus.Blocked;
        ticket.BlockedSince = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ticketId;
    }

    [Fact]
    public async Task Unblock_BlockedTicket_SetsInProgressAndClearsBlockedSince()
    {
        var ticketId = await CreateBlockedTicketAsync();

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/unblock", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(body);
        Assert.Equal(TicketStatus.InProgress, body!.Status);
        Assert.Null(body.BlockedSince);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var agent = await userManager.FindByEmailAsync(SeededAgentEmail);

        var persisted = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(persisted);
        Assert.Equal(TicketStatus.InProgress, persisted!.Status);
        Assert.Null(persisted.BlockedSince);

        var events = await db.TicketEvents.Where(e => e.TicketId == ticketId).OrderBy(e => e.OccurredAt).ToListAsync();
        var unblockEvent = events.Last();
        Assert.Equal(TicketEventType.StatusChanged, unblockEvent.EventType);
        Assert.Equal(agent!.Id, unblockEvent.ActorId);
        Assert.Equal(TicketStatus.Blocked, unblockEvent.FromStatus);
        Assert.Equal(TicketStatus.InProgress, unblockEvent.ToStatus);
        Assert.Null(unblockEvent.Reason);
    }

    [Fact]
    public async Task Unblock_NotOwner_ReturnsForbidden()
    {
        var ticketId = await CreateBlockedTicketAsync();

        await LoginAsync(SeededSecondAgentEmail);
        var response = await Client.PostAsync($"/api/tickets/{ticketId}/unblock", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unblock_ByTeamLead_Succeeds()
    {
        var ticketId = await CreateBlockedTicketAsync();

        await LoginAsync(SeededLeadEmail);
        var response = await Client.PostAsync($"/api/tickets/{ticketId}/unblock", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unblock_WithoutAuthentication_ReturnsUnauthorized()
    {
        var ticketId = await CreateTicketAsync();

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/unblock", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unblock_OpenTicket_ReturnsConflict()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/unblock", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Unblock_NonexistentTicket_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsync($"/api/tickets/{Guid.NewGuid()}/unblock", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
