namespace JTest.Core.Assertions;
/// <summary>
/// Equality assertion that handles cultural differences in numeric values
/// </summary>
public sealed class EqualsAssertion(object? actualValue, object? expectedValue, string? description = null, bool? mask = null)
    : EqualityOperatorAssertionBase(actualValue, expectedValue, description, mask)
{
    protected override string Operator => EqualityOperators.Equal;
}
