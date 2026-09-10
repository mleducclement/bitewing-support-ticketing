using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using bitewing.Data;
using bitewing.Options;
using Microsoft.Extensions.Options;

namespace bitewing.Services;

public class ClaudeClassifier : IClassifier
{
    private const string PromptVersion = "v1";
    private const string AnthropicVersion = "2023-06-01";
    private const string ToolName = "classify_ticket";

    // Boundary notes lifted from spec.md §6 so the model makes the same
    // calls a human reading that section would.
    private const string SystemPrompt = """
        You classify a customer support ticket for Chairside, a cloud scheduling and
        practice management product for dental clinics, along two independent axes.

        Area - where in the product the ticket concerns:
        - Claims
        - BookingAndCalendar (includes the patient portal, whose primary function is
          appointment request/confirmation)
        - Reminders
        - AccessAndAccounts (user management and staff turnover, not just password resets)
        - SubscriptionAndInvoicing (Chairside's own invoices and subscription billing;
          not the clinic's patient billing or insurance claims, which are Claims)
        - Other (use only when none of the above fit)

        Type - what kind of request it is:
        - Broken: the product isn't working as expected
        - HowTo: the customer can't find or figure out something that already works
        - FeatureRequest: the customer wants something that doesn't exist today

        Broken vs HowTo is the hardest boundary: a customer reports the same symptom
        whether the product is broken or they simply can't find the button. Judge by
        what the ticket text actually describes, not by assuming the harder case.

        Call the classify_ticket tool with your answer for both axes.
        """;

    private static readonly string[] AreaValues = Enum.GetNames<TicketArea>();
    private static readonly string[] TypeValues = Enum.GetNames<TicketType>();

    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly ClaudeOptions _options;

    public ClaudeClassifier(HttpClient httpClient, IOptions<ClaudeOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ClassificationResult> ClassifyTicket(string title, string body,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new ClaudeRequest(
            _options.Model,
            MaxTokens: 256,
            SystemPrompt,
            Messages: [new ClaudeMessage("user", $"Subject: {title}\n\n{body}")],
            Tools:
            [
                new ClaudeTool(
                    ToolName,
                    "Record the area and type classification for this ticket.",
                    new ClaudeToolInputSchema(
                        "object",
                        new Dictionary<string, ClaudeToolProperty>
                        {
                            ["area"] = new("string", AreaValues),
                            ["type"] = new("string", TypeValues)
                        },
                        Required: ["area", "type"]))
            ],
            ToolChoice: new ClaudeToolChoice("tool", ToolName));

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, RequestJsonOptions), Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ClassificationFailedException("Failed to reach the Claude API.", ex);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new ClassificationFailedException(
                $"Claude API returned {(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}");

        return ParseResponse(responseBody);
    }

    private ClassificationResult ParseResponse(string responseBody)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(responseBody);
        }
        catch (JsonException ex)
        {
            throw new ClassificationFailedException("Claude API returned malformed JSON.", ex);
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
                throw new ClassificationFailedException("Claude API response had no content array.");

            foreach (var block in content.EnumerateArray())
            {
                if (!block.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "tool_use")
                    continue;

                if (!block.TryGetProperty("name", out var nameProp) || nameProp.GetString() != ToolName)
                    continue;

                if (!block.TryGetProperty("input", out var input))
                    throw new ClassificationFailedException("Claude API tool_use block had no input.");

                var areaText = input.TryGetProperty("area", out var areaProp) ? areaProp.GetString() : null;
                var typeText = input.TryGetProperty("type", out var typeValueProp) ? typeValueProp.GetString() : null;

                if (!Enum.TryParse<TicketArea>(areaText, out var area))
                    throw new ClassificationFailedException($"Claude API returned an unrecognized area '{areaText}'.");

                if (!Enum.TryParse<TicketType>(typeText, out var type))
                    throw new ClassificationFailedException($"Claude API returned an unrecognized type '{typeText}'.");

                return new ClassificationResult(area, type, PromptVersion, _options.Model);
            }
        }

        throw new ClassificationFailedException("Claude API response had no classify_ticket tool_use block.");
    }

    private record ClaudeRequest(
        string Model,
        int MaxTokens,
        string System,
        List<ClaudeMessage> Messages,
        List<ClaudeTool> Tools,
        ClaudeToolChoice ToolChoice);

    private record ClaudeMessage(string Role, string Content);

    private record ClaudeTool(
        string Name,
        string Description,
        ClaudeToolInputSchema InputSchema);

    private record ClaudeToolInputSchema(
        string Type,
        Dictionary<string, ClaudeToolProperty> Properties,
        string[] Required);

    private record ClaudeToolProperty(string Type, string[] Enum);

    private record ClaudeToolChoice(string Type, string Name);
}
