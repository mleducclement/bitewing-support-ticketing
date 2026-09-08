using bitewing.Data;

namespace bitewing.Dtos.Tickets;

public record NoteResponse(
    Guid Id,
    string AuthorName,
    string Body,
    DateTime CreatedAt
);
