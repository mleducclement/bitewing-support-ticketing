using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace bitewing.Tests;

public class TicketsControllerTests : IClassFixture<TicketsApiFactory>
{
    private const string SeededAgentEmail = "skerrigan@bitewing.net";
    private const string SeededAgentPassword = "Password123!";

    private readonly TicketsApiFactory _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;

    public TicketsControllerTests(TicketsApiFactory factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
    }

    private async Task<Guid> CreateTicketAsync()
    {
        var request = new CreateTicketRequest(
            "Maple Dental",
            "Jordan Reyes",
            "jordan@mapledental.example",
            "Cannot access booking calendar",
            "Our front desk cannot see the calendar since this morning.");

        var response = await _client.PostAsJsonAsync("/api/tickets", request);
        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();

        return body!.Id;
    }

    private async Task LoginAsSeededAgentAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email = SeededAgentEmail, password = SeededAgentPassword });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAndPersistsTicket()
    {
        var request = new CreateTicketRequest(
            "Maple Dental",
            "Jordan Reyes",
            "jordan@mapledental.example",
            "Cannot access booking calendar",
            "Our front desk cannot see the calendar since this morning.");

        var response = await _client.PostAsJsonAsync("/api/tickets", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        _testOutputHelper.WriteLine(body!.ToString());
        Assert.NotNull(body);
        Assert.Equal(TicketStatus.Open, body!.Status);
        Assert.Equal(TicketPriority.Normal, body.Priority);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Tickets.FindAsync(body.Id);

        Assert.NotNull(persisted);
        Assert.Equal(request.Subject, persisted!.Subject);
        Assert.Equal(request.Body, persisted.Body);
        Assert.Null(persisted.AssigneeId);
        Assert.False(persisted.HandoffFlag);
    }

    [Fact]
    public async Task Create_WithMissingSubject_ReturnsBadRequest()
    {
        var request = new CreateTicketRequest(
            "Maple Dental",
            "Jordan Reyes",
            "jordan@mapledental.example",
            "",
            "Body text");

        var response = await _client.PostAsJsonAsync("/api/tickets", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Subject", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithInvalidEmail_ReturnsBadRequest()
    {
        var request = new CreateTicketRequest(
            "Maple Dental",
            "Jordan Reyes",
            "not-an-email",
            "Cannot access booking calendar",
            "Body text");

        var response = await _client.PostAsJsonAsync("/api/tickets", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("CustomerEmail", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithOversizedBody_ReturnsBadRequest()
    {
        var request = new CreateTicketRequest(
            "Maple Dental",
            "Jordan Reyes",
            "jordan@mapledental.example",
            "Cannot access booking calendar",
            new string('x', 5001));

        var response = await _client.PostAsJsonAsync("/api/tickets", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Body", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Claim_OpenTicket_AssignsAgentAndSetsInProgress()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await _client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(body);
        Assert.Equal(TicketStatus.InProgress, body!.Status);

        using var scope = _factory.Services.CreateScope();
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

        var response = await _client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Claim_AlreadyClaimedTicket_ReturnsConflict()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var first = await _client.PostAsync($"/api/tickets/{ticketId}/claim", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsync($"/api/tickets/{ticketId}/claim", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Claim_NonexistentTicket_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await _client.PostAsync($"/api/tickets/{Guid.NewGuid()}/claim", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_SortsByPriorityDescendingThenCreatedAtAscending()
    {
        var firstUrgentId = await CreateTicketAsync();
        var secondUrgentId = await CreateTicketAsync();
        var lowId = await CreateTicketAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Tickets.FindAsync(firstUrgentId))!.Priority = TicketPriority.Urgent;
            (await db.Tickets.FindAsync(secondUrgentId))!.Priority = TicketPriority.Urgent;
            (await db.Tickets.FindAsync(lowId))!.Priority = TicketPriority.Low;
            await db.SaveChangesAsync();
        }

        await LoginAsSeededAgentAsync();
        var response = await _client.GetAsync("/api/tickets");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>();
        var ids = tickets!.Select(t => t.Id).ToList();

        Assert.True(ids.IndexOf(firstUrgentId) < ids.IndexOf(secondUrgentId));
        Assert.True(ids.IndexOf(secondUrgentId) < ids.IndexOf(lowId));
    }

    [Fact]
    public async Task Get_FiltersByStatus()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await _client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        var response = await _client.GetAsync("/api/tickets?status=InProgress");
        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>();
        Assert.Contains(tickets!, t => t.Id == ticketId);

        var cancelledResponse = await _client.GetAsync("/api/tickets?status=Cancelled");
        var cancelledTickets = await cancelledResponse.Content.ReadFromJsonAsync<List<TicketResponse>>();
        Assert.DoesNotContain(cancelledTickets!, t => t.Id == ticketId);
    }

    [Fact]
    public async Task Get_FiltersByPriority()
    {
        var ticketId = await CreateTicketAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Tickets.FindAsync(ticketId))!.Priority = TicketPriority.Urgent;
            await db.SaveChangesAsync();
        }

        await LoginAsSeededAgentAsync();

        var response = await _client.GetAsync("/api/tickets?priority=Urgent");
        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>();
        Assert.Contains(tickets!, t => t.Id == ticketId);

        var lowResponse = await _client.GetAsync("/api/tickets?priority=Low");
        var lowTickets = await lowResponse.Content.ReadFromJsonAsync<List<TicketResponse>>();
        Assert.DoesNotContain(lowTickets!, t => t.Id == ticketId);
    }

    [Fact]
    public async Task Get_FiltersByAssignee_AndResolvesAssigneeName()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await _client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var agent = await userManager.FindByEmailAsync(SeededAgentEmail);

        var response = await _client.GetAsync($"/api/tickets?assigneeId={agent!.Id}");
        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>();

        var ticket = Assert.Single(tickets!, t => t.Id == ticketId);
        Assert.Equal("Sarah Kerrigan", ticket.AssigneeName);
    }

    [Fact]
    public async Task Get_FiltersByHandoffFlag()
    {
        var ticketId = await CreateTicketAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Tickets.FindAsync(ticketId))!.HandoffFlag = true;
            await db.SaveChangesAsync();
        }

        await LoginAsSeededAgentAsync();

        var response = await _client.GetAsync("/api/tickets?handoffFlag=true");
        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>();
        Assert.Contains(tickets!, t => t.Id == ticketId);

        var notHandedOffResponse = await _client.GetAsync("/api/tickets?handoffFlag=false");
        var notHandedOffTickets = await notHandedOffResponse.Content.ReadFromJsonAsync<List<TicketResponse>>();
        Assert.DoesNotContain(notHandedOffTickets!, t => t.Id == ticketId);
    }

    [Fact]
    public async Task Get_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}