using System.Text.Json;
using JTest.Language.Diagnostics;

namespace JTest.Language.Reading;

/// <summary>
/// Static structural checks over <see cref="JsonElement"/> shapes. Every
/// document object is a closed shape: unknown properties are rejected.
/// </summary>
internal static class ElementShape
{
    internal static void RejectUnknownProperties(
        JsonElement element,
        IReadOnlySet<string> known,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!known.Contains(property.Name))
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.UnknownProperty,
                    $"Unknown property '{property.Name}'.",
                    source,
                    $"{jsonPointer}/{PointerEscape(property.Name)}",
                    $"Allowed properties: {string.Join(", ", known.Order(StringComparer.Ordinal))}.");
            }
        }
    }

    internal static string? RequiredString(
        JsonElement element,
        string name,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink,
        bool allowEmpty = false)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            Diag.Error(
                sink,
                DiagnosticCodes.MissingProperty,
                $"Required property '{name}' is missing.",
                source,
                jsonPointer);
            return null;
        }

        if (value.ValueKind != JsonValueKind.String ||
            (!allowEmpty && string.IsNullOrWhiteSpace(value.GetString())))
        {
            Diag.Error(
                sink,
                DiagnosticCodes.WrongPropertyType,
                $"Property '{name}' must be a non-empty string.",
                source,
                $"{jsonPointer}/{PointerEscape(name)}");
            return null;
        }

        return value.GetString();
    }

    internal static string? OptionalString(
        JsonElement element,
        string name,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.WrongPropertyType,
                $"Property '{name}' must be a string.",
                source,
                $"{jsonPointer}/{PointerEscape(name)}");
            return null;
        }

        return value.GetString();
    }

    internal static double? OptionalNumber(
        JsonElement element,
        string name,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink,
        double? min = null)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Number)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.WrongPropertyType,
                $"Property '{name}' must be a number.",
                source,
                $"{jsonPointer}/{PointerEscape(name)}");
            return null;
        }

        var number = value.GetDouble();
        if (min is not null && number < min)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.ValueOutOfRange,
                $"Property '{name}' must be at least {min}.",
                source,
                $"{jsonPointer}/{PointerEscape(name)}");
            return null;
        }

        return number;
    }

    internal static JsonElement? OptionalObject(
        JsonElement element,
        string name,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.WrongPropertyType,
                $"Property '{name}' must be an object.",
                source,
                $"{jsonPointer}/{PointerEscape(name)}");
            return null;
        }

        return value;
    }

    internal static JsonElement? OptionalArray(
        JsonElement element,
        string name,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.WrongPropertyType,
                $"Property '{name}' must be an array.",
                source,
                $"{jsonPointer}/{PointerEscape(name)}");
            return null;
        }

        return value;
    }

    internal static Dictionary<string, string> StringMap(
        JsonElement element,
        string name,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var map = OptionalObject(element, name, source, jsonPointer, sink);
        if (map is null)
        {
            return result;
        }

        foreach (var property in map.Value.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.WrongPropertyType,
                    $"Value of '{property.Name}' must be a string.",
                    source,
                    $"{jsonPointer}/{PointerEscape(name)}/{PointerEscape(property.Name)}");
                continue;
            }

            result[property.Name] = property.Value.GetString() ?? string.Empty;
        }

        return result;
    }

    internal static Dictionary<string, JsonElement> ElementMap(
        JsonElement element,
        string name,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var map = OptionalObject(element, name, source, jsonPointer, sink);
        if (map is null)
        {
            return result;
        }

        foreach (var property in map.Value.EnumerateObject())
        {
            result[property.Name] = property.Value.Clone();
        }

        return result;
    }

    internal static string PointerEscape(string token) =>
        token.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
}
