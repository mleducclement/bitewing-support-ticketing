using System.ComponentModel.DataAnnotations;

namespace bitewing.Dtos.Tickets;

public record CreateTicketRequest(
    [Required, StringLength(200)] string ClinicName,
    [Required, StringLength(200)] string CustomerName,
    [Required, EmailAddress, StringLength(200)] string CustomerEmail,
    [Required, StringLength(200)] string Subject,
    [Required, StringLength(5000)] string Body
);