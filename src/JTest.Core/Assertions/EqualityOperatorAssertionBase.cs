using JTest.Core.Utilities;

namespace JTest.Core.Assertions;

public abstract class EqualityOperatorAssertionBase(object? actualValue, object? expectedValue, string? description = null, bool? mask = null)
    : AssertionOperationBase(actualValue, expectedValue, description, mask)
{
    protected abstract string Operator { get; }

    protected sealed override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return $"Condition failed: {resolvedActualValue} {Operator} {resolvedExpectedValue}";
    }

    internal sealed override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        bool result;
        if (resolvedActualValue.IsDateTimeValue(out var actualTicks) && resolvedExpectedValue.IsDateTimeValue(out var expectedTicks))
        {
            result = ExecuteOperator(actualTicks, expectedTicks);
        }
        else if (Operator is EqualityOperators.Equal or EqualityOperators.NotEqual)
        {
            result = ExecuteOperator(resolvedActualValue, resolvedExpectedValue);
        }
        else
        {
            var actual = resolvedActualValue.ConvertToDouble();
            var expected = resolvedExpectedValue.ConvertToDouble();
            result = ExecuteOperator(actual, expected);
        }

        return result;
    }

    private bool ExecuteOperator(double actual, double expected)
    {
        return Operator switch
        {
            EqualityOperators.GreaterThanOrEqual => actual >= expected,
            EqualityOperators.LessThanOrEqual => actual <= expected,
            EqualityOperators.GreaterThan => actual > expected,
            EqualityOperators.LessThan => actual < expected,
            EqualityOperators.Equal => actual == expected,
            EqualityOperators.NotEqual => actual != expected
            ,
            _ => throw new NotSupportedException($"Unknown operator: {Operator}")
        };
    }

    private bool ExecuteOperator(object? actual, object? expected)
    {
        return Operator switch
        {
            EqualityOperators.Equal => AreValuesEqual(actual, expected),
            EqualityOperators.NotEqual => !AreValuesEqual(actual, expected),

            _ => throw new NotSupportedException($"Unknown operator for string values: {Operator}")
        };
    }

    private static bool AreValuesEqual(object? actual, object? expected)
    {
        if (actual == null && expected == null) return true;
        if (actual == null || expected == null) return false;

        if (actual.IsNumericValue() && expected.IsNumericValue())
        {
            return CompareNumericValues(actual, expected);
        }

        var actualStr = actual.ConvertToInvariantString();
        var expectedStr = expected.ConvertToInvariantString();

        return string.Equals(actualStr, expectedStr, StringComparison.Ordinal);
    }
  
    /// <summary>
    /// Compares two numbers by value, so 25 and 25.0 — the same number written two ways, which is
    /// routine between a response body and a suite file — are equal.
    ///
    /// Exact decimal comparison is preferred: it keeps integers that a double cannot distinguish
    /// (2^53 and 2^53+1) distinct, so a real difference in an id or a cent amount cannot become a
    /// passing assertion. Values outside decimal's range, and doubles/floats — whose conversion to
    /// decimal would round away genuine binary-floating-point differences — fall back to comparing
    /// as doubles.
    /// </summary>
    private static bool CompareNumericValues(object actual, object expected)
    {
        if (actual.TryGetExactDecimal(out var actualDecimal) && expected.TryGetExactDecimal(out var expectedDecimal))
            return actualDecimal == expectedDecimal;

        if (actual.TryGetDoubleValue(out var actualDouble) && expected.TryGetDoubleValue(out var expectedDouble))
            return actualDouble.Equals(expectedDouble);

        return false;
    }
}
