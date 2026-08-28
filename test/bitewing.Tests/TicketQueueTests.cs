using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

public class TicketQueueTests : TicketsTestBase
{
    public TicketQueueTests(TicketsApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Get_SortsByPriorityDescendingThenCreatedAtAscending()
    {
        var firstUrgentId = await CreateTicketAsync();
        var secondUrgentId = await CreateTicketAsync();
        var lowId = await CreateTicketAsync();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Tickets.FindAsync(firstUrgentId))!.Priority = TicketPriority.Urgent;
            (await db.Tickets.FindAsync(secondUrgentId))!.Priority = TicketPriority.Urgent;
            (await db.Tickets.FindAsync(lowId))!.Priority = TicketPriority.Low;
            await db.SaveChangesAsync();
        }

        await LoginAsSeededAgentAsync();
        var response = await Client.GetAsync("/api/tickets");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(TestJson.Options);
        var ids = tickets!.Select(t => t.Id).ToList();

        Assert.True(ids.IndexOf(firstUrgentId) < ids.IndexOf(secondUrgentId));
        Assert.True(ids.IndexOf(secondUrgentId) < ids.IndexOf(lowId));
    }

    [Fact]
    public async Task Get_FiltersByStatus()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        var response = await Client.GetAsync("/api/tickets?status=InProgress");
        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(TestJson.Options);
        Assert.Contains(tickets!, t => t.Id == ticketId);

        var cancelledResponse = await Client.GetAsync("/api/tickets?status=Cancelled");
        var cancelledTickets = await cancelledResponse.Content.ReadFromJsonAsync<List<TicketResponse>>(TestJson.Options);
        Assert.DoesNotContain(cancelledTickets!, t => t.Id == ticketId);
    }

    [Fact]
    public async Task Get_FiltersByPriority()
    {
        var ticketId = await CreateTicketAsync();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Tickets.FindAsync(ticketId))!.Priority = TicketPriority.Urgent;
            await db.SaveChangesAsync();
        }

        await LoginAsSeededAgentAsync();

        var response = await Client.GetAsync("/api/tickets?priority=Urgent");
        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(TestJson.Options);
        Assert.Contains(tickets!, t => t.Id == ticketId);

        var lowResponse = await Client.GetAsync("/api/tickets?priority=Low");
        var lowTickets = await lowResponse.Content.ReadFromJsonAsync<List<TicketResponse>>(TestJson.Options);
        Assert.DoesNotContain(lowTickets!, t => t.Id == ticketId);
    }

    [Fact]
    public async Task Get_FiltersByAssignee_AndResolvesAssigneeName()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var agent = await userManager.FindByEmailAsync(SeededAgentEmail);

        var response = await Client.GetAsync($"/api/tickets?assigneeId={agent!.Id}");
        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(TestJson.Options);

        var ticket = Assert.Single(tickets!, t => t.Id == ticketId);
        Assert.Equal("Sarah Kerrigan", ticket.AssigneeName);
    }

    [Fact]
    public async Task Get_FiltersByHandoffFlag()
    {
        var ticketId = await CreateTicketAsync();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Tickets.FindAsync(ticketId))!.HandoffFlag = true;
            await db.SaveChangesAsync();
        }

        await LoginAsSeededAgentAsync();

        var response = await Client.GetAsync("/api/tickets?handoffFlag=true");
        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(TestJson.Options);
        Assert.Contains(tickets!, t => t.Id == ticketId);

        var notHandedOffResponse = await Client.GetAsync("/api/tickets?handoffFlag=false");
        var notHandedOffTickets = await notHandedOffResponse.Content.ReadFromJsonAsync<List<TicketResponse>>(TestJson.Options);
        Assert.DoesNotContain(notHandedOffTickets!, t => t.Id == ticketId);
    }

    [Fact]
    public async Task Get_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}