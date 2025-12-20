namespace JTest.Core.Assertions;
/// <summary>
/// Equality assertion that handles cultural differences in numeric values
/// </summary>
public sealed class EqualsAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : EqualityOperatorAssertion(actualValue, expectedValue, description, mask)
{
    public override string OperationType => "equals";

    protected override string Operator => EqualityOperators.Equal;
}
