namespace JTest.Core.Assertions;

/// <summary>
/// Greater than assertion with culture-independent numeric comparison
/// </summary>
public sealed class GreaterThanAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : EqualityOperatorAssertionBase(actualValue, expectedValue, description, mask)
{
    protected override string Operator => EqualityOperators.GreaterThan;
}
