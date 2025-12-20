namespace JTest.Core.Assertions;

/// <summary>
/// Not equals assertion
/// </summary>
public class NotEqualsAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : EqualityOperatorAssertion(actualValue, expectedValue, description, mask)
{
    public override string OperationType => "notequals";
    protected override string Operator => EqualityOperators.NotEqual;
}
