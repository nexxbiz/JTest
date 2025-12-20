namespace JTest.Core.Assertions;


/// <summary>
/// Empty assertion for collections and strings
/// </summary>
public sealed class NotEmptyAssertion(object? actualValue, string? description, bool? mask)
    : AssertionOperationBase(actualValue, expectedValue: null, description, mask)
{
    public override string OperationType => "empty";

    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected value to have values, but it was empty.";
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        var emptyAssertion = new EmptyAssertion(ActualValue, Description, Mask);
        var result = emptyAssertion.Execute(resolvedActualValue, resolvedActualValue);
        return !result;
    }
}
