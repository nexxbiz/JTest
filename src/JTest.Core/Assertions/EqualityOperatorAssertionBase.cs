using System.Globalization;

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
        if (AssertionHelper.IsDateTimeAssertion(resolvedActualValue, resolvedExpectedValue, out var actualTicks, out var expectedTicks))
        {
            result = ExecuteOperator(actualTicks, expectedTicks);
        }
        else if (Operator is EqualityOperators.Equal or EqualityOperators.NotEqual)
        {
            result = ExecuteOperator(resolvedActualValue, resolvedExpectedValue);
        }
        else
        {
            var actual = Convert.ToDouble(resolvedActualValue, CultureInfo.InvariantCulture);
            var expected = Convert.ToDouble(resolvedExpectedValue, CultureInfo.InvariantCulture);
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

        // Handle numeric comparisons with culture independence
        if (IsNumeric(actual) && IsNumeric(expected))
        {
            return CompareNumericValues(actual, expected);
        }

        // Convert both values to strings using culture-independent formatting
        var actualStr = ConvertToInvariantString(actual);
        var expectedStr = ConvertToInvariantString(expected);

        return string.Equals(actualStr, expectedStr, StringComparison.Ordinal);
    }

    private static bool IsNumeric(object value)
    {
        return value is double or float or decimal or int or long or short or byte or sbyte or uint or ulong or ushort;
    }

    private static bool CompareNumericValues(object actual, object expected)
    {
        try
        {
            var actualDouble = Convert.ToDouble(actual, CultureInfo.InvariantCulture);
            var expectedDouble = Convert.ToDouble(expected, CultureInfo.InvariantCulture);
            return Math.Abs(actualDouble - expectedDouble) < double.Epsilon;
        }
        catch
        {
            return false;
        }
    }

    private static string ConvertToInvariantString(object value)
    {
        return value switch
        {
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
