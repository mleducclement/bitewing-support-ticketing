using bitewing.Data;
using bitewing.Dtos.Tickets;
using bitewing.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace bitewing.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketsController(ITicketService ticketService, UserManager<ApplicationUser> userManager)
    {
        _ticketService = ticketService;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTicketRequest request)
    {
        var ticket = await _ticketService.CreateAsync(request);

        return Created($"/api/tickets/{ticket.Id}", ToResponse(ticket));
    }

    [Authorize(Policy = "AgentAccess")]
    [HttpPost("{id:guid}/claim")]
    public async Task<IActionResult> Claim(Guid id)
    {
        var agentId = _userManager.GetUserId(User)!;

        Ticket? ticket;
        try
        {
            ticket = await _ticketService.ClaimAsync(id, agentId);
        }
        catch (TicketAlreadyClaimedException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidTicketTransitionException ex)
        {
            return Conflict(new { message = ex.Message });
        }

        if (ticket is null) return NotFound();

        return Ok(ToResponse(ticket));
    }

    [Authorize(Policy = "AgentAccess")]
    [HttpPost("{id:guid}/release")]
    public async Task<IActionResult> Release(Guid id)
    {
        var agentId = _userManager.GetUserId(User)!;
        var isTeamLead = User.IsInRole("TeamLead");

        Ticket? ticket;
        try
        {
            ticket = await _ticketService.ReleaseAsync(id, agentId, isTeamLead);
        }
        catch (InvalidTicketTransitionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedTicketActionException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }

        if (ticket is null) return NotFound();

        return Ok(ToResponse(ticket));
    }

    [Authorize(Policy = "AgentAccess")]
    [HttpPost("{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id)
    {
        var agentId = _userManager.GetUserId(User)!;
        var isTeamLead = User.IsInRole("TeamLead");

        Ticket? ticket;
        try
        {
            ticket = await _ticketService.BlockAsync(id, agentId, isTeamLead);
        }
        catch (InvalidTicketTransitionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedTicketActionException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }

        if (ticket is null) return NotFound();

        return Ok(ToResponse(ticket));
    }

    [Authorize(Policy = "AgentAccess")]
    [HttpPost("{id:guid}/unblock")]
    public async Task<IActionResult> Unblock(Guid id)
    {
        var agentId = _userManager.GetUserId(User)!;
        var isTeamLead = User.IsInRole("TeamLead");

        Ticket? ticket;
        try
        {
            ticket = await _ticketService.UnblockAsync(id, agentId, isTeamLead);
        }
        catch (InvalidTicketTransitionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedTicketActionException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }

        if (ticket is null) return NotFound();

        return Ok(ToResponse(ticket));
    }

    [Authorize(Policy = "AgentAccess")]
    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id)
    {
        var agentId = _userManager.GetUserId(User)!;
        var isTeamLead = User.IsInRole("TeamLead");

        Ticket? ticket;
        try
        {
            ticket = await _ticketService.ResolveAsync(id, agentId, isTeamLead);
        }
        catch (InvalidTicketTransitionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedTicketActionException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }

        if (ticket is null) return NotFound();

        return Ok(ToResponse(ticket));
    }

    [Authorize(Policy = "AgentAccess")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancelTicketRequest request)
    {
        var agentId = _userManager.GetUserId(User)!;
        var isTeamLead = User.IsInRole("TeamLead");

        Ticket? ticket;
        try
        {
            ticket = await _ticketService.CancelAsync(id, agentId, isTeamLead, request.Reason!.Value);
        }
        catch (InvalidTicketTransitionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedTicketActionException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }

        if (ticket is null) return NotFound();

        return Ok(ToResponse(ticket));
    }

    [Authorize(Policy = "AgentAccess")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] TicketQueueRequest filter)
    {
        var tickets = await _ticketService.GetQueueAsync(filter);

        return Ok(tickets.Select(ToResponse));
    }

    private static TicketResponse ToResponse(Ticket ticket) => new(
        ticket.Id,
        ticket.DisplayId,
        ticket.Subject,
        ticket.Body,
        ticket.CustomerName,
        ticket.CustomerEmail,
        ticket.ClinicName,
        ticket.Status,
        ticket.Priority,
        ticket.CreatedAt,
        ticket.Assignee is null ? null : $"{ticket.Assignee.FirstName} {ticket.Assignee.LastName}",
        ticket.HandoffFlag,
        ticket.BlockedSince,
        ticket.CancellationReason);
}
