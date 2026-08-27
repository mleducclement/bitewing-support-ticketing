using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace bitewing.Tests;

public class TicketCreationTests : TicketsTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TicketCreationTests(TicketsApiFactory factory, ITestOutputHelper testOutputHelper) : base(factory)
    {
        _testOutputHelper = testOutputHelper;
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

        var response = await Client.PostAsJsonAsync("/api/tickets", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        _testOutputHelper.WriteLine(body!.ToString());
        Assert.NotNull(body);
        Assert.Equal(TicketStatus.Open, body!.Status);
        Assert.Equal(TicketPriority.Normal, body.Priority);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Tickets.FindAsync(body.Id);

        Assert.NotNull(persisted);
        Assert.Equal(request.Subject, persisted!.Subject);
        Assert.Equal(request.Body, persisted.Body);
        Assert.Null(persisted.AssigneeId);
        Assert.False(persisted.HandoffFlag);

        Assert.True(persisted.TicketNumber > 0);
        Assert.Equal($"CS-{persisted.TicketNumber}", body.DisplayId);
    }

    [Fact]
    public async Task Create_MultipleTickets_AssignsSequentialTicketNumbers()
    {
        var request = new CreateTicketRequest(
            "Maple Dental",
            "Jordan Reyes",
            "jordan@mapledental.example",
            "Cannot access booking calendar",
            "Our front desk cannot see the calendar since this morning.");

        var firstResponse = await Client.PostAsJsonAsync("/api/tickets", request);
        var first = await firstResponse.Content.ReadFromJsonAsync<TicketResponse>();

        var secondResponse = await Client.PostAsJsonAsync("/api/tickets", request);
        var second = await secondResponse.Content.ReadFromJsonAsync<TicketResponse>();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstPersisted = await db.Tickets.FindAsync(first!.Id);
        var secondPersisted = await db.Tickets.FindAsync(second!.Id);

        Assert.Equal(firstPersisted!.TicketNumber + 1, secondPersisted!.TicketNumber);
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

        var response = await Client.PostAsJsonAsync("/api/tickets", request);

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

        var response = await Client.PostAsJsonAsync("/api/tickets", request);

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

        var response = await Client.PostAsJsonAsync("/api/tickets", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Body", problem!.Errors.Keys);
    }
}