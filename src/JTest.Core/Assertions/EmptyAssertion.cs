using System.Collections;
using System.Text.Json;

namespace JTest.Core.Assertions;


/// <summary>
/// Empty assertion for collections and strings
/// </summary>
public sealed class EmptyAssertion(object? actualValue, string? description, bool? mask)
    : AssertionOperationBase(actualValue, expectedValue: null, description, mask)
{
    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected value to be empty but it has {GetLength(resolvedActualValue)} items/characters";
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        var length = GetLength(resolvedActualValue);

        return length == 0;
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

            _ => throw new NotSupportedException($"Cannot determine length of value '{value}'")
        };
    }
}
