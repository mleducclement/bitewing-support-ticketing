using System.Text.Json;
using System.Text.Json.Serialization;

namespace bitewing.Tests;

// The API serializes enums as names (JsonStringEnumConverter in Program.cs).
// System.Net.Http.Json's default web options don't, so response bodies with
// enum-typed fields (TicketResponse.Status/Priority/CancellationReason) must be
// read with these options.
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
