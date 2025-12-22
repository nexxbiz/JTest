using System.Collections;
using System.Text.Json;

namespace JTest.Core.Assertions;

/// <summary>
/// Type assertion to check the type of a value
/// </summary>
public sealed class TypeAssertion(object? actualValue, object? expectedValue, string? description = null, bool? mask = null)
    : AssertionOperationBase(actualValue, expectedValue, description, mask)
{
    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected type '{resolvedExpectedValue}' but got '{GetValueType(resolvedActualValue)}'";
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        if (ExpectedValue == null)
        {
            return false;
        }

        var expectedType = $"{resolvedExpectedValue}".ToLowerInvariant();
        var actualType = GetValueType(resolvedActualValue);

        return string.Equals(actualType, expectedType, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetValueType(object? value)
    {
        return value switch
        {
            null => "null",
            bool => "boolean",
            int or long or short or byte or sbyte or uint or ulong or ushort => "integer",
            float or double or decimal => "number",
            string => "string",
            JsonElement jsonElement => GetJsonElementType(jsonElement),
            IEnumerable => "array",
            _ => "object"
        };
    }

    private static string GetJsonElementType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => "null",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Number => element.TryGetInt32(out _) ? "integer" : "number",
            JsonValueKind.String => "string",
            JsonValueKind.Array => "array",
            JsonValueKind.Object => "object",
            _ => "unknown"
        };
    }
}