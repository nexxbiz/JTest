namespace JTest.Core.Assertions;
/// <summary>
/// Contains assertion for string values
/// </summary>
public sealed class NotContainsAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : AssertionOperationBase(actualValue, expectedValue, description, mask)
{
    public override string OperationType => "notcontains";

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        var containsAssertion = new ContainsAssertion(ActualValue, ExpectedValue, Description, Mask);
        var containsResult = containsAssertion.Execute(resolvedActualValue, resolvedExpectedValue);

        return !containsResult;
    }

    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected '{resolvedActualValue}' to not contain '{resolvedExpectedValue}'";
    }
}
