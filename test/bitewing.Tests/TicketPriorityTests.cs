using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

public class TicketPriorityTests : TicketsTestBase
{
    public TicketPriorityTests(TicketsApiFactory factory) : base(factory)
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
    public async Task ChangePriority_Upgrade_UpdatesTicketAndRecordsEvent()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/priority",
            new PriorityChangeRequest(TicketPriority.Urgent, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.Equal(TicketPriority.Urgent, body!.Priority);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var agent = await userManager.FindByEmailAsync(SeededAgentEmail);

        var persisted = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(persisted);
        Assert.Equal(TicketPriority.Urgent, persisted!.Priority);

        var priorityEvent = Assert.Single(await db.TicketEvents.Where(e => e.TicketId == ticketId).ToListAsync());
        Assert.Equal(TicketEventType.PriorityChanged, priorityEvent.EventType);
        Assert.Equal(agent!.Id, priorityEvent.ActorId);
        Assert.Equal(TicketPriority.Normal, priorityEvent.FromPriority);
        Assert.Equal(TicketPriority.Urgent, priorityEvent.ToPriority);
        Assert.Null(priorityEvent.Reason);
    }

    [Fact]
    public async Task ChangePriority_DowngradeWithReason_UpdatesTicketAndRecordsReason()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/priority",
            new PriorityChangeRequest(TicketPriority.Low, "Feature request, not time-sensitive"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);
        Assert.Equal(TicketPriority.Low, body!.Priority);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var priorityEvent = Assert.Single(await db.TicketEvents.Where(e => e.TicketId == ticketId).ToListAsync());
        Assert.Equal(TicketPriority.Normal, priorityEvent.FromPriority);
        Assert.Equal(TicketPriority.Low, priorityEvent.ToPriority);
        Assert.Equal("Feature request, not time-sensitive", priorityEvent.Reason);
    }

    [Fact]
    public async Task ChangePriority_DowngradeWithoutReason_ReturnsConflict()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/priority",
            new PriorityChangeRequest(TicketPriority.Low, null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangePriority_SamePriority_IsNoOpAndRecordsNoEvent()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/priority",
            new PriorityChangeRequest(TicketPriority.Normal, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);
        Assert.Equal(TicketPriority.Normal, body!.Priority);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.TicketEvents.Where(e => e.TicketId == ticketId).ToListAsync());
    }

    [Fact]
    public async Task ChangePriority_NotAssignee_StillSucceeds()
    {
        var ticketId = await CreateInProgressTicketAsync();

        await LoginAsync(SeededSecondAgentEmail);
        var response = await Client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/priority",
            new PriorityChangeRequest(TicketPriority.Urgent, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangePriority_ResolvedTicket_ReturnsConflict()
    {
        var ticketId = await CreateInProgressTicketAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/resolve", null);

        var response = await Client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/priority",
            new PriorityChangeRequest(TicketPriority.Urgent, null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangePriority_CancelledTicket_ReturnsConflict()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsJsonAsync($"/api/tickets/{ticketId}/cancel", new CancelTicketRequest(CancellationReason.Duplicate));

        var response = await Client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/priority",
            new PriorityChangeRequest(TicketPriority.Urgent, null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangePriority_MissingNewPriority_ReturnsBadRequest()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/priority",
            new PriorityChangeRequest(null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePriority_WithoutAuthentication_ReturnsUnauthorized()
    {
        var ticketId = await CreateTicketAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/priority",
            new PriorityChangeRequest(TicketPriority.Urgent, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePriority_NonexistentTicket_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/priority",
            new PriorityChangeRequest(TicketPriority.Urgent, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}