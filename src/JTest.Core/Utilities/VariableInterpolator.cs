using Json.Path;
using JTest.Core.Exceptions;
using JTest.Core.Execution;
using JTest.Core.Variables;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace JTest.Core.Utilities;

/// <summary>
/// Static utility class for resolving variable tokens in strings using JSONPath expressions
/// </summary>
public static class VariableInterpolator
{
    // Updated regex to properly handle nested braces by counting brace pairs
    private static readonly Regex TokenRegex = new(@"\{\{\s*\$\.(?:[^{}]|\{[^{}]*\})*\s*\}\}", RegexOptions.Compiled);
    private static readonly Regex EnvironmentVariableRegex = new(@"\$\{([A-Za-z0-9_]+)\}", RegexOptions.Compiled);
    private const int MaxNestingDepth = 10; // Prevent infinite recursion

    /// <summary>
    /// Resolves variable tokens in the input string using the provided execution context
    /// Supports nested tokens by resolving from innermost to outermost
    /// </summary>
    public static object? ResolveVariableTokens(string input, IExecutionContext context)
    {
        return ResolveVariableTokensInternal(input, context, 0);
    }

    /// <summary>
    /// Resolves variable tokens and additionally reports every JSONPath in the input that matched
    /// nothing. An unresolved path is a diagnostic, not a value: callers surface it instead of
    /// letting it collapse to an empty string that looks like real data (FR-049).
    /// </summary>
    public static object? ResolveVariableTokens(string input, IExecutionContext context, out IReadOnlyList<string> unresolvedPaths)
    {
        var collected = new List<string>();
        var result = ResolveVariableTokensInternal(input, context, 0, collected);
        unresolvedPaths = collected;
        return result;
    }

    /// <summary>
    /// Internal recursive method for resolving variable tokens with depth tracking
    /// </summary>
    private static object? ResolveVariableTokensInternal(string input, IExecutionContext context, int depth, ICollection<string>? unresolved = null)
    {
        if (input == null) return string.Empty;

        // Prevent infinite recursion
        if (depth >= MaxNestingDepth)
        {
            throw new InvalidOperationException($"Warning: Maximum token resolution depth ({MaxNestingDepth}) reached for input: {input}");
        }

        input = ResolveEnvironmentVariableTokens(input);

        var matches = TokenRegex.Matches(input);
        if (matches.Count == 0) return input;

        // Check for single token first before resolving nested tokens
        if (IsSingleTokenInput(input, matches))
        {
            return ResolveSingleTokenRecursive(matches[0], context, depth, unresolved);
        }

        // Resolve nested tokens iteratively from innermost to outermost for multi-token strings
        var resolvedInput = ResolveNestedTokens(input, context, depth, unresolved);
        var newMatches = TokenRegex.Matches(resolvedInput);

        if (newMatches.Count == 0) return resolvedInput;
        return ResolveMultipleTokensRecursive(resolvedInput, newMatches, context, depth, unresolved);
    }

    private static string ResolveEnvironmentVariableTokens(string input)
    {
        var matches = EnvironmentVariableRegex.Matches(input);
        foreach (Match match in matches)
        {
            var envVarName = match.Groups[1].Value;
            var envVarValue = Environment.GetEnvironmentVariable(envVarName, EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(envVarValue))
            {
                input = input.Replace(match.Value, envVarValue);
            }
        }

        return input;
    }

    /// <summary>
    /// Resolves nested tokens by finding the innermost tokens first and working outward
    /// Uses a custom parser to properly handle nested braces
    /// </summary>
    private static string ResolveNestedTokens(string input, IExecutionContext context, int depth, ICollection<string>? unresolved = null)
    {
        var current = input;
        var iterationDepth = 0;

        while (iterationDepth < MaxNestingDepth)
        {
            var innerTokens = FindInnermostTokensWithProperNesting(current);
            if (innerTokens.Count == 0) break; // No more tokens to resolve

            var hasChanges = false;
            foreach (var token in innerTokens)
            {
                var path = ExtractPath(token);
                var resolvedValue = ResolveJsonPath(path, context, depth, unresolved);
                var replacement = ConvertToString(resolvedValue);

                // Check if the replacement itself contains tokens and resolve recursively
                if (replacement != token && TokenRegex.IsMatch(replacement))
                {
                    var recursiveResult = ResolveVariableTokensInternal(replacement, context, depth + 1, unresolved);
                    replacement = ConvertToString(recursiveResult);
                }

                if (replacement != token)
                {
                    current = current.Replace(token, replacement);
                    hasChanges = true;
                }
            }

            if (!hasChanges) break; // No tokens were resolved, avoid infinite loop
            iterationDepth++;
        }

        if (iterationDepth >= MaxNestingDepth)
        {
            throw new InvalidOperationException($"Warning: Maximum nesting iteration depth ({MaxNestingDepth}) reached while resolving tokens in: {input}");
        }

        return current;
    }

    /// <summary>
    /// Finds tokens with proper nested brace handling using a custom parser
    /// </summary>
    private static List<string> FindInnermostTokensWithProperNesting(string input)
    {
        var tokens = new List<string>();
        var i = 0;

        while (i < input.Length)
        {
            // Look for start of token
            if (i < input.Length - 1 && input[i] == '{' && input[i + 1] == '{')
            {
                var tokenStart = i;
                var tokenEnd = FindMatchingClosingBraces(input, i);

                if (tokenEnd != -1)
                {
                    var token = input.Substring(tokenStart, tokenEnd - tokenStart + 2);

                    // Check if this token starts with $. (our variable pattern)
                    if (token.TrimStart('{', ' ').StartsWith("$."))
                    {
                        // Check if this is an innermost token (doesn't contain other tokens)
                        if (IsInnermostTokenCustom(token))
                        {
                            tokens.Add(token);
                        }
                    }

                    i += 2; // Move past this token
                }
                else
                {
                    i++; // Move to next character if no matching closing braces
                }
            }
            else
            {
                i++;
            }
        }

        return tokens;
    }

    /// <summary>
    /// Finds the matching closing braces for a token starting at the given position
    /// </summary>
    private static int FindMatchingClosingBraces(string input, int start)
    {
        if (start >= input.Length - 1 || input[start] != '{' || input[start + 1] != '{')
            return -1;

        var braceCount = 1; // We've seen the opening {{
        var i = start + 2; // Start after the opening {{

        while (i < input.Length - 1 && braceCount > 0)
        {
            if (input[i] == '{' && input[i + 1] == '{')
            {
                braceCount++;
                i += 2;
            }
            else if (input[i] == '}' && input[i + 1] == '}')
            {
                braceCount--;
                if (braceCount == 0)
                {
                    return i; // Return position of first } in }}
                }
                i += 2;
            }
            else
            {
                i++;
            }
        }

        return -1; // No matching closing braces found
    }

    /// <summary>
    /// Checks if a token is innermost (doesn't contain other tokens within it)
    /// </summary>
    private static bool IsInnermostTokenCustom(string token)
    {
        // Remove the outer {{ and }} to check the content
        var content = token[2..^2];

        // Look for inner {{ patterns in the content
        return !content.Contains("{{");
    }


    private static object? ResolveSingleTokenRecursive(Match match, IExecutionContext context, int depth, ICollection<string>? unresolved = null)
    {
        var path = ExtractPath(match.Value);
        var result = ResolveJsonPath(path, context, depth, unresolved);

        // If the result is a string that contains tokens, resolve them recursively
        if (result is string stringResult && TokenRegex.IsMatch(stringResult))
        {
            return ResolveVariableTokensInternal(stringResult, context, depth + 1, unresolved);
        }

        return result;
    }

    private static string ResolveMultipleTokensRecursive(string input, MatchCollection matches, IExecutionContext context, int depth, ICollection<string>? unresolved = null)
    {
        var result = input;
        foreach (Match match in matches)
        {
            result = ReplaceTokenRecursive(result, match, context, depth, unresolved);
        }
        return result;
    }

    private static string ReplaceTokenRecursive(string input, Match match, IExecutionContext context, int depth, ICollection<string>? unresolved = null)
    {
        var path = ExtractPath(match.Value);
        var value = ResolveJsonPath(path, context, depth, unresolved);
        var replacement = ConvertToString(value);

        // Check if the replacement contains tokens and resolve recursively
        if (TokenRegex.IsMatch(replacement))
        {
            var recursiveResult = ResolveVariableTokensInternal(replacement, context, depth + 1, unresolved);
            replacement = ConvertToString(recursiveResult);
        }

        return input.Replace(match.Value, replacement);
    }

    // Keep the original methods for compatibility, but they now call the recursive versions
    private static bool IsSingleTokenInput(string input, MatchCollection matches)
    {
        return matches.Count == 1 && matches[0].Value == input;
    }

    private static string ExtractPath(string token)
    {
        return token.Trim('{', '}', ' ');
    }

    /// <summary>How a JSONPath resolved against the context.</summary>
    public enum PathResolution
    {
        /// <summary>The path matched a non-null value.</summary>
        Resolved,

        /// <summary>The path matched, and the matched value is null.</summary>
        MatchedNull,

        /// <summary>The path matched nothing (e.g. a casing mismatch). Not a value — a diagnostic (FR-049).</summary>
        MatchedNothing
    }

    /// <summary>
    /// Resolve a JSONPath and report whether it matched nothing, distinct from matching an actual
    /// null. Callers use this to emit a "path matched nothing" diagnostic instead of silently
    /// coercing an unresolved path to null (FR-049).
    /// </summary>
    public static (PathResolution Status, object? Value) TryResolveJsonPath(string path, IExecutionContext context)
    {
        try
        {
            var value = ExecuteJsonPath(path, context, 0);
            return (value is null ? PathResolution.MatchedNull : PathResolution.Resolved, value);
        }
        catch (JsonPathValueNotFoundException)
        {
            return (PathResolution.MatchedNothing, null);
        }
    }

    /// <summary>
    /// Resolves <c>$.now</c> / <c>$.random</c>, which are computed rather than looked up. Real context
    /// data wins: if a suite has saved something under that root, the stored value is used, so adding
    /// these built-ins cannot change the meaning of an existing suite. An unknown field under a
    /// built-in root throws rather than falling through to a path that matches nothing — naming the
    /// mistake beats reporting "matched nothing" for a value that never could have existed.
    /// </summary>
    private static bool TryResolveBuiltIn(string path, IExecutionContext context, out object? value)
    {
        value = null;

        if (!BuiltInVariables.TryResolve(path, out var resolved, out var error))
        {
            if (error is null) return false;

            // Only complain when the root is genuinely the built-in (not shadowed by real data).
            if (ContextDefinesBuiltInRoot(path, context)) return false;

            throw new InvalidOperationException(error);
        }

        if (ContextDefinesBuiltInRoot(path, context)) return false;

        value = resolved ?? string.Empty;
        return true;
    }

    private static bool ContextDefinesBuiltInRoot(string path, IExecutionContext context)
    {
        var trimmed = path.Trim();
        if (!trimmed.StartsWith("$.", StringComparison.Ordinal)) return false;

        var root = trimmed[2..].Split('.')[0];
        return context.Variables.ContainsKey(root);
    }

    private static object? ResolveJsonPath(string path, IExecutionContext context, int depth, ICollection<string>? unresolved = null)
    {
        try
        {
            return ExecuteJsonPath(path, context, depth);
        }
        catch (JsonPathValueNotFoundException)
        {
            unresolved?.Add(path);
            return null;
        }
        catch (Exception e) 
        {
            throw new InvalidOperationException($"Failed to execute JSONPath '{path}'. Error: {e.Message}");
        }
    }

    private static object ExecuteJsonPath(string path, IExecutionContext context, int depth)
    {
        if (TryResolveBuiltIn(path, context, out var builtIn))
        {
            return builtIn!;
        }

        var jsonPath = JsonPath.Parse(path);
        var jsonNode = JsonSerializer.SerializeToNode(context.Variables);
        var result = jsonPath.Evaluate(jsonNode);
        if (result.Matches == null || result.Matches.Count == 0)
            throw new JsonPathValueNotFoundException(path);

        // If there's only one match, return the single value (preserves existing behavior)
        if (result.Matches.Count == 1)
        {
            return ExtractValue(result.Matches.First().Value, context, depth);
        }

        // If there are multiple matches, return an array of all extracted values
        var extractedValues = new List<object>();
        foreach (var match in result.Matches)
        {
            extractedValues.Add(ExtractValue(match.Value, context, depth));
        }
        return extractedValues.ToArray();
    }

    private static object ExtractValue(object? value, IExecutionContext context, int depth)
    {
        return value switch
        {
            JsonNode node => ExtractFromJsonNode(node, context, depth),
            JsonElement element => ExtractFromJsonElement(element),
            _ => value ?? string.Empty
        };
    }

    private static object ExtractFromJsonNode(JsonNode node, IExecutionContext context, int depth)
    {
        return node switch
        {
            JsonValue value => ExtractPrimitiveValue(value),
            JsonObject jsonObj => ResolveTokensInJsonObject(jsonObj, context, depth),
            JsonArray jsonArray => ResolveTokensInJsonArray(jsonArray, context, depth),
            _ => node.ToString()
        };
    }

    private static object ExtractPrimitiveValue(JsonValue value)
    {
        try
        {
            if (value.TryGetValue<string>(out var stringVal))
            {
                return ExtractStringValue(stringVal);
            }
            if (value.TryGetValue<int>(out var intVal))
                return intVal;
            if (value.TryGetValue<double>(out var doubleVal))
                return doubleVal;
            if (value.TryGetValue<bool>(out var boolVal))
                return boolVal;
            return value.GetValue<object>();
        }
        catch
        {
            return value.ToString();
        }
    }

    private static string ExtractStringValue(string value)
    {
        return ResolveEnvironmentVariableTokens(value);
    }

    /// <summary>
    /// Recursively resolves tokens in a JsonObject by converting it to a Dictionary and resolving each value
    /// </summary>
    private static Dictionary<string, object?> ResolveTokensInJsonObject(JsonObject jsonObj, IExecutionContext context, int depth)
    {
        // Prevent infinite recursion
        if (depth >= MaxNestingDepth)
        {
            throw new InvalidOperationException($"Warning: Maximum token resolution depth ({MaxNestingDepth}) reached while resolving JsonObject");            
        }

        var resolvedDict = new Dictionary<string, object?>();

        foreach (var kvp in jsonObj)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            if (value == null)
            {
                resolvedDict[key] = null!;
                continue;
            }

            // Recursively resolve the value
            var resolvedValue = ExtractValue(value, context, depth);

            // If the resolved value is a string containing tokens, resolve those too
            if (resolvedValue is string stringValue && TokenRegex.IsMatch(stringValue))
            {
                resolvedValue = ResolveVariableTokensInternal(stringValue, context, depth + 1);
            }

            resolvedDict[key] = resolvedValue;
        }

        return resolvedDict;
    }

    /// <summary>
    /// Recursively resolves tokens in a JsonArray by converting it to an array and resolving each element
    /// </summary>
    private static object?[] ResolveTokensInJsonArray(JsonArray jsonArray, IExecutionContext context, int depth)
    {
        // Prevent infinite recursion
        if (depth >= MaxNestingDepth)
        {
            throw new InvalidOperationException($"Warning: Maximum token resolution depth ({MaxNestingDepth}) reached while resolving JsonArray");            
        }

        var resolvedList = new List<object?>();

        foreach (var element in jsonArray)
        {
            if (element == null)
            {
                resolvedList.Add(null!);
                continue;
            }

            // Recursively resolve the element
            var resolvedValue = ExtractValue(element, context, depth);

            // If the resolved value is a string containing tokens, resolve those too
            if (resolvedValue is string stringValue && TokenRegex.IsMatch(stringValue))
            {
                resolvedValue = ResolveVariableTokensInternal(stringValue, context, depth + 1);
            }

            resolvedList.Add(resolvedValue);
        }

        return [.. resolvedList];
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

    /// <summary>
    /// The user-facing diagnostic for a JSONPath that matched nothing (FR-049), including a hint for
    /// the JavaScript-isms authors most often reach for — they resolve to nothing rather than
    /// erroring, so the resulting failure otherwise looks like a data problem.
    /// </summary>
    public static string DescribeUnresolvedPath(string path)
    {
        var message = $"JSONPath '{path}' matched nothing: the path does not exist (distinct from matching an actual null).";
        var hint = HintForUnresolvedPath(path);

        return hint is null ? message : $"{message} {hint}";
    }

    private static string? HintForUnresolvedPath(string path)
    {
        var trimmed = path.TrimEnd();

        if (trimmed.EndsWith(".length", StringComparison.Ordinal))
        {
            return "'.length' is a JavaScript-ism; RFC 9535 JSONPath has no length property. " +
                   "Use the 'length' assertion operator against the collection itself instead.";
        }

        if (trimmed.EndsWith(".count", StringComparison.Ordinal) || trimmed.EndsWith(".size", StringComparison.Ordinal))
        {
            return "RFC 9535 JSONPath has no count/size property. " +
                   "Use the 'length' assertion operator against the collection itself instead.";
        }

        return "Check the spelling and the casing — JSONPath name selectors are case-sensitive.";
    }

    private static string ConvertToString(object? value)
    {
        if (value is null) return string.Empty;

        // Use invariant culture for numeric types to ensure consistent decimal formatting
        return value switch
        {
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
