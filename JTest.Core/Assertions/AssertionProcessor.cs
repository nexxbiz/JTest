using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using JTest.Core.Execution;
using JTest.Core.Utilities;

namespace JTest.Core.Assertions;

/// <summary>
/// Result of an assertion operation
/// </summary>
public class AssertionResult
{
    public bool Success { get; set; }
    public string Operation { get; set; } = "";
    public string Description { get; set; } = "";
    public object? ActualValue { get; set; }
    public object? ExpectedValue { get; set; }
    public string ErrorMessage { get; set; } = "";

    public AssertionResult(bool success, string errorMessage = "")
    {
        Success = success;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Interface for assertion processing
/// </summary>
public interface IAssertionProcessor
{
    Task<List<AssertionResult>> ProcessAssertionsAsync(JsonElement assertArray, IExecutionContext context);
}

/// <summary>
/// Default implementation of IAssertionProcessor
/// </summary>
public class DefaultAssertionProcessor : IAssertionProcessor
{
    public Task<List<AssertionResult>> ProcessAssertionsAsync(JsonElement assertArray, IExecutionContext context)
    {
        var results = AssertionProcessor.ProcessAssertions(assertArray, context);
        return Task.FromResult(results);
    }
}

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
        var errorMessage = result ? "" : $"Expected '{expectedValue}' but got '{actualValue}'";
        return new AssertionResult(result, errorMessage)
        {
            Operation = OperationType,
            ActualValue = actualValue,
            ExpectedValue = expectedValue
        };
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
        var errorMessage = exists ? "" : "Value does not exist or is null/empty";
        return new AssertionResult(exists, errorMessage)
        {
            Operation = OperationType,
            ActualValue = actualValue,
            ExpectedValue = expectedValue
        };
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
            var errorMessage = result ? "" : $"Expected {actualValue} to be greater than {expectedValue}";
            return new AssertionResult(result, errorMessage)
            {
                Operation = OperationType,
                ActualValue = actualValue,
                ExpectedValue = expectedValue
            };
        }
        catch (Exception ex)
        {
            return new AssertionResult(false, $"Cannot compare values: {ex.Message}")
            {
                Operation = OperationType,
                ActualValue = actualValue,
                ExpectedValue = expectedValue
            };
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
            var errorMessage = result ? "" : $"Expected {actualValue} to be less than {expectedValue}";
            return new AssertionResult(result, errorMessage)
            {
                Operation = OperationType,
                ActualValue = actualValue,
                ExpectedValue = expectedValue
            };
        }
        catch (Exception ex)
        {
            return new AssertionResult(false, $"Cannot compare values: {ex.Message}")
            {
                Operation = OperationType,
                ActualValue = actualValue,
                ExpectedValue = expectedValue
            };
        }
    }
}

/// <summary>
/// Not equals assertion
/// </summary>
public class NotEqualsAssertion : IAssertionOperation
{
    public string OperationType => "notequals";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        var equalsAssertion = new EqualsAssertion();
        var equalsResult = equalsAssertion.Execute(actualValue, expectedValue);
        var result = !equalsResult.Success;
        var errorMessage = result ? "" : $"Expected '{actualValue}' to not equal '{expectedValue}'";
        return new AssertionResult(result, errorMessage)
        {
            Operation = OperationType,
            ActualValue = actualValue,
            ExpectedValue = expectedValue
        };
    }
}

/// <summary>
/// Not exists assertion
/// </summary>
public class NotExistsAssertion : IAssertionOperation
{
    public string OperationType => "notexists";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        var exists = actualValue != null && !string.IsNullOrEmpty(actualValue.ToString());
        var result = !exists;
        var message = result ? "" : "Expected value to not exist, but it does";
        return new AssertionResult(result, message);
    }
}

/// <summary>
/// Contains assertion for string values
/// </summary>
public class ContainsAssertion : IAssertionOperation
{
    public string OperationType => "contains";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        if (actualValue == null || expectedValue == null)
        {
            return new AssertionResult(false, "Cannot perform contains check on null values")
            {
                Operation = OperationType,
                ActualValue = actualValue,
                ExpectedValue = expectedValue
            };
        }

        var actualStr = actualValue.ToString() ?? "";
        var expectedStr = expectedValue.ToString() ?? "";
        var result = actualStr.Contains(expectedStr, StringComparison.OrdinalIgnoreCase);
        var errorMessage = result ? "" : $"Expected '{actualStr}' to contain '{expectedStr}'";
        return new AssertionResult(result, errorMessage)
        {
            Operation = OperationType,
            ActualValue = actualValue,
            ExpectedValue = expectedValue
        };
    }
}

/// <summary>
/// Not contains assertion for string values
/// </summary>
public class NotContainsAssertion : IAssertionOperation
{
    public string OperationType => "notcontains";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        if (actualValue == null || expectedValue == null)
        {
            return new AssertionResult(true, "Null values don't contain anything");
        }

        var actualStr = actualValue.ToString() ?? "";
        var expectedStr = expectedValue.ToString() ?? "";
        var result = !actualStr.Contains(expectedStr, StringComparison.OrdinalIgnoreCase);
        var message = result ? "" : $"Expected '{actualStr}' to not contain '{expectedStr}'";
        return new AssertionResult(result, message);
    }
}

/// <summary>
/// Starts with assertion for string values
/// </summary>
public class StartsWithAssertion : IAssertionOperation
{
    public string OperationType => "startswith";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        if (actualValue == null || expectedValue == null)
        {
            return new AssertionResult(false, "Cannot perform startswith check on null values");
        }

        var actualStr = actualValue.ToString() ?? "";
        var expectedStr = expectedValue.ToString() ?? "";
        var result = actualStr.StartsWith(expectedStr, StringComparison.OrdinalIgnoreCase);
        var message = result ? "" : $"Expected '{actualStr}' to start with '{expectedStr}'";
        return new AssertionResult(result, message);
    }
}

/// <summary>
/// Ends with assertion for string values
/// </summary>
public class EndsWithAssertion : IAssertionOperation
{
    public string OperationType => "endswith";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        if (actualValue == null || expectedValue == null)
        {
            return new AssertionResult(false, "Cannot perform endswith check on null values");
        }

        var actualStr = actualValue.ToString() ?? "";
        var expectedStr = expectedValue.ToString() ?? "";
        var result = actualStr.EndsWith(expectedStr, StringComparison.OrdinalIgnoreCase);
        var message = result ? "" : $"Expected '{actualStr}' to end with '{expectedStr}'";
        return new AssertionResult(result, message);
    }
}

/// <summary>
/// Regex matches assertion for string values
/// </summary>
public class MatchesAssertion : IAssertionOperation
{
    public string OperationType => "matches";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        if (actualValue == null || expectedValue == null)
        {
            return new AssertionResult(false, "Cannot perform regex match on null values");
        }

        var actualStr = actualValue.ToString() ?? "";
        var pattern = expectedValue.ToString() ?? "";

        try
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var result = regex.IsMatch(actualStr);
            var message = result ? "" : $"Expected '{actualStr}' to match pattern '{pattern}'";
            return new AssertionResult(result, message);
        }
        catch (Exception ex)
        {
            return new AssertionResult(false, $"Invalid regex pattern '{pattern}': {ex.Message}");
        }
    }
}

/// <summary>
/// Greater than or equal assertion with culture-independent numeric comparison
/// </summary>
public class GreaterOrEqualAssertion : IAssertionOperation
{
    public string OperationType => "greaterorequal";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        try
        {
            var actual = Convert.ToDouble(actualValue, CultureInfo.InvariantCulture);
            var expected = Convert.ToDouble(expectedValue, CultureInfo.InvariantCulture);
            var result = actual >= expected;
            var message = result ? "" : $"Expected {actualValue} to be greater than or equal to {expectedValue}";
            return new AssertionResult(result, message);
        }
        catch (Exception ex)
        {
            return new AssertionResult(false, $"Cannot compare values: {ex.Message}");
        }
    }
}

/// <summary>
/// Less than or equal assertion with culture-independent numeric comparison
/// </summary>
public class LessOrEqualAssertion : IAssertionOperation
{
    public string OperationType => "lessorequal";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        try
        {
            var actual = Convert.ToDouble(actualValue, CultureInfo.InvariantCulture);
            var expected = Convert.ToDouble(expectedValue, CultureInfo.InvariantCulture);
            var result = actual <= expected;
            var message = result ? "" : $"Expected {actualValue} to be less than or equal to {expectedValue}";
            return new AssertionResult(result, message);
        }
        catch (Exception ex)
        {
            return new AssertionResult(false, $"Cannot compare values: {ex.Message}");
        }
    }
}

/// <summary>
/// Between assertion for numeric values
/// </summary>
public class BetweenAssertion : IAssertionOperation
{
    public string OperationType => "between";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        try
        {
            var actual = Convert.ToDouble(actualValue, CultureInfo.InvariantCulture);
            
            // Expected value should be an array with min and max values
            if (expectedValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                var array = jsonElement.EnumerateArray().ToArray();
                if (array.Length != 2)
                {
                    return new AssertionResult(false, "Between assertion requires exactly 2 values: [min, max]");
                }

                var min = Convert.ToDouble(array[0].GetRawText(), CultureInfo.InvariantCulture);
                var max = Convert.ToDouble(array[1].GetRawText(), CultureInfo.InvariantCulture);
                var result = actual >= min && actual <= max;
                var message = result ? "" : $"Expected {actualValue} to be between {min} and {max}";
                return new AssertionResult(result, message);
            }

            return new AssertionResult(false, "Between assertion requires an array of [min, max] values");
        }
        catch (Exception ex)
        {
            return new AssertionResult(false, $"Cannot compare values: {ex.Message}");
        }
    }
}

/// <summary>
/// Length assertion for collections and strings
/// </summary>
public class LengthAssertion : IAssertionOperation
{
    public string OperationType => "length";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        try
        {
            var expectedLength = Convert.ToInt32(expectedValue, CultureInfo.InvariantCulture);
            var actualLength = GetLength(actualValue);
            
            if (actualLength == -1)
            {
                return new AssertionResult(false, "Cannot determine length of the provided value");
            }

            var result = actualLength == expectedLength;
            var message = result ? "" : $"Expected length {expectedLength} but got {actualLength}";
            return new AssertionResult(result, message);
        }
        catch (Exception ex)
        {
            return new AssertionResult(false, $"Cannot compare lengths: {ex.Message}");
        }
    }

    private int GetLength(object? value)
    {
        return value switch
        {
            null => 0,
            string str => str.Length,
            ICollection collection => collection.Count,
            IEnumerable enumerable => enumerable.Cast<object>().Count(),
            JsonElement { ValueKind: JsonValueKind.Array } jsonArray => jsonArray.GetArrayLength(),
            JsonElement { ValueKind: JsonValueKind.String } jsonString => jsonString.GetString()?.Length ?? 0,
            _ => -1
        };
    }
}

/// <summary>
/// Empty assertion for collections and strings
/// </summary>
public class EmptyAssertion : IAssertionOperation
{
    public string OperationType => "empty";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        var length = GetLength(actualValue);
        
        if (length == -1)
        {
            return new AssertionResult(false, "Cannot determine if the provided value is empty");
        }

        var result = length == 0;
        var message = result ? "" : $"Expected value to be empty but it has {length} items/characters";
        return new AssertionResult(result, message);
    }

    private int GetLength(object? value)
    {
        return value switch
        {
            null => 0,
            string str => str.Length,
            ICollection collection => collection.Count,
            IEnumerable enumerable => enumerable.Cast<object>().Count(),
            JsonElement { ValueKind: JsonValueKind.Array } jsonArray => jsonArray.GetArrayLength(),
            JsonElement { ValueKind: JsonValueKind.String } jsonString => jsonString.GetString()?.Length ?? 0,
            _ => -1
        };
    }
}

/// <summary>
/// Not empty assertion for collections and strings
/// </summary>
public class NotEmptyAssertion : IAssertionOperation
{
    public string OperationType => "notempty";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        var length = GetLength(actualValue);
        
        if (length == -1)
        {
            return new AssertionResult(false, "Cannot determine if the provided value is empty");
        }

        var result = length > 0;
        var message = result ? "" : "Expected value to not be empty but it is";
        return new AssertionResult(result, message);
    }

    private int GetLength(object? value)
    {
        return value switch
        {
            null => 0,
            string str => str.Length,
            ICollection collection => collection.Count,
            IEnumerable enumerable => enumerable.Cast<object>().Count(),
            JsonElement { ValueKind: JsonValueKind.Array } jsonArray => jsonArray.GetArrayLength(),
            JsonElement { ValueKind: JsonValueKind.String } jsonString => jsonString.GetString()?.Length ?? 0,
            _ => -1
        };
    }
}

/// <summary>
/// In assertion to check if value is in a collection
/// </summary>
public class InAssertion : IAssertionOperation
{
    public string OperationType => "in";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        if (expectedValue == null)
        {
            return new AssertionResult(false, "Expected value cannot be null for 'in' assertion");
        }

        var collection = GetCollection(expectedValue);
        if (collection == null)
        {
            return new AssertionResult(false, "Expected value must be a collection for 'in' assertion");
        }

        var actualStr = actualValue?.ToString() ?? "";
        var result = collection.Any(item => 
        {
            var itemStr = item?.ToString() ?? "";
            return string.Equals(actualStr, itemStr, StringComparison.OrdinalIgnoreCase);
        });

        var collectionStr = string.Join(", ", collection.Select(x => x?.ToString() ?? "null"));
        var message = result ? "" : $"Expected '{actualValue}' to be in [{collectionStr}]";
        return new AssertionResult(result, message);
    }

    private IEnumerable<object?>? GetCollection(object value)
    {
        return value switch
        {
            IEnumerable<object> enumerable => enumerable,
            IEnumerable enumerable => enumerable.Cast<object>(),
            JsonElement { ValueKind: JsonValueKind.Array } jsonArray => 
                jsonArray.EnumerateArray().Select(GetJsonElementValue),
            _ => null
        };
    }

    private object GetJsonElementValue(JsonElement element)
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

/// <summary>
/// Type assertion to check the type of a value
/// </summary>
public class TypeAssertion : IAssertionOperation
{
    public string OperationType => "type";

    public AssertionResult Execute(object? actualValue, object? expectedValue)
    {
        if (expectedValue == null)
        {
            return new AssertionResult(false, "Expected type cannot be null");
        }

        var expectedType = expectedValue.ToString()?.ToLowerInvariant() ?? "";
        var actualType = GetValueType(actualValue);
        
        var result = string.Equals(actualType, expectedType, StringComparison.OrdinalIgnoreCase);
        var message = result ? "" : $"Expected type '{expectedType}' but got '{actualType}'";
        return new AssertionResult(result, message);
    }

    private string GetValueType(object? value)
    {
        return value switch
        {
            null => "null",
            bool => "boolean",
            int or long or short or byte or sbyte or uint or ulong or ushort => "integer",
            float or double or decimal => "number",
            string => "string",
            JsonElement jsonElement => GetJsonElementType(jsonElement),
            IEnumerable => "array",
            _ => "object"
        };
    }

    private string GetJsonElementType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => "null",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Number => element.TryGetInt32(out _) ? "integer" : "number",
            JsonValueKind.String => "string",
            JsonValueKind.Array => "array",
            JsonValueKind.Object => "object",
            _ => "unknown"
        };
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
        // Basic operations
        Register(new EqualsAssertion());
        Register(new NotEqualsAssertion());
        Register(new ExistsAssertion());
        Register(new NotExistsAssertion());
        
        // String operations
        Register(new ContainsAssertion());
        Register(new NotContainsAssertion());
        Register(new StartsWithAssertion());
        Register(new EndsWithAssertion());
        Register(new MatchesAssertion());
        
        // Numeric operations
        Register(new GreaterThanAssertion());
        Register(new GreaterOrEqualAssertion());
        Register(new LessThanAssertion());
        Register(new LessOrEqualAssertion());
        Register(new BetweenAssertion());
        
        // Collection operations
        Register(new LengthAssertion());
        Register(new EmptyAssertion());
        Register(new NotEmptyAssertion());
        Register(new InAssertion());
        
        // Type checking
        Register(new TypeAssertion());
    }

    public void Register(IAssertionOperation operation)
    {
        _operations[operation.OperationType] = operation;
    }

    public IAssertionOperation? GetOperation(string operationType)
    {
        return _operations.GetValueOrDefault(operationType);
    }

    /// <summary>
    /// Get all available operation types for error messages
    /// </summary>
    public IEnumerable<string> GetAvailableOperations()
    {
        return _operations.Keys.OrderBy(x => x);
    }

    /// <summary>
    /// Get suggestions for similar operation names
    /// </summary>
    public IEnumerable<string> GetSimilarOperations(string operationType)
    {
        if (string.IsNullOrEmpty(operationType))
            return Enumerable.Empty<string>();

        return _operations.Keys
            .Where(op => op.Contains(operationType, StringComparison.OrdinalIgnoreCase) ||
                        operationType.Contains(op, StringComparison.OrdinalIgnoreCase) ||
                        LevenshteinDistance(op, operationType) <= 2)
            .OrderBy(op => LevenshteinDistance(op, operationType))
            .Take(3);
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;

        var d = new int[s1.Length + 1, s2.Length + 1];

        for (var i = 0; i <= s1.Length; i++)
            d[i, 0] = i;

        for (var j = 0; j <= s2.Length; j++)
            d[0, j] = j;

        for (var i = 1; i <= s1.Length; i++)
        {
            for (var j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[s1.Length, s2.Length];
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
            var availableOps = Registry.GetAvailableOperations().ToList();
            var suggestions = Registry.GetSimilarOperations(operationType).ToList();
            
            var errorMessage = $"Unknown assertion operation: '{operationType}'";
            
            if (suggestions.Any())
            {
                errorMessage += $". Did you mean: {string.Join(", ", suggestions.Select(s => $"'{s}'"))}?";
            }
            else
            {
                errorMessage += $". Available operations: {string.Join(", ", availableOps.Select(s => $"'{s}'"))}";
            }
            
            return new AssertionResult(false, errorMessage);
        }

        var actualValue = GetAssertionValue(assertionElement, "actualValue", context);
        var expectedValue = GetAssertionValue(assertionElement, "expectedValue", context);

        // Enhanced validation for cardinality mismatches
        var cardinalityError = ValidateCardinality(operationType, actualValue, expectedValue);
        if (!string.IsNullOrEmpty(cardinalityError))
        {
            return new AssertionResult(false, cardinalityError);
        }

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
            
            // Enhanced validation for JSONPath expressions
            if (stringValue.StartsWith("{{") && stringValue.EndsWith("}}"))
            {
                var pathError = ValidateJsonPath(stringValue, context);
                if (!string.IsNullOrEmpty(pathError))
                {
                    context.Log.Add($"JSONPath validation warning: {pathError}");
                }
            }
            
            return VariableInterpolator.ResolveVariableTokens(stringValue, context);
        }

        return GetJsonElementValue(valueElement);
    }

    /// <summary>
    /// Validate cardinality mismatches between operators and value types
    /// </summary>
    private static string ValidateCardinality(string operationType, object? actualValue, object? expectedValue)
    {
        var collectionOperators = new[] { "length", "empty", "notempty", "in" };
        var scalarOperators = new[] { "equals", "notequals", "contains", "notcontains", 
                                     "startswith", "endswith", "matches", "greaterthan", 
                                     "greaterorequal", "lessthan", "lessorequal", "between" };

        if (collectionOperators.Contains(operationType))
        {
            if (actualValue != null && !IsCollectionLike(actualValue))
            {
                return $"Operator '{operationType}' expects a collection or string, but got {GetValueTypeDescription(actualValue)}. " +
                       "Consider using a scalar operator like 'equals' or 'type' instead.";
            }
        }

        if (operationType == "between" && expectedValue != null)
        {
            if (!(expectedValue is JsonElement { ValueKind: JsonValueKind.Array }))
            {
                return "Operator 'between' requires an array of [min, max] values as expectedValue.";
            }
        }

        return "";
    }

    /// <summary>
    /// Validate JSONPath expressions and provide helpful suggestions
    /// </summary>
    private static string ValidateJsonPath(string pathExpression, IExecutionContext context)
    {
        var path = pathExpression.Trim('{', '}').Trim();
        
        // Check for reserved keys that shouldn't be modified
        var reservedKeys = new[] { "this", "env", "now", "random" };
        var pathParts = path.Split('.');
        
        if (pathParts.Length > 1)
        {
            var rootKey = pathParts[1]; // Skip the '$' part
            if (reservedKeys.Contains(rootKey))
            {
                return $"Path references reserved scope '{rootKey}' which is read-only.";
            }
            
            // Check for potential step references
            if (pathParts.Length > 2 && !context.Variables.ContainsKey(rootKey))
            {
                return $"Step reference '{rootKey}' not found. Available variables: {string.Join(", ", context.Variables.Keys.Take(10))}";
            }
        }

        return "";
    }

    private static bool IsCollectionLike(object value)
    {
        return value switch
        {
            string => true,
            IEnumerable => true,
            JsonElement { ValueKind: JsonValueKind.Array } => true,
            JsonElement { ValueKind: JsonValueKind.String } => true,
            _ => false
        };
    }

    private static string GetValueTypeDescription(object value)
    {
        return value switch
        {
            null => "null",
            bool => "boolean",
            int or long or short or byte or sbyte or uint or ulong or ushort => "integer",
            float or double or decimal => "number",
            string => "string",
            JsonElement jsonElement => $"JSON {jsonElement.ValueKind.ToString().ToLowerInvariant()}",
            IEnumerable => "collection",
            _ => "object"
        };
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