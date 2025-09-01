using System.Globalization;
using System.Text.Json;
using JTest.Core.Execution;
using JTest.Core.Utilities;

namespace JTest.Core.Assertions;

/// <summary>
/// Result of an assertion operation
/// </summary>
public record AssertionResult(bool Success, string Message = "");

/// <summary>
/// Base interface for assertion operations
/// </summary>
public interface IAssertionOperation
{
    string OperationType { get; }
    AssertionResult Execute(object? actualValue, object? expectedValue);
}

/// <summary>
/// Equality assertion that handles cultural differences in numeric values
/// </summary>
public class EqualsAssertion : IAssertionOperation
{
    public string OperationType => "equals";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        var result = AreValuesEqual(actualValue, expectedValue);
        var message = result ? "" : $"Expected '{expectedValue}' but got '{actualValue}'";
        return new AssertionResult(result, message);
    }

    private bool AreValuesEqual(object? actual, object? expected)
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

    private bool IsNumeric(object value)
    {
        return value is double or float or decimal or int or long or short or byte or sbyte or uint or ulong or ushort;
    }

    private bool CompareNumericValues(object actual, object expected)
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

    private string ConvertToInvariantString(object value)
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

/// <summary>
/// Existence assertion
/// </summary>
public class ExistsAssertion : IAssertionOperation
{
    public string OperationType => "exists";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        var exists = actualValue != null && 
                    !string.IsNullOrEmpty(actualValue.ToString());
        var message = exists ? "" : "Value does not exist or is null/empty";
        return new AssertionResult(exists, message);
    }
}

/// <summary>
/// Greater than assertion with culture-independent numeric comparison
/// </summary>
public class GreaterThanAssertion : IAssertionOperation
{
    public string OperationType => "greater-than";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        try
        {
            var actual = Convert.ToDouble(actualValue, CultureInfo.InvariantCulture);
            var expected = Convert.ToDouble(expectedValue, CultureInfo.InvariantCulture);
            var result = actual > expected;
            var message = result ? "" : $"Expected {actualValue} to be greater than {expectedValue}";
            return new AssertionResult(result, message);
        }
        catch (Exception ex)
        {
            return new AssertionResult(false, $"Cannot compare values: {ex.Message}");
        }
    }
}

/// <summary>
/// Less than assertion with culture-independent numeric comparison
/// </summary>
public class LessThanAssertion : IAssertionOperation
{
    public string OperationType => "less-than";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        try
        {
            var actual = Convert.ToDouble(actualValue, CultureInfo.InvariantCulture);
            var expected = Convert.ToDouble(expectedValue, CultureInfo.InvariantCulture);
            var result = actual < expected;
            var message = result ? "" : $"Expected {actualValue} to be less than {expectedValue}";
            return new AssertionResult(result, message);
        }
        catch (Exception ex)
        {
            return new AssertionResult(false, $"Cannot compare values: {ex.Message}");
        }
    }
}

/// <summary>
/// Registry for assertion operations
/// </summary>
public class AssertionRegistry
{
    private readonly Dictionary<string, IAssertionOperation> _operations = new();

    public AssertionRegistry()
    {
        RegisterDefaultOperations();
    }

    private void RegisterDefaultOperations()
    {
        Register(new EqualsAssertion());
        Register(new ExistsAssertion());
        Register(new GreaterThanAssertion());
        Register(new LessThanAssertion());
    }

    public void Register(IAssertionOperation operation)
    {
        _operations[operation.OperationType] = operation;
    }

    public IAssertionOperation? GetOperation(string operationType)
    {
        return _operations.GetValueOrDefault(operationType);
    }
}

/// <summary>
/// Assertion processor for handling assertion arrays in step configurations
/// </summary>
public static class AssertionProcessor
{
    private static readonly AssertionRegistry Registry = new();

    public static List<AssertionResult> ProcessAssertions(JsonElement assertionsElement, IExecutionContext context)
    {
        var results = new List<AssertionResult>();

        if (assertionsElement.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var assertionElement in assertionsElement.EnumerateArray())
        {
            var result = ProcessSingleAssertion(assertionElement, context);
            results.Add(result);
        }

        return results;
    }

    private static AssertionResult ProcessSingleAssertion(JsonElement assertionElement, IExecutionContext context)
    {
        if (!assertionElement.TryGetProperty("op", out var opElement))
        {
            return new AssertionResult(false, "Missing 'op' property in assertion");
        }

        var operationType = opElement.GetString();
        if (string.IsNullOrEmpty(operationType))
        {
            return new AssertionResult(false, "Invalid operation type");
        }

        var operation = Registry.GetOperation(operationType);
        if (operation == null)
        {
            return new AssertionResult(false, $"Unknown assertion operation: {operationType}");
        }

        var actualValue = GetAssertionValue(assertionElement, "actualValue", context);
        var expectedValue = GetAssertionValue(assertionElement, "expectedValue", context);

        return operation.Execute(actualValue, expectedValue);
    }

    private static object? GetAssertionValue(JsonElement assertionElement, string propertyName, IExecutionContext context)
    {
        if (!assertionElement.TryGetProperty(propertyName, out var valueElement))
        {
            return null;
        }

        if (valueElement.ValueKind == JsonValueKind.String)
        {
            var stringValue = valueElement.GetString() ?? "";
            return VariableInterpolator.ResolveVariableTokens(stringValue, context);
        }

        return GetJsonElementValue(valueElement);
    }

    private static object GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Null => null!,
            _ => element.GetRawText()
        };
    }
}