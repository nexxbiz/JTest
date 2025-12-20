namespace JTest.Core.Assertions;

/// <summary>
/// Less than assertion with culture-independent numeric comparison
/// </summary>
public sealed class GreaterThanOrEqualAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : EqualityOperatorAssertion(actualValue, expectedValue, description, mask)
{
    public override string OperationType => "greaterorequal";
    protected override string Operator => EqualityOperators.GreaterThanOrEqual;
}
