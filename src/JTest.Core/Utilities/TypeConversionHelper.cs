using System.Globalization;
using System.Text.Json;

namespace JTest.Core.Utilities;

internal static class TypeConversionHelper
{
    internal static double ConvertToDouble(object? value)
    {
        if (value is null)
        {
            throw new FormatException("Cannot convert null to a numeric value");
        }

        if (value is JsonElement element)
        {
            return ConvertJsonElementToDouble(element);
        }

        if (IsNumeric(value))
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        throw new FormatException($"Cannot convert '{value}' to a numeric value");
    }

    internal static double ConvertJsonElementToDouble(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.GetDouble();

        if (element.ValueKind == JsonValueKind.String)
            return Convert.ToDouble(element.GetString(), CultureInfo.InvariantCulture);

        throw new FormatException($"Cannot convert JsonElement '{element}' to a numeric value");
    }

    internal static bool IsNumeric(object value)
    {
        return value is double or float or decimal or int or long or short or byte or sbyte or uint or ulong or ushort;
    }

    internal static string ConvertToInvariantString(object value)
    {
        return value switch
        {
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    internal static bool IsDateTimeValue(object? value, out long ticks)
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
