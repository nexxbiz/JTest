using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace JTest.Core.Assertions;

/// <summary>
/// Between assertion for numeric values
/// </summary>
public sealed class LengthAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : AssertionOperationBase(actualValue, expectedValue, description, mask)
{    
    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected length {resolvedExpectedValue} but got {GetLength(resolvedActualValue)}";
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        var expectedLength = Convert.ToInt32(resolvedExpectedValue, CultureInfo.InvariantCulture);
        var actualLength = GetLength(resolvedActualValue);

        return actualLength == expectedLength;
    }

    private static int GetLength(object? value)
    {
        return value switch
        {
            null => 0,
            string str => str.Length,
            ICollection collection => collection.Count,
            IEnumerable enumerable => enumerable.Cast<object>().Count(),
            JsonElement { ValueKind: JsonValueKind.Array } jsonArray => jsonArray.GetArrayLength(),
            JsonElement { ValueKind: JsonValueKind.String } jsonString => jsonString.GetString()?.Length ?? 0,
            _ => throw new InvalidOperationException($"Cannot determine length for value: {value}")
        };
    }
}
