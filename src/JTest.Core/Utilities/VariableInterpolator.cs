using Json.Path;
using JTest.Core.Execution;
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
    private static readonly Regex EnvironmentVariableRegex = new(@"^\$\{([A-Za-z0-9_]+)\}$", RegexOptions.Compiled);
    private const int MaxNestingDepth = 10; // Prevent infinite recursion

    /// <summary>
    /// Resolves variable tokens in the input string using the provided execution context
    /// Supports nested tokens by resolving from innermost to outermost
    /// </summary>
    public static object ResolveVariableTokens(string input, IExecutionContext context)
    {
        return ResolveVariableTokensInternal(input, context, 0);
    }

    /// <summary>
    /// Internal recursive method for resolving variable tokens with depth tracking
    /// </summary>
    private static object ResolveVariableTokensInternal(string input, IExecutionContext context, int depth)
    {
        if (input == null) return string.Empty;

        // Prevent infinite recursion
        if (depth >= MaxNestingDepth)
        {
            context.Log.Add($"Warning: Maximum token resolution depth ({MaxNestingDepth}) reached for input: {input}");
            return input;
        }

        var matches = TokenRegex.Matches(input);
        if (matches.Count == 0) return input;

        // Check for single token first before resolving nested tokens
        if (IsSingleTokenInput(input, matches))
        {
            return ResolveSingleTokenRecursive(matches[0], context, depth);
        }

        // Resolve nested tokens iteratively from innermost to outermost for multi-token strings
        var resolvedInput = ResolveNestedTokens(input, context, depth);
        var newMatches = TokenRegex.Matches(resolvedInput);

        if (newMatches.Count == 0) return resolvedInput;
        return ResolveMultipleTokensRecursive(resolvedInput, newMatches, context, depth);
    }

    /// <summary>
    /// Resolves nested tokens by finding the innermost tokens first and working outward
    /// Uses a custom parser to properly handle nested braces
    /// </summary>
    private static string ResolveNestedTokens(string input, IExecutionContext context, int depth)
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
                var resolvedValue = ResolveJsonPath(path, context, depth);
                var replacement = ConvertToString(resolvedValue);

                // Check if the replacement itself contains tokens and resolve recursively
                if (replacement != token && TokenRegex.IsMatch(replacement))
                {
                    var recursiveResult = ResolveVariableTokensInternal(replacement, context, depth + 1);
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
            context.Log.Add($"Warning: Maximum nesting iteration depth ({MaxNestingDepth}) reached while resolving tokens in: {input}");
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
        var content = token.Substring(2, token.Length - 4);

        // Look for inner {{ patterns in the content
        var innerStart = content.IndexOf("{{");
        return innerStart == -1; // If no inner {{ found, it's innermost
    }

    /// <summary>
    /// Finds innermost tokens that don't contain other tokens within them
    /// (Legacy method kept for compatibility with simpler cases)
    /// </summary>
    private static List<Match> FindInnermostTokens(string input)
    {
        var allMatches = TokenRegex.Matches(input).Cast<Match>().ToList();
        var innermostTokens = new List<Match>();

        foreach (var match in allMatches)
        {
            if (IsInnermostToken(match, allMatches))
            {
                innermostTokens.Add(match);
            }
        }

        return innermostTokens;
    }

    /// <summary>
    /// Determines if a token is innermost by checking if no other tokens are contained within it
    /// (Legacy method kept for compatibility)
    /// </summary>
    private static bool IsInnermostToken(Match candidate, List<Match> allMatches)
    {
        var candidateStart = candidate.Index;
        var candidateEnd = candidate.Index + candidate.Length;

        // Check if any other token is completely contained within this token
        foreach (var other in allMatches)
        {
            if (other == candidate) continue;

            var otherStart = other.Index;
            var otherEnd = other.Index + other.Length;

            // If another token is completely inside this one, this is not innermost
            if (otherStart > candidateStart && otherEnd < candidateEnd)
            {
                return false;
            }
        }

        return true;
    }

    private static object ResolveSingleTokenRecursive(Match match, IExecutionContext context, int depth)
    {
        var path = ExtractPath(match.Value);
        var result = ResolveJsonPath(path, context, depth);

        // If the result is a string that contains tokens, resolve them recursively
        if (result is string stringResult && TokenRegex.IsMatch(stringResult))
        {
            return ResolveVariableTokensInternal(stringResult, context, depth + 1);
        }

        return result;
    }

    private static string ResolveMultipleTokensRecursive(string input, MatchCollection matches, IExecutionContext context, int depth)
    {
        var result = input;
        foreach (Match match in matches)
        {
            result = ReplaceTokenRecursive(result, match, context, depth);
        }
        return result;
    }

    private static string ReplaceTokenRecursive(string input, Match match, IExecutionContext context, int depth)
    {
        var path = ExtractPath(match.Value);
        var value = ResolveJsonPath(path, context, depth);
        var replacement = ConvertToString(value);

        // Check if the replacement contains tokens and resolve recursively
        if (TokenRegex.IsMatch(replacement))
        {
            var recursiveResult = ResolveVariableTokensInternal(replacement, context, depth + 1);
            replacement = ConvertToString(recursiveResult);
        }

        return input.Replace(match.Value, replacement);
    }

    // Keep the original methods for compatibility, but they now call the recursive versions
    private static bool IsSingleTokenInput(string input, MatchCollection matches)
    {
        return matches.Count == 1 && matches[0].Value == input;
    }

    private static object ResolveSingleToken(Match match, IExecutionContext context)
    {
        return ResolveSingleTokenRecursive(match, context, 0);
    }

    private static string ResolveMultipleTokens(string input, MatchCollection matches, IExecutionContext context)
    {
        return ResolveMultipleTokensRecursive(input, matches, context, 0);
    }

    private static string ReplaceToken(string input, Match match, IExecutionContext context)
    {
        return ReplaceTokenRecursive(input, match, context, 0);
    }

    private static string ExtractPath(string token)
    {
        return token.Trim('{', '}', ' ');
    }

    private static object ResolveJsonPath(string path, IExecutionContext context)
    {
        try { return ExecuteJsonPath(path, context, 0); }
        catch (Exception) { LogPathError(path, context); return string.Empty; }
    }

    private static object ResolveJsonPath(string path, IExecutionContext context, int depth)
    {
        try { return ExecuteJsonPath(path, context, depth); }
        catch (Exception) { LogPathError(path, context); return string.Empty; }
    }

    private static object ExecuteJsonPath(string path, IExecutionContext context, int depth)
    {
        var jsonPath = JsonPath.Parse(path);
        var jsonNode = JsonSerializer.SerializeToNode(context.Variables);
        var result = jsonPath.Evaluate(jsonNode);
        if (result.Matches == null || !result.Matches.Any()) return HandleMissingPath(path, context);
        
        // If there's only one match, return the single value (preserves existing behavior)
        if (result.Matches.Count() == 1)
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
        var environmentVariableTokenMatch = EnvironmentVariableRegex.Match(value);
        if (!environmentVariableTokenMatch.Success)
        {
            return value;
        }

        var environmentVariableName = environmentVariableTokenMatch.Groups[1].Value;
        var result = Environment.GetEnvironmentVariable(environmentVariableName);
        if(!string.IsNullOrWhiteSpace(result))
        {
            return result;
        }

        return value;
    }

    /// <summary>
    /// Recursively resolves tokens in a JsonObject by converting it to a Dictionary and resolving each value
    /// </summary>
    private static object ResolveTokensInJsonObject(JsonObject jsonObj, IExecutionContext context, int depth)
    {
        // Prevent infinite recursion
        if (depth >= MaxNestingDepth)
        {
            context.Log.Add($"Warning: Maximum token resolution depth ({MaxNestingDepth}) reached while resolving JsonObject");
            return jsonObj;
        }

        var resolvedDict = new Dictionary<string, object>();

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
    private static object ResolveTokensInJsonArray(JsonArray jsonArray, IExecutionContext context, int depth)
    {
        // Prevent infinite recursion
        if (depth >= MaxNestingDepth)
        {
            context.Log.Add($"Warning: Maximum token resolution depth ({MaxNestingDepth}) reached while resolving JsonArray");
            return jsonArray;
        }

        var resolvedList = new List<object>();

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

        return resolvedList.ToArray();
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
        if (value == null) return string.Empty;

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
