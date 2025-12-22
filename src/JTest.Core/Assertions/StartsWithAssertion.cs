namespace JTest.Core.Assertions;

/// <summary>
/// Starts with assertion for string values
/// </summary>
public sealed class StartsWithAssertion(object? actualValue, object? expectedValue, string? description = null, bool? mask = null)
    : AssertionOperationBase(actualValue, expectedValue, description, mask)
{
    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected '{resolvedActualValue}' to start with '{resolvedExpectedValue}'";
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        if (resolvedActualValue == null || resolvedExpectedValue == null)
        {
            return false;
        }

        var actualStr = $"{resolvedActualValue}";
        var expectedStr = $"{resolvedExpectedValue}";

        return actualStr.StartsWith(expectedStr, StringComparison.OrdinalIgnoreCase);
    }
}
