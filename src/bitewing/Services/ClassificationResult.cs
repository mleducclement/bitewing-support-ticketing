using bitewing.Data;

namespace bitewing.Services;

public record ClassificationResult(
    TicketArea Area,
    TicketType Type,
    string PromptVersion,
    string ModelName
);