using System.ComponentModel.DataAnnotations;
using bitewing.Data;

namespace bitewing.Dtos.Tickets;

public record PriorityChangeRequest([Required] TicketPriority? NewPriority, string? Reason);