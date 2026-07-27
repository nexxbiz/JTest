using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using JTest.Engine.Contexts;
using JTest.Engine.Expressions;
using JTest.Language.Documents;

namespace JTest.Engine.Assertions;

/// <summary>
/// Static evaluation of the closed assertion-operator set with
/// invariant-culture comparison rules. An unresolvable operand fails the
/// assertion with the resolution diagnostic — never silently.
/// </summary>
public static class AssertionEvaluator
{
    /// <summary>Evaluates one assertion against the frame.</summary>
    /// <param name="assertion">The bound assertion.</param>
    /// <param name="frame">The executing frame.</param>
    /// <param name="source">Document name for diagnostics.</param>
    public static AssertionOutcome Evaluate(AssertionDefinition assertion, ExecutionFrame frame, string source)
    {
        JsonNode? actual = null;
        var actualResolved = false;
        if (assertion.Actual is not null)
        {
            var resolution = ExpressionResolver.ResolveValue(assertion.Actual.Value, frame, source);
            if (resolution.Success)
            {
                actual = resolution.Value;
                actualResolved = true;
            }
            else if (assertion.Operator is not ("exists" or "notExists" or "empty"))
            {
                return new AssertionOutcome(
                    false, assertion.Operator, null, null, assertion.Description, resolution.Diagnostic!.Message);
            }
        }

        JsonNode? expected = null;
        if (assertion.Expected is not null)
        {
            var resolution = ExpressionResolver.ResolveValue(assertion.Expected.Value, frame, source);
            if (!resolution.Success)
            {
                return new AssertionOutcome(
                    false, assertion.Operator, actual, null, assertion.Description, resolution.Diagnostic!.Message);
            }

            expected = resolution.Value;
        }

        var passed = Check(assertion.Operator, actual, actualResolved, expected);
        var message = passed
            ? string.Empty
            : $"Expected {assertion.Operator} to hold; actual was {Describe(actual, actualResolved)} and expected operand was {Describe(expected, true)}.";

        return new AssertionOutcome(passed, assertion.Operator, actual, expected, assertion.Description, message);
    }

    private static bool Check(string op, JsonNode? actual, bool actualResolved, JsonNode? expected) => op switch
    {
        "equals" => JsonNode.DeepEquals(actual, expected),
        "notEquals" => !JsonNode.DeepEquals(actual, expected),
        "exists" => actualResolved,
        "notExists" => !actualResolved,
        "contains" => Contains(actual, expected),
        "notContains" => !Contains(actual, expected),
        "greaterThan" => Compare(actual, expected) is > 0,
        "lessThan" => Compare(actual, expected) is < 0,
        "greaterOrEqual" => Compare(actual, expected) is >= 0,
        "lessOrEqual" => Compare(actual, expected) is <= 0,
        "between" => Between(actual, expected),
        "in" => In(actual, expected),
        "matches" => Matches(actual, expected),
        "startsWith" => StringPair(actual, expected, static (a, e) => a.StartsWith(e, StringComparison.Ordinal)),
        "endsWith" => StringPair(actual, expected, static (a, e) => a.EndsWith(e, StringComparison.Ordinal)),
        "length" => LengthOf(actual) is { } length && expected is not null &&
                    TryNumber(expected, out var expectedLength) && length == expectedLength,
        "empty" => !actualResolved || LengthOf(actual) == 0,
        "notEmpty" => actualResolved && LengthOf(actual) > 0,
        "type" => TypeName(actual) == expected?.GetValue<string>(),
        _ => false,
    };

    private static bool Contains(JsonNode? actual, JsonNode? expected)
    {
        if (actual is JsonArray array)
        {
            return array.Any(item => JsonNode.DeepEquals(item, expected));
        }

        if (actual?.GetValueKind() == JsonValueKind.String && expected?.GetValueKind() == JsonValueKind.String)
        {
            return actual.GetValue<string>().Contains(expected.GetValue<string>(), StringComparison.Ordinal);
        }

        return false;
    }

    private static int? Compare(JsonNode? actual, JsonNode? expected)
    {
        if (TryNumber(actual, out var a) && TryNumber(expected, out var b))
        {
            return a.CompareTo(b);
        }

        return null;
    }

    private static bool Between(JsonNode? actual, JsonNode? expected)
    {
        if (expected is not JsonArray range || range.Count != 2)
        {
            return false;
        }

        return TryNumber(actual, out var value) &&
               TryNumber(range[0], out var min) &&
               TryNumber(range[1], out var max) &&
               value >= min && value <= max;
    }

    private static bool In(JsonNode? actual, JsonNode? expected) =>
        expected is JsonArray options && options.Any(option => JsonNode.DeepEquals(actual, option));

    private static bool Matches(JsonNode? actual, JsonNode? expected)
    {
        if (actual?.GetValueKind() != JsonValueKind.String || expected?.GetValueKind() != JsonValueKind.String)
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(
                actual.GetValue<string>(),
                expected.GetValue<string>(),
                RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(1));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool StringPair(JsonNode? actual, JsonNode? expected, Func<string, string, bool> check) =>
        actual?.GetValueKind() == JsonValueKind.String &&
        expected?.GetValueKind() == JsonValueKind.String &&
        check(actual.GetValue<string>(), expected.GetValue<string>());

    private static int? LengthOf(JsonNode? value) => value switch
    {
        JsonArray array => array.Count,
        JsonObject jsonObject => jsonObject.Count,
        JsonValue primitive when primitive.GetValueKind() == JsonValueKind.String =>
            primitive.GetValue<string>().Length,
        _ => null,
    };

    private static bool TryNumber(JsonNode? value, out double number)
    {
        number = 0;
        if (value is null)
        {
            return false;
        }

        if (value.GetValueKind() == JsonValueKind.Number)
        {
            number = value.GetValue<double>();
            return true;
        }

        return value.GetValueKind() == JsonValueKind.String &&
               double.TryParse(value.GetValue<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }

    private static string TypeName(JsonNode? value) => value switch
    {
        null => "null",
        JsonObject => "object",
        JsonArray => "array",
        _ => value.GetValueKind() switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Null => "null",
            _ => "unknown",
        },
    };

    private static string Describe(JsonNode? value, bool resolved) =>
        !resolved ? "unresolvable" : value?.ToJsonString() ?? "null";
}
