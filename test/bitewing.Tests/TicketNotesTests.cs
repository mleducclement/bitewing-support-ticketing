using System.Net;
using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bitewing.Tests;

public class TicketNotesTests : TicketsTestBase
{
    public TicketNotesTests(TicketsApiFactory factory) : base(factory)
    {
    }

    private Task<HttpResponseMessage> PostNoteAsync(Guid ticketId, string body) =>
        Client.PostAsJsonAsync($"/api/tickets/{ticketId}/notes", new CreateNoteRequest(body));

    private async Task<List<NoteResponse>> GetNotesAsync(Guid ticketId)
    {
        var response = await Client.GetAsync($"/api/tickets/{ticketId}/notes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<List<NoteResponse>>(TestJson.Options))!;
    }

    [Fact]
    public async Task AddNote_ValidBody_Returns201WithBodyAndAuthor()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await PostNoteAsync(ticketId, "Called the clinic, waiting on a screenshot.");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var note = await response.Content.ReadFromJsonAsync<NoteResponse>(TestJson.Options);
        Assert.Equal("Called the clinic, waiting on a screenshot.", note!.Body);
        Assert.Equal("Sarah Kerrigan", note.AuthorName);
    }

    [Fact]
    public async Task AddNote_ThenGet_ReturnsTheNote()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await PostNoteAsync(ticketId, "First note");

        var note = Assert.Single(await GetNotesAsync(ticketId));
        Assert.Equal("First note", note.Body);
        Assert.Equal("Sarah Kerrigan", note.AuthorName);
    }

    [Fact]
    public async Task AddNote_Twice_ListedNewestFirst()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await PostNoteAsync(ticketId, "Older note");
        await PostNoteAsync(ticketId, "Newer note");

        var notes = await GetNotesAsync(ticketId);

        Assert.Equal(2, notes.Count);
        Assert.Equal("Newer note", notes[0].Body);
        Assert.Equal("Older note", notes[1].Body);
    }

    [Fact]
    public async Task GetNotes_NoNotes_ReturnsEmptyList()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        Assert.Empty(await GetNotesAsync(ticketId));
    }

    [Fact]
    public async Task AddNote_AgentWhoDoesNotOwnTheTicket_Succeeds()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);

        await LoginAsync(SeededSecondAgentEmail);
        var response = await PostNoteAsync(ticketId, "Adding context from a related ticket.");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var note = Assert.Single(await GetNotesAsync(ticketId));
        Assert.Equal("Jim Raynor", note.AuthorName);
    }

    [Theory]
    [InlineData("resolve")]
    [InlineData("cancel")]
    public async Task AddNote_ClosedTicket_Succeeds(string closeAction)
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await Client.PostAsync($"/api/tickets/{ticketId}/claim", null);
        if (closeAction == "cancel")
            await Client.PostAsJsonAsync($"/api/tickets/{ticketId}/cancel", new CancelTicketRequest(CancellationReason.Withdrawn));
        else
            await Client.PostAsync($"/api/tickets/{ticketId}/resolve", null);

        var response = await PostNoteAsync(ticketId, "Customer emailed back after close.");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(await GetNotesAsync(ticketId));
    }

    [Fact]
    public async Task AddNote_UnknownTicket_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await PostNoteAsync(Guid.NewGuid(), "Note for a ghost");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNotes_UnknownTicket_ReturnsNotFound()
    {
        await LoginAsSeededAgentAsync();

        var response = await Client.GetAsync($"/api/tickets/{Guid.NewGuid()}/notes");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddNote_BlankBody_ReturnsBadRequest()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await PostNoteAsync(ticketId, "   ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await GetNotesAsync(ticketId));
    }

    [Fact]
    public async Task AddNote_MissingBody_ReturnsBadRequest()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();

        var response = await Client.PostAsJsonAsync($"/api/tickets/{ticketId}/notes", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddNote_WithoutAuthentication_ReturnsUnauthorized()
    {
        var ticketId = await CreateTicketAsync();

        var response = await PostNoteAsync(ticketId, "Unauthenticated note");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNotes_WithoutAuthentication_ReturnsUnauthorized()
    {
        var ticketId = await CreateTicketAsync();

        var response = await Client.GetAsync($"/api/tickets/{ticketId}/notes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddNote_DoesNotWriteTicketEvent()
    {
        var ticketId = await CreateTicketAsync();
        await LoginAsSeededAgentAsync();
        await PostNoteAsync(ticketId, "A note, not an event");

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.TicketEvents.Where(e => e.TicketId == ticketId).ToListAsync());
    }
}