using System.Net;
using System.Text;
using System.Text.Json;
using bitewing.Data;
using bitewing.Options;
using bitewing.Services;

namespace bitewing.Tests;

// Unit tests for the Claude Messages API request/response shape. No database,
// no real network call - a fake handler stands in for Anthropic's API so these
// run without an ANTHROPIC_API_KEY.
public class ClaudeClassifierTests
{
    private class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedRequestBody { get; private set; }

        public FakeHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            CapturedRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private static ClaudeClassifier BuildClassifier(HttpStatusCode statusCode, string responseBody,
        out FakeHandler handler)
    {
        handler = new FakeHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") };
        var options = Microsoft.Extensions.Options.Options.Create(new ClaudeOptions
            { ApiKey = "test-key", Model = "claude-haiku-4-5-20251001" });
        return new ClaudeClassifier(httpClient, options);
    }

    private const string WellFormedResponse = """
        {
          "content": [
            { "type": "tool_use", "name": "classify_ticket", "input": { "area": "Reminders", "type": "HowTo" } }
          ]
        }
        """;

    [Fact]
    public async Task ClassifyTicket_SendsExpectedHeadersAndToolCall()
    {
        var classifier = BuildClassifier(HttpStatusCode.OK, WellFormedResponse, out var handler);

        await classifier.ClassifyTicket("Can't find the reminders tab", "Where is it?");

        Assert.NotNull(handler.CapturedRequest);
        Assert.Equal("test-key", handler.CapturedRequest!.Headers.GetValues("x-api-key").Single());
        Assert.Equal("2023-06-01", handler.CapturedRequest.Headers.GetValues("anthropic-version").Single());
        Assert.Equal("https://api.anthropic.com/v1/messages", handler.CapturedRequest.RequestUri!.ToString());

        using var body = JsonDocument.Parse(handler.CapturedRequestBody!);
        Assert.Equal("claude-haiku-4-5-20251001", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("classify_ticket",
            body.RootElement.GetProperty("tool_choice").GetProperty("name").GetString());

        var tool = body.RootElement.GetProperty("tools")[0];
        var areaEnum = tool.GetProperty("input_schema").GetProperty("properties").GetProperty("area")
            .GetProperty("enum").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Equal(Enum.GetNames<TicketArea>(), areaEnum);
    }

    [Fact]
    public async Task ClassifyTicket_WellFormedResponse_ParsesResult()
    {
        var classifier = BuildClassifier(HttpStatusCode.OK, WellFormedResponse, out _);

        var result = await classifier.ClassifyTicket("subject", "body");

        Assert.Equal(TicketArea.Reminders, result.Area);
        Assert.Equal(TicketType.HowTo, result.Type);
        Assert.Equal("claude-haiku-4-5-20251001", result.ModelName);
        Assert.Equal("v1", result.PromptVersion);
    }

    [Fact]
    public async Task ClassifyTicket_NonSuccessStatus_Throws()
    {
        var classifier = BuildClassifier(HttpStatusCode.TooManyRequests, "{\"error\":\"rate limited\"}", out _);

        await Assert.ThrowsAsync<ClassificationFailedException>(() => classifier.ClassifyTicket("s", "b"));
    }

    [Fact]
    public async Task ClassifyTicket_NoToolUseBlock_Throws()
    {
        var classifier = BuildClassifier(HttpStatusCode.OK, """{ "content": [{ "type": "text", "text": "sorry" }] }""",
            out _);

        await Assert.ThrowsAsync<ClassificationFailedException>(() => classifier.ClassifyTicket("s", "b"));
    }

    [Fact]
    public async Task ClassifyTicket_UnrecognizedAreaValue_Throws()
    {
        var classifier = BuildClassifier(HttpStatusCode.OK,
            """{ "content": [{ "type": "tool_use", "name": "classify_ticket", "input": { "area": "NotARealArea", "type": "Broken" } }] }""",
            out _);

        await Assert.ThrowsAsync<ClassificationFailedException>(() => classifier.ClassifyTicket("s", "b"));
    }
}