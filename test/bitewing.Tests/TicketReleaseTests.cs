using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

public class TicketReleaseTests : TicketsTestBase
{
    public TicketReleaseTests(TicketsApiFactory factory) : base(factory)
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
    public async Task Release_InProgressTicket_ClearsAssigneeAndSetsOpen()
    {
        var ticketId = await CreateInProgressTicketAsync();

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/release", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(body);
        Assert.Equal(TicketStatus.Open, body!.Status);
        Assert.True(body.HandoffFlag);
        Assert.Null(body.AssigneeName);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var agent = await userManager.FindByEmailAsync(SeededAgentEmail);

        var persisted = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(persisted);
        Assert.Equal(TicketStatus.Open, persisted!.Status);
        Assert.Null(persisted.AssigneeId);
        Assert.True(persisted.HandoffFlag);
        Assert.Null(persisted.BlockedSince);

        var events = await db.TicketEvents.Where(e => e.TicketId == ticketId).OrderBy(e => e.OccurredAt).ToListAsync();
        Assert.Equal(2, events.Count);
        var releaseEvent = events.Last();
        Assert.Equal(TicketEventType.StatusChanged, releaseEvent.EventType);
        Assert.Equal(agent!.Id, releaseEvent.ActorId);
        Assert.Equal(TicketStatus.InProgress, releaseEvent.FromStatus);
        Assert.Equal(TicketStatus.Open, releaseEvent.ToStatus);
        Assert.Null(releaseEvent.Reason);
    }

    [Fact]
    public async Task Release_BlockedTicket_ClearsAssigneeBlockedSinceAndSetsOpen()
    {
        var ticketId = await CreateInProgressTicketAsync();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ticket = await db.Tickets.FindAsync(ticketId);
            ticket!.Status = TicketStatus.Blocked;
            ticket.BlockedSince = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/release", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(body);
        Assert.Equal(TicketStatus.Open, body!.Status);
        Assert.True(body.HandoffFlag);

        using var verifyScope = Factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await verifyDb.Tickets.FindAsync(ticketId);
        Assert.NotNull(persisted);
        Assert.Equal(TicketStatus.Open, persisted!.Status);
        Assert.Null(persisted.AssigneeId);
        Assert.True(persisted.HandoffFlag);
        Assert.Null(persisted.BlockedSince);

        var events = await verifyDb.TicketEvents.Where(e => e.TicketId == ticketId).OrderBy(e => e.OccurredAt).ToListAsync();
        var releaseEvent = events.Last();
        Assert.Equal(TicketStatus.Blocked, releaseEvent.FromStatus);
        Assert.Equal(TicketStatus.Open, releaseEvent.ToStatus);
    }

    [Fact]
    public async Task Release_NotOwner_ReturnsForbidden()
    {
        var ticketId = await CreateInProgressTicketAsync();

        await LoginAsync(SeededSecondAgentEmail);
        var response = await Client.PostAsync($"/api/tickets/{ticketId}/release", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Release_ByTeamLead_Succeeds()
    {
        var ticketId = await CreateInProgressTicketAsync();

        await LoginAsync(SeededLeadEmail);
        var response = await Client.PostAsync($"/api/tickets/{ticketId}/release", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Release_WithoutAuthentication_ReturnsUnauthorized()
    {
        var ticketId = await CreateTicketAsync();

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/release", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Release_OpenTicket_ReturnsConflict()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsync($"/api/tickets/{ticketId}/release", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Release_NonexistentTicket_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsync($"/api/tickets/{Guid.NewGuid()}/release", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}