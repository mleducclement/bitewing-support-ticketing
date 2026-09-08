using System.ComponentModel.DataAnnotations;

namespace bitewing.Dtos.Tickets;

public record CreateNoteRequest(
    [Required, StringLength(5000)] string Body
);