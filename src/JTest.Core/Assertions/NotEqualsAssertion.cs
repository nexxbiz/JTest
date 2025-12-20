namespace JTest.Core.Assertions;

/// <summary>
/// Not equals assertion
/// </summary>
public class NotEqualsAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : EqualityOperatorAssertionBase(actualValue, expectedValue, description, mask)
{
    protected override string Operator => EqualityOperators.NotEqual;
}
