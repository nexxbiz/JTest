namespace JTest.Core.Assertions;

/// <summary>
/// Greater than assertion with culture-independent numeric comparison
/// </summary>
public sealed class GreaterThanAssertion(object? actualValue, object? expectedValue, string? description = null, bool? mask = null)
    : EqualityOperatorAssertionBase(actualValue, expectedValue, description, mask)
{
    protected override string Operator => EqualityOperators.GreaterThan;
}
