using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JTest.Engine.Diagnostics;
using JTest.Language.Diagnostics;

namespace JTest.Engine.Expressions;

/// <summary>
/// Load-time substitution of <c>${NAME}</c> tokens inside the suite's
/// <c>env</c> and <c>globals</c> value maps — the only places the language
/// permits them. Undefined variables fail closed, and every substituted
/// value is reported so it can be marked sensitive.
/// </summary>
public static class EnvironmentTokenSubstitution
{
    /// <summary>Substitutes every token in the map's string values.</summary>
    /// <param name="values">The bound value map.</param>
    /// <param name="lookup">Process-environment lookup.</param>
    /// <param name="source">Document name for diagnostics.</param>
    /// <param name="pointerBase">JSON pointer of the map, e.g. <c>/env</c>.</param>
    public static EnvironmentSubstitutionResult Substitute(
        IReadOnlyDictionary<string, JsonElement> values,
        Func<string, string?> lookup,
        string source,
        string pointerBase)
    {
        var result = new JsonObject();
        var substituted = new List<string>();
        var diagnostics = new List<LanguageDiagnostic>();

        foreach (var entry in values)
        {
            var jsonPointer = $"{pointerBase}/{Escape(entry.Key)}";
            result[entry.Key] = SubstituteValue(entry.Value, lookup, source, jsonPointer, substituted, diagnostics);
        }

        return new EnvironmentSubstitutionResult(result, substituted, diagnostics);
    }

    private static JsonNode? SubstituteValue(
        JsonElement element,
        Func<string, string?> lookup,
        string source,
        string jsonPointer,
        List<string> substituted,
        List<LanguageDiagnostic> diagnostics)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var text = element.GetString() ?? string.Empty;
                if (!text.Contains("${", StringComparison.Ordinal))
                {
                    return JsonValue.Create(text);
                }

                substituted.Add(jsonPointer);
                return JsonValue.Create(SubstituteString(text, lookup, source, jsonPointer, diagnostics));

            case JsonValueKind.Object:
                var resolvedObject = new JsonObject();
                foreach (var property in element.EnumerateObject())
                {
                    resolvedObject[property.Name] = SubstituteValue(
                        property.Value, lookup, source, $"{jsonPointer}/{Escape(property.Name)}", substituted, diagnostics);
                }

                return resolvedObject;

            case JsonValueKind.Array:
                var resolvedArray = new JsonArray();
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    resolvedArray.Add(SubstituteValue(
                        item, lookup, source, $"{jsonPointer}/{index}", substituted, diagnostics));
                    index++;
                }

                return resolvedArray;

            default:
                return JsonElementNodes.ToNode(element);
        }
    }

    private static string SubstituteString(
        string text,
        Func<string, string?> lookup,
        string source,
        string jsonPointer,
        List<LanguageDiagnostic> diagnostics)
    {
        var builder = new StringBuilder();
        var position = 0;
        while (position < text.Length)
        {
            var start = text.IndexOf("${", position, StringComparison.Ordinal);
            if (start < 0)
            {
                builder.Append(text, position, text.Length - position);
                break;
            }

            var end = text.IndexOf('}', start + 2);
            if (end < 0)
            {
                builder.Append(text, position, text.Length - position);
                break;
            }

            builder.Append(text, position, start - position);
            var name = text[(start + 2)..end];
            var value = lookup(name);
            if (value is null)
            {
                diagnostics.Add(new LanguageDiagnostic(
                    RuntimeDiagnosticCodes.UndefinedEnvironmentVariable,
                    DiagnosticSeverity.Error,
                    $"Process environment variable '{name}' is not defined.",
                    source,
                    jsonPointer));
            }
            else
            {
                builder.Append(value);
            }

            position = end + 1;
        }

        return builder.ToString();
    }

    private static string Escape(string token) =>
        token.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
}
