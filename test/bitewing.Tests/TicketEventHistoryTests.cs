using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using bitewing.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

public class TicketEventHistoryTests : TicketsTestBase
{
    public TicketEventHistoryTests(TicketsApiFactory factory) : base(factory)
    {
    }

    private async Task<List<TicketEventResponse>> GetEventsAsync(Guid ticketId)
    {
        var response = await Client.GetAsync($"/api/tickets/{ticketId}/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<List<TicketEventResponse>>(TestJson.Options))!;
    }

    [Fact]
    public async Task GetEvents_NewTicket_ReturnsEmptyList()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        Assert.Empty(await GetEventsAsync(ticketId));
    }

    [Fact]
    public async Task GetEvents_AfterClaimThenBlock_ReturnsBothStatusEventsNewestFirst()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);
        await Client.PostAsync($"/api/tickets/{ticketId}/block", null);

        var events = await GetEventsAsync(ticketId);

        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(TicketEventType.StatusChanged, e.EventType));
        Assert.All(events, e => Assert.Equal("Sarah Kerrigan", e.ActorName));

        Assert.Equal(TicketStatus.InProgress, events[0].FromStatus);
        Assert.Equal(TicketStatus.Blocked, events[0].ToStatus);
        Assert.Equal(TicketStatus.Open, events[1].FromStatus);
        Assert.Equal(TicketStatus.InProgress, events[1].ToStatus);
    }

    [Fact]
    public async Task GetEvents_PriorityChange_ReturnsFromToAndActor()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/priority",
            new PriorityChangeRequest(TicketPriority.Urgent, null));

        var priorityEvent = Assert.Single(await GetEventsAsync(ticketId));
        Assert.Equal(TicketEventType.PriorityChanged, priorityEvent.EventType);
        Assert.Equal(TicketPriority.Normal, priorityEvent.FromPriority);
        Assert.Equal(TicketPriority.Urgent, priorityEvent.ToPriority);
        Assert.Equal("Sarah Kerrigan", priorityEvent.ActorName);
    }

    [Fact]
    public async Task GetEvents_CancelWithReason_EventCarriesReason()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/cancel",
            new CancelTicketRequest(CancellationReason.Duplicate));

        var cancelEvent = Assert.Single(await GetEventsAsync(ticketId));
        Assert.Equal(TicketStatus.Open, cancelEvent.FromStatus);
        Assert.Equal(TicketStatus.Cancelled, cancelEvent.ToStatus);
        Assert.Equal("Duplicate", cancelEvent.Reason);
    }

    [Fact]
    public async Task GetEvents_SystemAutoCancel_ActorNameIsNull()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ticket = await db.Tickets.FindAsync(ticketId);
            ticket!.Status = TicketStatus.Blocked;
            ticket.BlockedSince = DateTime.UtcNow - TimeSpan.FromDays(30);
            await db.SaveChangesAsync();

            var ticketService = scope.ServiceProvider.GetRequiredService<ITicketService>();
            await ticketService.AutoCancelExpiredTicketsAsync();
        }

        var events = await GetEventsAsync(ticketId);

        // Newest-first: [0] is the system auto-cancel, [1] the agent's claim.
        Assert.Equal(TicketStatus.Cancelled, events[0].ToStatus);
        Assert.Null(events[0].ActorName);
        Assert.Equal("Expired", events[0].Reason);
        Assert.Equal("Sarah Kerrigan", events[1].ActorName);
    }

    [Fact]
    public async Task GetEvents_UnknownTicket_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await Client.GetAsync($"/api/tickets/{Guid.NewGuid()}/events");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_WithoutAuthentication_ReturnsUnauthorized()
    {
        var ticketId = await CreateTicketAsync();

        var response = await Client.GetAsync($"/api/tickets/{ticketId}/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}