using System.Globalization;
using System.Text.Json;

namespace JTest.Core.Assertions;

/// <summary>
/// Between assertion for numeric values
/// </summary>
public sealed class BetweenAssertion(object? actualValue, object? expectedValue, string? description, bool? mask)
    : AssertionOperationBase(actualValue, expectedValue, description, mask)
{
    public override string OperationType => "between";

    private string errorMessage = string.Empty;

    protected override string GetErrorMessage(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        return errorMessage;
    }

    internal override bool Execute(object? resolvedActualValue, object? resolvedExpectedValue)
    {
        try
        {
            var actual = Convert.ToDouble(ActualValue, CultureInfo.InvariantCulture);

            // Expected value should be an array with min and max values
            if (ExpectedValue is not JsonElement jsonElement || jsonElement.ValueKind != JsonValueKind.Array)
            {
                errorMessage = "Between assertion requires an array of [min, max] values";
                return false;
            }

            var array = jsonElement.EnumerateArray().ToArray();
            if (array.Length != 2)
            {
                errorMessage = "Between assertion requires exactly 2 values: [min, max]";
                return false;
            }

            var min = Convert.ToDouble(array[0].GetRawText(), CultureInfo.InvariantCulture);
            var max = Convert.ToDouble(array[1].GetRawText(), CultureInfo.InvariantCulture);
            var result = actual >= min && actual <= max;

            errorMessage = $"Expected {resolvedActualValue} to be between {min} and {max}";

            return result;

        }
        catch (Exception ex)
        {
            errorMessage = $"Cannot compare values: {ex.Message}";
            return false;
        }
    }
}
