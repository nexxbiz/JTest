using System.Text.Json;
using System.Text.Json.Serialization;

namespace JTest.Core.Tracing;

/// <summary>
/// Canonical JSON serialization for the execution trace. camelCase names and camelCase
/// string enums match contracts/execution-trace.schema.json; nulls are omitted so optional
/// members do not appear. This is the machine-readable evidence artifact (FR-009/010).
/// </summary>
public static class TraceJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    public static string Serialize(ExecutionTrace trace) =>
        JsonSerializer.Serialize(trace, Options);

    public static ExecutionTrace? Deserialize(string json) =>
        JsonSerializer.Deserialize<ExecutionTrace>(json, Options);
}
