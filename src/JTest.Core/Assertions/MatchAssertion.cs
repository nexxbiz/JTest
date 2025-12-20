using System.Text.RegularExpressions;

namespace JTest.Core.Assertions;

/// <summary>
/// Checks whether the actual value matches the expected regex pattern
/// </summary>
public sealed class MatchAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : AssertionOperationBase(actualValue, expectedValue, description, mask)
{    
    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Expected '{resolvedActualValue}' to match pattern '{resolvedExpectedValue}'";
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        if (ActualValue == null || ExpectedValue == null)
        {
            return false;
        }

        var actualStr = $"{resolvedActualValue}";
        var pattern = $"{resolvedExpectedValue}";

        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        return regex.IsMatch(actualStr);
    }
}