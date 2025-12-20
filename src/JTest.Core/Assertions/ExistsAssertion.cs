namespace JTest.Core.Assertions;


/// <summary>
/// Existence assertion
/// </summary>
public class ExistsAssertion(object? actualValue, string? description, bool? mask)
    : AssertionOperationBase(actualValue, expectedValue: null, description, mask)
{
    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return "Value does not exist or is null/empty";
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return resolvedActualValue != null && !string.IsNullOrEmpty(resolvedActualValue.ToString());
    }
}