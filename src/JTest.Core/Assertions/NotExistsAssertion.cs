namespace JTest.Core.Assertions;

/// <summary>
/// Not equals assertion
/// </summary>
public sealed class NotExistsAssertion(object? actualValue, string? description = null, bool? mask = null)
    : AssertionOperationBase(actualValue, expectedValue: null, description, mask)
{    
    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected value {resolvedActualValue} to not exist.";
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        var existsAssertion = new ExistsAssertion(ActualValue, Description, Mask);
        var existsResult = existsAssertion.Execute(resolvedActualValue, resolvedExpectedValue);

        return !existsResult;
    }
}
