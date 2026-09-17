using JTest.Core.Execution;
using System.Globalization;
using System.Text.Json;

namespace JTest.Core.Utilities;

internal static class TypeConversionHelper
{
    internal static IEnumerable<object> ConvertToArray(this object? value, IExecutionContext? context = null)
    {
        if (value is IEnumerable<object> enumerable)
        {
            return enumerable;
        }

        if (value is string stringValue && context is not null)
        {
            var resolved = VariableInterpolator.ResolveVariableTokens(stringValue, context);
            if (resolved is not IEnumerable<object> resolvedArray)
            {
                throw new InvalidOperationException($"Failed to convert value '{value}' to an array");
            }

            return resolvedArray;
        }

        if (value is JsonElement jsonValue && jsonValue.ValueKind == JsonValueKind.String && context is not null)
        {
            var arrayObject = VariableInterpolator.ResolveVariableTokens(jsonValue.GetString()!, context);
            if (arrayObject is not IEnumerable<object> enumerableArrayObject)
            {
                throw new InvalidOperationException($"Failed to convert value '{value}' to an array");
            }

            return enumerableArrayObject;
        }

        if(value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            return jsonElement.EnumerateArray().Select(x => x as object);
        }

        throw new InvalidOperationException($"Failed to convert value '{value}' to an array");
    }

    internal static double ConvertToDouble(this object? value, IExecutionContext? context = null)
    {
        if (value is null)
        {
            throw new FormatException("Cannot convert null to a numeric value");
        }

        if (value is string)
        {
            var stringValue = context is not null
                ? VariableInterpolator.ResolveVariableTokens($"{value}", context)
                : $"{value}";

            return Convert.ToDouble(stringValue, CultureInfo.InvariantCulture);
        }

        if (value is JsonElement element)
        {
            return element.ConvertToDouble(context);
        }

        if (IsNumeric(value))
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        throw new FormatException($"Cannot convert '{value}' to a numeric value");
    }

    internal static double ConvertToDouble(this JsonElement element, IExecutionContext? context = null)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.GetDouble();

        if (element.ValueKind == JsonValueKind.String)
        {
            var elementString = context is not null
                ? VariableInterpolator.ResolveVariableTokens(element.GetString()!, context)
                : element.GetString();

            return Convert.ToDouble(elementString, CultureInfo.InvariantCulture);
        }

        throw new FormatException($"Cannot convert JsonElement '{element}' to a numeric value");
    }

    internal static bool IsNumeric(this object value)
    {
        return value is double or float or decimal or int or long or short or byte or sbyte or uint or ulong or ushort;
    }

    /// <summary>
    /// True when the value is a number, counting a JSON number as one. Values reaching an assertion
    /// normally arrive as <see cref="JsonElement"/> — the actual from a response body, the expected
    /// from the suite file — so a check that only recognises CLR numerics answers "not a number" for
    /// almost every real comparison.
    /// </summary>
    internal static bool IsNumericValue(this object? value) =>
        value is JsonElement { ValueKind: JsonValueKind.Number } || (value is not null && value.IsNumeric());

    /// <summary>
    /// Reads the value as an exact <see cref="decimal"/>, for sources that carry an exact decimal
    /// value: a JSON number (parsed from its literal text) and CLR integral/decimal types.
    /// <see cref="double"/> and <see cref="float"/> are deliberately excluded — converting them to
    /// decimal rounds to 15 significant digits, which would make values that genuinely differ in
    /// binary floating point compare equal.
    /// </summary>
    internal static bool TryGetExactDecimal(this object? value, out decimal number)
    {
        number = 0m;

        if (value is JsonElement { ValueKind: JsonValueKind.Number } element)
            return element.TryGetDecimal(out number);

        switch (value)
        {
            case decimal d: number = d; return true;
            case int or long or short or byte or sbyte or uint or ulong or ushort:
                try { number = Convert.ToDecimal(value, CultureInfo.InvariantCulture); return true; }
                catch (OverflowException) { return false; }
            default:
                return false;
        }
    }

    /// <summary>Reads the value as a <see cref="double"/>, for numbers decimal cannot represent.</summary>
    internal static bool TryGetDoubleValue(this object? value, out double number)
    {
        number = 0d;

        if (value is JsonElement { ValueKind: JsonValueKind.Number } element)
            return element.TryGetDouble(out number);

        if (value is not null && value.IsNumeric())
        {
            try { number = Convert.ToDouble(value, CultureInfo.InvariantCulture); return true; }
            catch (OverflowException) { return false; }
        }

        return false;
    }

    internal static string ConvertToInvariantString(this object value)
    {
        return value switch
        {
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    internal static bool IsDateTimeValue(this object? value, out long ticks)
    {
        ticks = 0;
        if (value is string valueString)
        {
            if (DateTimeOffset.TryParse(valueString, out var actualDateTime))
            {
                ticks = actualDateTime.ToUniversalTime().Ticks;
                return true;
            }

            if (TimeOnly.TryParse(valueString, out var actualTimeOnly))
            {
                ticks = actualTimeOnly.Ticks;
                return true;
            }

            return false;
        }

        if (value is DateTime actualValueDateTime)
        {
            ticks = actualValueDateTime.ToUniversalTime().Ticks;
            return true;
        }

        if (value is DateOnly actualValueDateOnly)
        {
            ticks = actualValueDateOnly.ToDateTime(TimeOnly.MinValue).Ticks;
            return true;
        }

        if (value is TimeOnly actualValueTimeOnly)
        {
            ticks = actualValueTimeOnly.Ticks;
            return true;
        }

        if (value is DateTimeOffset actualValueDateTimeOffset)
        {
            ticks = actualValueDateTimeOffset.ToUniversalTime().Ticks;
            return true;
        }

        return false;
    }
}
