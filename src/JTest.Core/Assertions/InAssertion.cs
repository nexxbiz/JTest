using System.Collections;
using System.Text.Json;

namespace JTest.Core.Assertions;

/// <summary>
/// In assertion to check if value is in a collection
/// </summary>
public sealed class InAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : AssertionOperationBase(actualValue, expectedValue, description, mask)
{
    public override string OperationType => "in";

    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected '{resolvedActualValue}' to be in {resolvedExpectedValue}";
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        if (resolvedExpectedValue == null)
        {
            return false;
        }

        var collection = GetCollection(resolvedExpectedValue)
            ?? throw new ArgumentException("Expected value must be a collection for 'in' assertion");

        var actualStr = resolvedActualValue?.ToString() ?? "";
        var result = collection.Any(item =>
        {
            var itemStr = item?.ToString() ?? "";
            return string.Equals(actualStr, itemStr, StringComparison.OrdinalIgnoreCase);
        });

        return result;
    }

    private IEnumerable<object?>? GetCollection(object value)
    {
        return value switch
        {
            IEnumerable<object> enumerable => enumerable,
            IEnumerable enumerable => enumerable.Cast<object>(),
            JsonElement { ValueKind: JsonValueKind.Array } jsonArray =>
                jsonArray.EnumerateArray().Select(GetJsonElementValue),
            _ => null
        };
    }

    private object GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Null => null!,
            _ => element.GetRawText()
        };
    }
}
