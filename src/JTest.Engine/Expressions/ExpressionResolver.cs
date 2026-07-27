using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Path;
using JTest.Engine.Contexts;
using JTest.Engine.Diagnostics;
using JTest.Language.Diagnostics;

namespace JTest.Engine.Expressions;

/// <summary>
/// Deterministic, fail-closed resolution of <c>{{$.path}}</c> tokens against
/// an execution frame. A path that resolves to nothing is an error, never a
/// silent null. Substitution is position-exact, and resolved values are
/// never re-interpreted as expressions.
/// </summary>
public static class ExpressionResolver
{
    /// <summary>Resolves every token in one string.</summary>
    /// <param name="template">The string, possibly containing tokens.</param>
    /// <param name="frame">The executing frame.</param>
    /// <param name="source">Document name for diagnostics.</param>
    public static ResolutionResult ResolveString(string template, ExecutionFrame frame, string source)
    {
        var open = template.IndexOf("{{", StringComparison.Ordinal);
        if (open < 0)
        {
            return ResolutionResult.Ok(JsonValue.Create(template));
        }

        var close = template.IndexOf("}}", open + 2, StringComparison.Ordinal);
        if (close < 0)
        {
            return ResolutionResult.Ok(JsonValue.Create(template));
        }

        if (open == 0 && close == template.Length - 2)
        {
            return ResolvePath(template[2..^2].Trim(), frame, source);
        }

        var builder = new StringBuilder();
        var position = 0;
        while (position < template.Length)
        {
            var start = template.IndexOf("{{", position, StringComparison.Ordinal);
            if (start < 0)
            {
                builder.Append(template, position, template.Length - position);
                break;
            }

            var end = template.IndexOf("}}", start + 2, StringComparison.Ordinal);
            if (end < 0)
            {
                builder.Append(template, position, template.Length - position);
                break;
            }

            builder.Append(template, position, start - position);
            var resolved = ResolvePath(template[(start + 2)..end].Trim(), frame, source);
            if (!resolved.Success)
            {
                return resolved;
            }

            builder.Append(Stringify(resolved.Value));
            position = end + 2;
        }

        return ResolutionResult.Ok(JsonValue.Create(builder.ToString()));
    }

    /// <summary>Recursively resolves every token inside one JSON value.</summary>
    /// <param name="template">The value template.</param>
    /// <param name="frame">The executing frame.</param>
    /// <param name="source">Document name for diagnostics.</param>
    public static ResolutionResult ResolveValue(JsonElement template, ExecutionFrame frame, string source)
    {
        switch (template.ValueKind)
        {
            case JsonValueKind.String:
                return ResolveString(template.GetString() ?? string.Empty, frame, source);

            case JsonValueKind.Object:
                var resolvedObject = new JsonObject();
                foreach (var property in template.EnumerateObject())
                {
                    var resolved = ResolveValue(property.Value, frame, source);
                    if (!resolved.Success)
                    {
                        return resolved;
                    }

                    resolvedObject[property.Name] = resolved.Value;
                }

                return ResolutionResult.Ok(resolvedObject);

            case JsonValueKind.Array:
                var resolvedArray = new JsonArray();
                foreach (var item in template.EnumerateArray())
                {
                    var resolved = ResolveValue(item, frame, source);
                    if (!resolved.Success)
                    {
                        return resolved;
                    }

                    resolvedArray.Add(resolved.Value);
                }

                return ResolutionResult.Ok(resolvedArray);

            default:
                return ResolutionResult.Ok(JsonElementNodes.ToNode(template));
        }
    }

    /// <summary>Resolves one bare token path such as <c>$.env.baseUrl</c>.</summary>
    /// <param name="path">The token body, starting with <c>$.</c>.</param>
    /// <param name="frame">The executing frame.</param>
    /// <param name="source">Document name for diagnostics.</param>
    public static ResolutionResult ResolvePath(string path, ExecutionFrame frame, string source)
    {
        var body = path[2..];
        var nameLength = 0;
        while (nameLength < body.Length && body[nameLength] != '.' && body[nameLength] != '[')
        {
            nameLength++;
        }

        var name = body[..nameLength];
        var rest = body[nameLength..];

        JsonNode? root;
        if (!frame.TryResolveScope(name, out root) && !frame.TryResolveName(name, out root))
        {
            return Unresolvable(path, $"'{name}' is not a scope, binding, or step id visible in this frame.", source);
        }

        if (rest.Length == 0)
        {
            return ResolutionResult.Ok(root?.DeepClone());
        }

        if (root is null)
        {
            return Unresolvable(path, $"'{name}' has no value to navigate into.", source);
        }

        JsonPath jsonPath;
        try
        {
            jsonPath = JsonPath.Parse("$" + rest);
        }
        catch (PathParseException exception)
        {
            return Unresolvable(path, $"the path is not valid JSONPath: {exception.Message}", source);
        }

        var matches = jsonPath.Evaluate(root).Matches;
        if (matches is null || matches.Count == 0)
        {
            return Unresolvable(path, "the path matched nothing.", source);
        }

        if (matches.Count == 1)
        {
            return ResolutionResult.Ok(matches[0].Value?.DeepClone());
        }

        var array = new JsonArray();
        foreach (var match in matches)
        {
            array.Add(match.Value?.DeepClone());
        }

        return ResolutionResult.Ok(array);
    }

    /// <summary>Renders a resolved value into a larger string, invariant culture.</summary>
    /// <param name="value">The resolved value.</param>
    public static string Stringify(JsonNode? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is JsonValue primitive)
        {
            if (primitive.TryGetValue<string>(out var text))
            {
                return text;
            }

            if (primitive.GetValueKind() == JsonValueKind.Number)
            {
                return primitive.TryGetValue<long>(out var integral)
                    ? integral.ToString(CultureInfo.InvariantCulture)
                    : primitive.GetValue<double>().ToString(CultureInfo.InvariantCulture);
            }

            if (primitive.TryGetValue<bool>(out var flag))
            {
                return flag ? "true" : "false";
            }
        }

        return value.ToJsonString();
    }

    private static ResolutionResult Unresolvable(string path, string reason, string source) =>
        ResolutionResult.Fail(new LanguageDiagnostic(
            RuntimeDiagnosticCodes.UnresolvableExpression,
            DiagnosticSeverity.Error,
            $"Expression '{{{{{path}}}}}' cannot be resolved: {reason}",
            source,
            string.Empty));
}
