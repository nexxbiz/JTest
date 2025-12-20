namespace JTest.Core.Assertions;

/// <summary>
/// Less than assertion with culture-independent numeric comparison
/// </summary>
public sealed class LessThanOrEqualAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : EqualityOperatorAssertion(actualValue, expectedValue, description, mask)
{
    public override string OperationType => "lessorequal";
    protected override string Operator => EqualityOperators.LessThanOrEqual;
}
