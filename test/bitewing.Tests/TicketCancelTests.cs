using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

public class TicketCancelTests : TicketsTestBase
{
    public TicketCancelTests(TicketsApiFactory factory) : base(factory)
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
    public async Task Cancel_OpenTicket_Succeeds()
    {
        var ticketId = await CreateTicketAsync();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ticket = await db.Tickets.FindAsync(ticketId);
            ticket!.HandoffFlag = true;
            await db.SaveChangesAsync();
        }

        await LoginAsSeededAgentAsync();
        var response = await Client.PostAsJsonAsync($"/api/tickets/{ticketId}/cancel", new CancelTicketRequest(CancellationReason.Duplicate));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope2 = Factory.Services.CreateScope();
        var verifyDb = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope2.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var agent = await userManager.FindByEmailAsync(SeededAgentEmail);

        var persisted = await verifyDb.Tickets.FindAsync(ticketId);
        Assert.NotNull(persisted);
        Assert.Equal(TicketStatus.Cancelled, persisted!.Status);
        Assert.Equal(CancellationReason.Duplicate, persisted.CancellationReason);
        Assert.False(persisted.HandoffFlag);

        var events = await verifyDb.TicketEvents.Where(e => e.TicketId == ticketId).OrderBy(e => e.OccurredAt).ToListAsync();
        var cancelEvent = Assert.Single(events);
        Assert.Equal(agent!.Id, cancelEvent.ActorId);
        Assert.Equal(TicketStatus.Open, cancelEvent.FromStatus);
        Assert.Equal(TicketStatus.Cancelled, cancelEvent.ToStatus);
        Assert.Equal("Duplicate", cancelEvent.Reason);
    }

    [Fact]
    public async Task Cancel_InProgressTicket_ByOwner_Succeeds()
    {
        var ticketId = await CreateInProgressTicketAsync();

        var response = await Client.PostAsJsonAsync($"/api/tickets/{ticketId}/cancel", new CancelTicketRequest(CancellationReason.Withdrawn));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(TicketStatus.Cancelled, body!.Status);
        Assert.Equal(CancellationReason.Withdrawn, body.CancellationReason);
    }

    [Fact]
    public async Task Cancel_InProgressTicket_NotOwner_ReturnsForbidden()
    {
        var ticketId = await CreateInProgressTicketAsync();

        await LoginAsync(SeededSecondAgentEmail);
        var response = await Client.PostAsJsonAsync($"/api/tickets/{ticketId}/cancel", new CancelTicketRequest(CancellationReason.Withdrawn));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_BlockedTicket_ByTeamLead_Succeeds()
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

        await LoginAsync(SeededLeadEmail);
        var response = await Client.PostAsJsonAsync($"/api/tickets/{ticketId}/cancel", new CancelTicketRequest(CancellationReason.Spam));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope2 = Factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db2.Tickets.FindAsync(ticketId);
        Assert.NotNull(persisted);
        Assert.Equal(TicketStatus.Cancelled, persisted!.Status);
        Assert.Null(persisted.BlockedSince);
    }

    [Fact]
    public async Task Cancel_WithExpiredReason_ReturnsBadRequest()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsJsonAsync($"/api/tickets/{ticketId}/cancel", new CancelTicketRequest(CancellationReason.Expired));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_WithoutReason_ReturnsBadRequest()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsJsonAsync($"/api/tickets/{ticketId}/cancel", new CancelTicketRequest(null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_WithoutAuthentication_ReturnsUnauthorized()
    {
        var ticketId = await CreateTicketAsync();

        var response = await Client.PostAsJsonAsync($"/api/tickets/{ticketId}/cancel", new CancelTicketRequest(CancellationReason.Duplicate));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ResolvedTicket_ReturnsConflict()
    {
        var ticketId = await CreateInProgressTicketAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/resolve", null);

        var response = await Client.PostAsJsonAsync($"/api/tickets/{ticketId}/cancel", new CancelTicketRequest(CancellationReason.Duplicate));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_NonexistentTicket_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsJsonAsync($"/api/tickets/{Guid.NewGuid()}/cancel", new CancelTicketRequest(CancellationReason.Duplicate));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}