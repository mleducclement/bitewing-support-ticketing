using System.Net.Http.Json;
using bitewing.Dtos.Tickets;
using Microsoft.AspNetCore.Mvc.Testing;

namespace bitewing.Tests;

public abstract class TicketsTestBase : IClassFixture<TicketsApiFactory>
{
    protected const string SeededAgentEmail = "skerrigan@bitewing.net";
    protected const string SeededAgentPassword = "Password123!";
    protected const string SeededLeadEmail = "amengsk@bitewing.net";
    protected const string SeededSecondAgentEmail = "jraynor@bitewing.net";

    protected readonly TicketsApiFactory Factory;
    protected readonly HttpClient Client;

    protected TicketsTestBase(TicketsApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
    }

    protected async Task<Guid> CreateTicketAsync()
    {
        var request = new CreateTicketRequest(
            "Maple Dental",
            "Jordan Reyes",
            "jordan@mapledental.example",
            "Cannot access booking calendar",
            "Our front desk cannot see the calendar since this morning.");

        var response = await Client.PostAsJsonAsync("/api/tickets", request);
        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();

        return body!.Id;
    }

    protected async Task LoginAsync(string email)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password = SeededAgentPassword });
        response.EnsureSuccessStatusCode();
    }

    protected Task LoginAsSeededAgentAsync() => LoginAsync(SeededAgentEmail);
}