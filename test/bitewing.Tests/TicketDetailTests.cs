using System.Net;
using System.Net.Http.Json;
using bitewing.Dtos.Tickets;

namespace bitewing.Tests;

public class TicketDetailTests : TicketsTestBase
{
    public TicketDetailTests(TicketsApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetById_ExistingTicket_ReturnsTicket()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);
        Assert.Equal(ticketId, body!.Id);
        Assert.Equal("Cannot access booking calendar", body.Subject);
    }

    [Fact]
    public async Task GetById_UnknownTicket_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await Client.GetAsync($"/api/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithoutAuthentication_ReturnsUnauthorized()
    {
        var ticketId = await CreateTicketAsync();

        var response = await Client.GetAsync($"/api/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}