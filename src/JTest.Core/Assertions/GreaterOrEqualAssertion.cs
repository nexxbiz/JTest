namespace JTest.Core.Assertions;

/// <summary>
/// Less than assertion with culture-independent numeric comparison
/// </summary>
public sealed class GreaterOrEqualAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : EqualityOperatorAssertionBase(actualValue, expectedValue, description, mask)
{
    protected override string Operator => EqualityOperators.GreaterThanOrEqual;
}
