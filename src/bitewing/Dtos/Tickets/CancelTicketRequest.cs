using System.ComponentModel.DataAnnotations;
using bitewing.Data;

namespace bitewing.Dtos.Tickets;

public record CancelTicketRequest(
    [Required, AllowedValues(CancellationReason.Duplicate, CancellationReason.Spam, CancellationReason.Withdrawn)]
    CancellationReason? Reason
);