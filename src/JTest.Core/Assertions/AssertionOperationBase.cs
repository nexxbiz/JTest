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

    /// <summary>
    /// Operators for which an unresolved path IS the signal being tested, rather than a broken
    /// expression — 'notexists' against a path that matches nothing must still pass.
    /// </summary>
    private static readonly string[] ExistenceOperators = ["exists", "notexists"];

    public AssertionResult Execute(IExecutionContext context)
    {
        object? resolvedActualValue = null;
        object? resolvedExpectedValue = null;
        var unresolvedPaths = new List<string>();

        if (ActualValue is not null)
        {
            resolvedActualValue = GetAssertionValue(ActualValue, context, unresolvedPaths);
        }
        if (ExpectedValue is not null)
        {
            resolvedExpectedValue = GetAssertionValue(ExpectedValue, context, unresolvedPaths);
        }

        // An unresolved path makes any comparison meaningless: the operand never had a value, so a
        // pass or a fail here would both be misleading. Report the path itself (FR-049). Existence
        // operators are exempt — for them, "matched nothing" is the answer, not a broken expression.
        if (unresolvedPaths.Count > 0 && !ExistenceOperators.Contains(OperationName))
        {
            return new AssertionResult(false, string.Join(" ", unresolvedPaths.Select(VariableInterpolator.DescribeUnresolvedPath)))
            {
                ActualValue = resolvedActualValue,
                ExpectedValue = resolvedExpectedValue,
                Subject = ActualValue,
                Operation = OperationName,
                UnresolvedPaths = unresolvedPaths
            };
        }

        if (!ValidateCardinality(resolvedActualValue, resolvedExpectedValue, out var errorMessage))
        {
            return new AssertionResult(false, errorMessage!)
            {
                ActualValue = resolvedActualValue,
                ExpectedValue = resolvedExpectedValue,
                Subject = ActualValue,
                Operation = OperationName,
                UnresolvedPaths = unresolvedPaths
            };
        }

        var result = Execute(resolvedActualValue, resolvedExpectedValue);
        var error = result
            ? string.Empty
            : GetErrorMessage(resolvedActualValue, resolvedExpectedValue);

        return new AssertionResult(result, error)
        {
            ActualValue = resolvedActualValue,
            ExpectedValue = resolvedExpectedValue,
            Subject = ActualValue,
            Operation = OperationName,
            UnresolvedPaths = unresolvedPaths
        };
    }

    internal abstract bool Execute(object? actualValue, object? expectedValue);

    protected abstract string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue);

    private static object? GetAssertionValue(object value, IExecutionContext context, ICollection<string> unresolvedPaths)
    {
        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
        {
            value = jsonElement.GetString() ?? string.Empty;
        }

        if (value is string stringValue)
        {
            var resolved = VariableInterpolator.ResolveVariableTokens(stringValue, context, out var unresolved);
            foreach (var path in unresolved)
            {
                unresolvedPaths.Add(path);
            }

            return resolved;
        }

        return value;
    }

    protected bool ValidateCardinality(object? resolvedActualValue, object? resolvedExpectedValue, out string? errorMessage)
    {
        errorMessage = null;

        // Operators that inspect the actual value AS a collection. 'in' is deliberately not one of
        // them: it asks whether a scalar actual is one of the expected values, so it is the EXPECTED
        // value that must be a collection.
        var collectionOperators = new[] { "length", "empty", "notempty" };

        if (collectionOperators.Contains(OperationName) && resolvedActualValue != null && !IsCollectionLike(resolvedActualValue))
        {
            errorMessage =
                $"Operator '{OperationName}' expects a collection or string, but got {GetValueTypeDescription(resolvedActualValue)}. " +
                "Consider using a scalar operator like 'equals' or 'type' instead.";
        }

        if (OperationName == "in" && resolvedExpectedValue != null && !IsCollectionLike(resolvedExpectedValue))
        {
            errorMessage =
                $"Operator 'in' expects a collection as expectedValue (e.g. [200, 201]), but got " +
                $"{GetValueTypeDescription(resolvedExpectedValue)}.";
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
