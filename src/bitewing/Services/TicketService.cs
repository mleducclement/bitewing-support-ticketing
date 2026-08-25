using bitewing.Data;
using bitewing.Dtos.Tickets;

namespace bitewing.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _db;

    public TicketService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Ticket> CreateAsync(CreateTicketRequest request)
    {
        var now = DateTime.UtcNow;

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Subject = request.Subject,
            Body = request.Body,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            ClinicName = request.ClinicName,
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        return ticket;
    }
}