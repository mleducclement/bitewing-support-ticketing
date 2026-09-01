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
        Assert.Equal(body.CreatedAt, body.UpdatedAt);
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

    [Fact]
    public async Task GetByNumber_ExistingTicket_ReturnsSameTicketAsById()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var byId = await Client.GetFromJsonAsync<TicketResponse>(
            $"/api/tickets/{ticketId}", TestJson.Options);

        var response = await Client.GetAsync($"/api/tickets/{byId!.DisplayId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var byNumber = await response.Content.ReadFromJsonAsync<TicketResponse>(TestJson.Options);
        Assert.Equal(ticketId, byNumber!.Id);
    }

    [Fact]
    public async Task GetByNumber_LowercasePrefix_StillResolves()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var byId = await Client.GetFromJsonAsync<TicketResponse>(
            $"/api/tickets/{ticketId}", TestJson.Options);

        var response = await Client.GetAsync($"/api/tickets/{byId!.DisplayId.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetByNumber_UnknownNumber_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await Client.GetAsync("/api/tickets/CS-99999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}