namespace JTest.Core.Assertions;

/// <summary>
/// Not equals assertion
/// </summary>
public sealed class NotEqualsAssertion(object? actualValue, object? expectedValue, string? description = null, bool? mask = null)
    : EqualityOperatorAssertionBase(actualValue, expectedValue, description, mask)
{
    protected override string Operator => EqualityOperators.NotEqual;
}
