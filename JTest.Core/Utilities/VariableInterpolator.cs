using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Path;
using JTest.Core.Execution;

namespace JTest.Core.Utilities;

/// <summary>
/// Static utility class for resolving variable tokens in strings using JSONPath expressions
/// </summary>
public static class VariableInterpolator
{
    private static readonly Regex TokenRegex = new(@"\{\{\s*\$\.[^}]+\s*\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Resolves variable tokens in the input string using the provided execution context
    /// </summary>
    public static object ResolveVariableTokens(string input, IExecutionContext context)
    {
        if (input == null) return string.Empty;
        var matches = TokenRegex.Matches(input);
        if (matches.Count == 0) return input;
        if (IsSingleTokenInput(input, matches)) return ResolveSingleToken(matches[0], context);
        return ResolveMultipleTokens(input, matches, context);
    }

    private static bool IsSingleTokenInput(string input, MatchCollection matches)
    {
        return matches.Count == 1 && matches[0].Value == input;
    }

    private static object ResolveSingleToken(Match match, IExecutionContext context)
    {
        var path = ExtractPath(match.Value);
        return ResolveJsonPath(path, context);
    }

    private static string ResolveMultipleTokens(string input, MatchCollection matches, IExecutionContext context)
    {
        var result = input;
        foreach (Match match in matches) result = ReplaceToken(result, match, context);
        return result;
    }

    private static string ReplaceToken(string input, Match match, IExecutionContext context)
    {
        var path = ExtractPath(match.Value);
        var value = ResolveJsonPath(path, context);
        return input.Replace(match.Value, ConvertToString(value));
    }

    private static string ExtractPath(string token)
    {
        return token.Trim('{', '}', ' ');
    }

    private static object ResolveJsonPath(string path, IExecutionContext context)
    {
        try { return ExecuteJsonPath(path, context); }
        catch (Exception) { LogPathError(path, context); return string.Empty; }
    }

    private static object ExecuteJsonPath(string path, IExecutionContext context)
    {
        var jsonPath = JsonPath.Parse(path);
        var jsonNode = JsonSerializer.SerializeToNode(context.Variables);
        var result = jsonPath.Evaluate(jsonNode);
        if (result.Matches == null || !result.Matches.Any()) return HandleMissingPath(path, context);
        return ExtractValue(result.Matches.First().Value);
    }

    private static object ExtractValue(object? value)
    {
        return value switch
        {
            JsonNode node => ExtractFromJsonNode(node),
            JsonElement element => ExtractFromJsonElement(element),
            _ => value ?? string.Empty
        };
    }

    private static object ExtractFromJsonNode(JsonNode node)
    {
        return node switch
        {
            JsonValue value => ExtractPrimitiveValue(value),
            JsonObject => node,
            JsonArray => node,
            _ => node.ToString()
        };
    }

    private static object ExtractPrimitiveValue(JsonValue value)
    {
        try
        {
            if (value.TryGetValue<string>(out var stringVal)) return stringVal;
            if (value.TryGetValue<int>(out var intVal)) return intVal;
            if (value.TryGetValue<double>(out var doubleVal)) return doubleVal;
            if (value.TryGetValue<bool>(out var boolVal)) return boolVal;
            return value.GetValue<object>();
        }
        catch
        {
            return value.ToString();
        }
    }

    private static object ExtractFromJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => string.Empty,
            _ => element
        };
    }

    private static string HandleMissingPath(string path, IExecutionContext context)
    {
        LogPathError(path, context);
        return string.Empty;
    }

    private static void LogPathError(string path, IExecutionContext context)
    {
        context.Log.Add($"Warning: JSONPath '{path}' not found in variables");
    }

    private static string ConvertToString(object value)
    {
        return value?.ToString() ?? string.Empty;
    }
}