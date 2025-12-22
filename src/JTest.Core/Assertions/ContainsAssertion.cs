namespace JTest.Core.Assertions;

/// <summary>
/// Contains assertion for string values
/// </summary>
public sealed class ContainsAssertion(object? actualValue, object? expectedValue, string? description = null, bool? mask = null)
    : AssertionOperationBase(actualValue, expectedValue, description, mask)
{
    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        if (resolvedActualValue == null || resolvedExpectedValue == null)
        {
            return false;
        }

        var actualStr = resolvedActualValue.ToString() ?? "";
        var expectedStr = resolvedExpectedValue.ToString() ?? "";

        return actualStr.Contains(expectedStr, StringComparison.OrdinalIgnoreCase);
    }

    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected '{resolvedActualValue}' to contain '{resolvedExpectedValue}'";
    }
}
