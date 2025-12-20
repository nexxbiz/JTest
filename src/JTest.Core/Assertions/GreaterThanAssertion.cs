namespace JTest.Core.Assertions;

/// <summary>
/// Greater than assertion with culture-independent numeric comparison
/// </summary>
public sealed class GreaterThanAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : EqualityOperatorAssertion(actualValue, expectedValue, description, mask)
{
    public override string OperationType => "greaterthan";
    protected override string Operator => EqualityOperators.GreaterThan;
}
