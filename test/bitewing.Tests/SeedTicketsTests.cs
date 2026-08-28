using System.Net.Http.Json;
using bitewing.Data;
using bitewing.Dtos.Tickets;
using bitewing.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace bitewing.Tests;

public class SeedTicketsTests : TicketsTestBase
{
    public SeedTicketsTests(TicketsApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Seed_PopulatesQueue_RespectingStatusInvariants()
    {
        await LoginAsSeededAgentAsync();

        var tickets = await Client.GetFromJsonAsync<List<TicketResponse>>("/api/tickets", TestJson.Options);

        Assert.NotNull(tickets);
        Assert.True(tickets!.Count >= 12);

        var openTicket = Assert.Single(
            tickets,
            t => t.Subject == "Schedule completely inaccessible since this morning");
        Assert.Equal(TicketStatus.Open, openTicket.Status);
        Assert.Null(openTicket.AssigneeName);

        var blockedTicket = Assert.Single(
            tickets,
            t => t.Subject == "Claim submissions stuck in pending for two days");
        Assert.Equal(TicketStatus.Blocked, blockedTicket.Status);
        Assert.NotNull(blockedTicket.BlockedSince);
        Assert.NotNull(blockedTicket.AssigneeName);

        var cancelledTicket = Assert.Single(
            tickets,
            t => t.Subject == "asdfasdf test ticket please ignore");
        Assert.Equal(TicketStatus.Cancelled, cancelledTicket.Status);
        Assert.Equal(CancellationReason.Spam, cancelledTicket.CancellationReason);
    }

    [Fact]
    public async Task Seed_IsIdempotent_WhenTicketsAlreadyExist()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var before = await db.Tickets.CountAsync();

        await db.SeedTicketsAsync(userManager, NullLogger.Instance);

        var after = await db.Tickets.CountAsync();
        Assert.Equal(before, after);
    }
}