namespace JTest.Core.Assertions;

/// <summary>
/// Less than assertion with culture-independent numeric comparison
/// </summary>
public sealed class LessOrEqualAssertion(object? actualValue, object? expectedValue, string? description = null, bool? mask = null)
    : EqualityOperatorAssertionBase(actualValue, expectedValue, description, mask)
{
    protected override string Operator => EqualityOperators.LessThanOrEqual;
}
