using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace bitewing.Tests;

public class TicketsControllerTests : IClassFixture<TicketsApiFactory>
{
    private readonly TicketsApiFactory _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;

    public TicketsControllerTests(TicketsApiFactory factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        _client = factory.CreateClient();
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
}