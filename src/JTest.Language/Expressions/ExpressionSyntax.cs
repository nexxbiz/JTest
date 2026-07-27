using System.Text.Json;
using JTest.Language.Diagnostics;
using JTest.Language.Reading;

namespace JTest.Language.Expressions;

/// <summary>
/// Static syntactic checks for the two expression token forms:
/// <c>{{$.path}}</c> (context path) and <c>${NAME}</c> (process environment).
/// Full resolution semantics live in the engine; this scanner only rejects
/// tokens that can never be valid.
/// </summary>
public static class ExpressionSyntax
{
    /// <summary>Returns whether the string contains at least one context or environment token.</summary>
    /// <param name="value">The string to inspect.</param>
    public static bool ContainsToken(string value) =>
        value.Contains("{{", StringComparison.Ordinal) ||
        value.Contains("${", StringComparison.Ordinal);

    /// <summary>Validates every token in a single string value.</summary>
    /// <param name="value">The string to scan.</param>
    /// <param name="source">Document name for diagnostics.</param>
    /// <param name="jsonPointer">JSON pointer of the string for diagnostics.</param>
    /// <param name="sink">Diagnostic sink.</param>
    public static void ValidateString(
        string value,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        var index = 0;
        while (index < value.Length)
        {
            var open = value.IndexOf("{{", index, StringComparison.Ordinal);
            if (open < 0)
            {
                break;
            }

            var close = value.IndexOf("}}", open + 2, StringComparison.Ordinal);
            if (close < 0)
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.UnterminatedExpression,
                    "Expression token '{{' is never terminated with '}}'.",
                    source,
                    jsonPointer);
                return;
            }

            var body = value[(open + 2)..close].Trim();
            if (body.Length == 0)
            {
                Diag.Error(sink, DiagnosticCodes.EmptyExpressionPath, "Expression token has an empty path.", source, jsonPointer);
            }
            else if (!body.StartsWith("$.", StringComparison.Ordinal) || body.Length <= 2)
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.MalformedExpression,
                    $"Expression token '{{{{{body}}}}}' must start with '$.' followed by a path.",
                    source,
                    jsonPointer);
            }

            index = close + 2;
        }

        ValidateEnvironmentTokens(value, source, jsonPointer, sink);
    }

    /// <summary>Recursively validates every string inside a JSON value.</summary>
    /// <param name="element">The JSON value to scan.</param>
    /// <param name="source">Document name for diagnostics.</param>
    /// <param name="jsonPointer">JSON pointer of the value for diagnostics.</param>
    /// <param name="sink">Diagnostic sink.</param>
    public static void ValidateValue(
        JsonElement element,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                ValidateString(element.GetString() ?? string.Empty, source, jsonPointer, sink);
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    ValidateValue(
                        property.Value,
                        source,
                        $"{jsonPointer}/{PointerEscape(property.Name)}",
                        sink);
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    ValidateValue(item, source, $"{jsonPointer}/{index}", sink);
                    index++;
                }

                break;
            default:
                break;
        }
    }

    private static void ValidateEnvironmentTokens(
        string value,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        var index = 0;
        while (index < value.Length)
        {
            var open = value.IndexOf("${", index, StringComparison.Ordinal);
            if (open < 0)
            {
                return;
            }

            var close = value.IndexOf('}', open + 2);
            if (close < 0)
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.UnterminatedExpression,
                    "Environment token '${' is never terminated with '}'.",
                    source,
                    jsonPointer);
                return;
            }

            var name = value[(open + 2)..close];
            if (name.Length == 0 || !name.All(static c => char.IsAsciiLetterOrDigit(c) || c == '_'))
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.MalformedExpression,
                    $"Environment token '${{{name}}}' must name a variable using letters, digits, or underscores.",
                    source,
                    jsonPointer);
            }

            index = close + 1;
        }
    }

    private static string PointerEscape(string token) =>
        token.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
}
