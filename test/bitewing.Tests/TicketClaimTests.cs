using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

public class TicketClaimTests : TicketsTestBase
{
    public TicketClaimTests(TicketsApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Claim_OpenTicket_AssignsAgentAndSetsInProgress()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(body);
        Assert.Equal(TicketStatus.InProgress, body!.Status);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var agent = await userManager.FindByEmailAsync(SeededAgentEmail);

        var persisted = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(persisted);
        Assert.Equal(TicketStatus.InProgress, persisted!.Status);
        Assert.Equal(agent!.Id, persisted.AssigneeId);
        Assert.False(persisted.HandoffFlag);

        var events = await db.TicketEvents.Where(e => e.TicketId == ticketId).ToListAsync();
        var claimEvent = Assert.Single(events);
        Assert.Equal(TicketEventType.StatusChanged, claimEvent.EventType);
        Assert.Equal(agent.Id, claimEvent.ActorId);
        Assert.Null(claimEvent.Reason);
    }

    [Fact]
    public async Task Claim_WithoutAuthentication_ReturnsUnauthorized()
    {
        var ticketId = await CreateTicketAsync();

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Claim_AlreadyClaimedTicket_ReturnsConflict()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var first = await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Claim_NonexistentTicket_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsync($"/api/tickets/{Guid.NewGuid()}/claim", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}