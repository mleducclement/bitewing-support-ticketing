using bitewing.Dtos.Tickets;
using bitewing.Services;
using Microsoft.AspNetCore.Mvc;

namespace bitewing.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTicketRequest request)
    {
        var ticket = await _ticketService.CreateAsync(request);

        var response = new TicketResponse(
            ticket.Id,
            ticket.Subject,
            ticket.Body,
            ticket.CustomerName,
            ticket.CustomerEmail,
            ticket.ClinicName,
            ticket.Status,
            ticket.Priority,
            ticket.CreatedAt);

        return Created($"/api/tickets/{ticket.Id}", response);
    }
}
