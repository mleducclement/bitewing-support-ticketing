using bitewing.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace bitewing.Extensions;

// Demo tickets so the queue is not empty on a fresh database (local or Render).
// Runs on every startup but seeds only while the Tickets table is empty, so once
// a real ticket is created the seed never runs again. Fixed ids mean a racy
// double-run collides on the primary key instead of duplicating.
public static class SeedTicketsExtensions
{
    public static async Task SeedTicketsAsync(
        this AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        if (await db.Tickets.AnyAsync()) return;

        var agents = await ResolveAgentIdsAsync(userManager);
        if (agents.Count == 0)
        {
            logger.LogWarning("Skipping ticket seed: no seeded agents found");
            return;
        }

        var now = DateTime.UtcNow;
        var tickets = BuildTickets(now, agents);

        db.Tickets.AddRange(tickets);
        await db.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} demo tickets", tickets.Count);
    }

    private static async Task<Dictionary<string, string>> ResolveAgentIdsAsync(
        UserManager<ApplicationUser> userManager)
    {
        var emails = new[]
        {
            "jraynor@bitewing.net",
            "skerrigan@bitewing.net",
            "gdugalle@bitewing.net",
            "eduke@bitewing.net",
            "astukov@bitewing.net",
            "sduran@bitewing.net",
        };

        var map = new Dictionary<string, string>();
        foreach (var email in emails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is not null) map[email] = user.Id;
        }

        return map;
    }

    private static List<Ticket> BuildTickets(DateTime now, Dictionary<string, string> agents)
    {
        string? Agent(string email) => agents.GetValueOrDefault(email);

        return
        [
            Open(1, "Schedule completely inaccessible since this morning",
                "The calendar just spins and never loads. We cannot see any of today's appointments.",
                "Dr. Alison Reyes", "alison@brightsmiledental.example", "Bright Smile Dental",
                TicketPriority.Urgent, now.AddHours(-3)),

            InProgress(2, "Online booking widget rejecting all new patients",
                "Every new patient booking fails with a generic error. Existing patients can still book.",
                "Marcus Tan", "frontdesk@lakesideortho.example", "Lakeside Orthodontics",
                TicketPriority.Urgent, Agent("skerrigan@bitewing.net"), now.AddDays(-1)),

            Blocked(3, "Claim submissions stuck in pending for two days",
                "A batch of insurance claims submitted Monday still shows pending. No rejection, no acceptance.",
                "Dana Whitfield", "billing@familycareperio.example", "Family Care Periodontics",
                TicketPriority.Normal, Agent("jraynor@bitewing.net"), now.AddDays(-6), blockedSince: now.AddDays(-5)),

            InProgress(4, "Need to remove a former employee's login",
                "Hygienist left last week. Want to make sure her account no longer has access.",
                "Robert Kim", "admin@cedarparkdental.example", "Cedar Park Dental",
                TicketPriority.Normal, Agent("skerrigan@bitewing.net"), now.AddDays(-2)),

            Open(5, "How do I set up automated appointment reminders?",
                "We want text reminders 24 hours before each visit. Not sure where that setting lives.",
                "Sofia Marchetti", "reception@gentledentalcare.example", "Gentle Dental Care",
                TicketPriority.Normal, now.AddDays(-2)),

            Open(6, "Reminder texts going out in the wrong time zone",
                "Patients are getting 6 AM reminder texts. We are Pacific, the reminders look like Eastern.",
                "Henry Osei", "office@harbordentalgroup.example", "Harbor Dental Group",
                TicketPriority.Normal, now.AddDays(-4), handoffFlag: true),

            Blocked(7, "Invoice shows a plan we did not sign up for",
                "This month's invoice lists the Premium tier. We are on Standard and never changed it.",
                "Laura Benitez", "accounts@meadowbrookdental.example", "Meadowbrook Dental",
                TicketPriority.Normal, Agent("eduke@bitewing.net"), now.AddDays(-5), blockedSince: now.AddDays(-3)),

            InProgress(8, "Patient portal password resets not arriving",
                "Several patients say the reset email never shows up. Checked spam folders already.",
                "Grace Liu", "help@downtownsmilestudio.example", "Downtown Smile Studio",
                TicketPriority.Normal, Agent("astukov@bitewing.net"), now.AddDays(-7)),

            Open(9, "Can we get a report of no-shows by month?",
                "Would love a breakdown of missed appointments per month. Not urgent, planning for Q4.",
                "Daniel Pope", "manager@sunsetfamilydental.example", "Sunset Family Dental",
                TicketPriority.Low, now.AddDays(-9)),

            Resolved(10, "Feature request: color-code providers on the calendar",
                "It would help a lot to assign each provider a color in the day view.",
                "Emily Sanders", "hello@riverstonedental.example", "Riverstone Dental",
                TicketPriority.Low, Agent("sduran@bitewing.net"), now.AddDays(-13)),

            Resolved(11, "Double-billed for August subscription",
                "Two identical charges on the same day for the monthly subscription. Need one refunded.",
                "Victor Nguyen", "finance@greenvalleyortho.example", "Green Valley Orthodontics",
                TicketPriority.Normal, Agent("eduke@bitewing.net"), now.AddDays(-16)),

            Cancelled(12, "asdfasdf test ticket please ignore",
                "testing the form",
                "Unknown", "test@example.com", "N/A",
                TicketPriority.Normal, now.AddDays(-20), CancellationReason.Spam),
        ];
    }

    private static Guid SeedId(int n) => Guid.Parse($"5eed0000-0000-0000-0000-{n:D12}");

    private static Ticket Open(
        int n, string subject, string body, string customerName, string customerEmail,
        string clinicName, TicketPriority priority, DateTime createdAt, bool handoffFlag = false) =>
        new()
        {
            Id = SeedId(n),
            Subject = subject,
            Body = body,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            ClinicName = clinicName,
            Status = TicketStatus.Open,
            Priority = priority,
            HandoffFlag = handoffFlag,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };

    private static Ticket InProgress(
        int n, string subject, string body, string customerName, string customerEmail,
        string clinicName, TicketPriority priority, string? assigneeId, DateTime createdAt) =>
        new()
        {
            Id = SeedId(n),
            Subject = subject,
            Body = body,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            ClinicName = clinicName,
            Status = TicketStatus.InProgress,
            Priority = priority,
            AssigneeId = assigneeId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt.AddHours(6),
        };

    private static Ticket Blocked(
        int n, string subject, string body, string customerName, string customerEmail,
        string clinicName, TicketPriority priority, string? assigneeId, DateTime createdAt,
        DateTime blockedSince) =>
        new()
        {
            Id = SeedId(n),
            Subject = subject,
            Body = body,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            ClinicName = clinicName,
            Status = TicketStatus.Blocked,
            Priority = priority,
            AssigneeId = assigneeId,
            BlockedSince = blockedSince,
            CreatedAt = createdAt,
            UpdatedAt = blockedSince,
        };

    private static Ticket Resolved(
        int n, string subject, string body, string customerName, string customerEmail,
        string clinicName, TicketPriority priority, string? assigneeId, DateTime createdAt) =>
        new()
        {
            Id = SeedId(n),
            Subject = subject,
            Body = body,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            ClinicName = clinicName,
            Status = TicketStatus.Resolved,
            Priority = priority,
            AssigneeId = assigneeId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt.AddDays(1),
        };

    private static Ticket Cancelled(
        int n, string subject, string body, string customerName, string customerEmail,
        string clinicName, TicketPriority priority, DateTime createdAt, CancellationReason reason) =>
        new()
        {
            Id = SeedId(n),
            Subject = subject,
            Body = body,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            ClinicName = clinicName,
            Status = TicketStatus.Cancelled,
            Priority = priority,
            CancellationReason = reason,
            CreatedAt = createdAt,
            UpdatedAt = createdAt.AddHours(2),
        };
}
