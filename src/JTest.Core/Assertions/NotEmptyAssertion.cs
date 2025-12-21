namespace JTest.Core.Assertions;


/// <summary>
/// Empty assertion for collections and strings
/// </summary>
public sealed class NotEmptyAssertion(object? actualValue, string? description = null, bool? mask = null)
    : AssertionOperationBase(actualValue, expectedValue: null, description, mask)
{    
    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected value to not be empty, but it is.";
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        var emptyAssertion = new EmptyAssertion(ActualValue, Description, Mask);
        var result = emptyAssertion.Execute(resolvedActualValue, resolvedActualValue);
        return !result;
    }
}
