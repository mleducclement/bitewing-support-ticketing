using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

public class TicketResolveTests : TicketsTestBase
{
    public TicketResolveTests(TicketsApiFactory factory) : base(factory)
    {
    }

    private async Task<Guid> CreateInProgressTicketAsync()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        return ticketId;
    }

    [Fact]
    public async Task Resolve_InProgressTicket_SetsResolved()
    {
        var ticketId = await CreateInProgressTicketAsync();

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/resolve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.Equal(TicketStatus.Resolved, body!.Status);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var agent = await userManager.FindByEmailAsync(SeededAgentEmail);

        var persisted = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(persisted);
        Assert.Equal(TicketStatus.Resolved, persisted!.Status);

        var events = await db.TicketEvents.Where(e => e.TicketId == ticketId).OrderBy(e => e.OccurredAt).ToListAsync();
        Assert.Equal(2, events.Count);
        var resolveEvent = events.Last();
        Assert.Equal(TicketEventType.StatusChanged, resolveEvent.EventType);
        Assert.Equal(agent!.Id, resolveEvent.ActorId);
        Assert.Equal(TicketStatus.InProgress, resolveEvent.FromStatus);
        Assert.Equal(TicketStatus.Resolved, resolveEvent.ToStatus);
        Assert.Null(resolveEvent.Reason);
    }

    [Fact]
    public async Task Resolve_NotOwner_ReturnsForbidden()
    {
        var ticketId = await CreateInProgressTicketAsync();

        await LoginAsync(SeededSecondAgentEmail);
        var response = await Client.PostAsync($"/api/tickets/{ticketId}/resolve", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_ByTeamLead_Succeeds()
    {
        var ticketId = await CreateInProgressTicketAsync();

        await LoginAsync(SeededLeadEmail);
        var response = await Client.PostAsync($"/api/tickets/{ticketId}/resolve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_WithoutAuthentication_ReturnsUnauthorized()
    {
        var ticketId = await CreateTicketAsync();

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/resolve", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_OpenTicket_ReturnsConflict()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/resolve", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_NonexistentTicket_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsync($"/api/tickets/{Guid.NewGuid()}/resolve", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}