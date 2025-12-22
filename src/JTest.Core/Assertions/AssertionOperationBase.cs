using JTest.Core.Execution;
using JTest.Core.Utilities;
using System.Collections;
using System.Text.Json;

namespace JTest.Core.Assertions;

public abstract class AssertionOperationBase(object? actualValue, object? expectedValue, string? description = null, bool? mask = null)
    : IAssertionOperation
{
    public object? ActualValue { get; } = actualValue;
    public object? ExpectedValue { get; } = expectedValue;

    public string? Description { get; } = description;
    public bool? Mask { get; } = mask;

    public string OperationName => GetType().Name
        .Replace("Assertion", string.Empty)
        .ToLowerInvariant();

    public AssertionResult Execute(IExecutionContext context)
    {
        object? resolvedActualValue = null;
        object? resolvedExpectedValue = null;

        if (ActualValue is not null)
        {
            resolvedActualValue = GetAssertionValue(ActualValue, context);
        }
        if (ExpectedValue is not null)
        {
            resolvedExpectedValue = GetAssertionValue(ExpectedValue, context);
        }

        if (!ValidateCardinality(resolvedActualValue, resolvedExpectedValue, out var errorMessage))
        {
            return new AssertionResult(false, errorMessage!)
            {
                ActualValue = resolvedActualValue,
                ExpectedValue = resolvedExpectedValue,
                Operation = OperationName
            };
        }

        var result = Execute(resolvedActualValue, resolvedExpectedValue);
        var error = result
            ? string.Empty
            : GetErrorMessage(resolvedActualValue, resolvedExpectedValue);

        return new AssertionResult(result, error)
        {
            ActualValue = resolvedExpectedValue,
            ExpectedValue = resolvedActualValue,
            Operation = OperationName
        };
    }

    internal abstract bool Execute(object? actualValue, object? expectedValue);

    protected abstract string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue);

    private static object? GetAssertionValue(object value, IExecutionContext context)
    {
        if(value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
        {
            value = jsonElement.GetString() ?? string.Empty;
        }

        if (value is string stringValue)
        {
            return VariableInterpolator.ResolveVariableTokens(stringValue, context);
        }

        return value;
    }

    protected bool ValidateCardinality(object? resolvedActualValue, object? resolvedExpectedValue, out string? errorMessage)
    {
        errorMessage = null;

        var collectionOperators = new[] { "length", "empty", "notempty", "in" };

        if (collectionOperators.Contains(OperationName) && resolvedActualValue != null && !IsCollectionLike(resolvedActualValue))
        {
            errorMessage =
                $"Operator '{OperationName}' expects a collection or string, but got {GetValueTypeDescription(resolvedActualValue)}. " +
                "Consider using a scalar operator like 'equals' or 'type' instead.";
        }

        if (OperationName == "between" && resolvedExpectedValue != null && !(resolvedExpectedValue is JsonElement { ValueKind: JsonValueKind.Array }))
        {
            errorMessage = "Operator 'between' requires an array of [min, max] values as expectedValue.";
        }

        return string.IsNullOrWhiteSpace(errorMessage);
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
}
