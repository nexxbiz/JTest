namespace JTest.Core.Assertions;

/// <summary>
/// Starts with assertion for string values
/// </summary>
public sealed class EndsWithAssertion(object? actualValue, object? expectedValue, string? description = null, bool? mask = null)
    : AssertionOperationBase(actualValue, expectedValue, description, mask)
{
    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected '{resolvedActualValue}' to end with '{resolvedExpectedValue}'";
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        if (ActualValue == null || ExpectedValue == null)
        {
            return false;
        }

        var actualStr = ActualValue?.ToString() ?? string.Empty;
        var expectedStr = ExpectedValue?.ToString() ?? string.Empty;

        return actualStr.EndsWith(expectedStr, StringComparison.OrdinalIgnoreCase);
    }
}
