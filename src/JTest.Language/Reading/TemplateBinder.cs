using System.Text.Json;
using JTest.Language.Diagnostics;
using JTest.Language.Documents;
using JTest.Language.Expressions;

namespace JTest.Language.Reading;

/// <summary>Static binding of a template file root.</summary>
internal static class TemplateBinder
{
    private static readonly HashSet<string> RootProperties =
        new(StringComparer.Ordinal) { "jtest", "components" };

    private static readonly HashSet<string> ComponentsProperties =
        new(StringComparer.Ordinal) { "templates" };

    private static readonly HashSet<string> TemplateProperties =
        new(StringComparer.Ordinal) { "name", "description", "params", "steps", "output" };

    private static readonly HashSet<string> ParameterProperties =
        new(StringComparer.Ordinal) { "type", "required", "description", "default" };

    private static readonly HashSet<string> ParameterTypes =
        new(StringComparer.Ordinal) { "string", "number", "boolean", "object", "array" };

    private static readonly HashSet<string> NoBindings = new(StringComparer.Ordinal);

    internal static TemplateFileDocument? Bind(
        JsonElement root,
        string source,
        ICollection<LanguageDiagnostic> sink)
    {
        ElementShape.RejectUnknownProperties(root, RootProperties, source, string.Empty, sink);

        var languageVersion = LanguageVersionBinder.Bind(root, source, sink);
        if (languageVersion is null)
        {
            return null;
        }

        if (!root.TryGetProperty("components", out var components) || components.ValueKind != JsonValueKind.Object)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.MissingProperty,
                "Required property 'components' is missing or not an object.",
                source,
                string.Empty);
            return null;
        }

        ElementShape.RejectUnknownProperties(components, ComponentsProperties, source, "/components", sink);

        if (!components.TryGetProperty("templates", out var templates) || templates.ValueKind != JsonValueKind.Array)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.MissingProperty,
                "Required property 'templates' is missing or not an array.",
                source,
                "/components");
            return null;
        }

        var result = new List<TemplateDefinition>();
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var item in templates.EnumerateArray())
        {
            var bound = BindTemplate(item, source, $"/components/templates/{index}", sink);
            if (bound is not null)
            {
                if (!seenNames.Add(bound.Name))
                {
                    Diag.Error(
                        sink,
                        DiagnosticCodes.DuplicateTemplateName,
                        $"Template name '{bound.Name}' is declared more than once.",
                        source,
                        $"/components/templates/{index}/name");
                }

                result.Add(bound);
            }

            index++;
        }

        return new TemplateFileDocument(languageVersion, result);
    }

    private static TemplateDefinition? BindTemplate(
        JsonElement element,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            Diag.Error(sink, DiagnosticCodes.WrongPropertyType, "A template must be an object.", source, jsonPointer);
            return null;
        }

        ElementShape.RejectUnknownProperties(element, TemplateProperties, source, jsonPointer, sink);

        var name = ElementShape.RequiredString(element, "name", source, jsonPointer, sink);
        var description = ElementShape.OptionalString(element, "description", source, jsonPointer, sink);
        var parameters = BindParameters(element, source, jsonPointer, sink);

        if (!element.TryGetProperty("steps", out var steps) ||
            steps.ValueKind != JsonValueKind.Array ||
            steps.GetArrayLength() == 0)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.MissingProperty,
                "Required property 'steps' is missing, not an array, or empty.",
                source,
                jsonPointer);
            return null;
        }

        var boundSteps = StepBinder.BindList(steps, source, $"{jsonPointer}/steps", insideTemplate: true, NoBindings, sink);

        var output = ElementShape.ElementMap(element, "output", source, jsonPointer, sink);
        foreach (var entry in output)
        {
            ExpressionSyntax.ValidateValue(
                entry.Value, source, $"{jsonPointer}/output/{ElementShape.PointerEscape(entry.Key)}", sink);
        }

        return name is null
            ? null
            : new TemplateDefinition(name, description, parameters, boundSteps, output);
    }

    private static Dictionary<string, TemplateParameterDefinition> BindParameters(
        JsonElement element,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        var result = new Dictionary<string, TemplateParameterDefinition>(StringComparer.Ordinal);
        var parameters = ElementShape.OptionalObject(element, "params", source, jsonPointer, sink);
        if (parameters is null)
        {
            return result;
        }

        foreach (var property in parameters.Value.EnumerateObject())
        {
            var parameterPointer = $"{jsonPointer}/params/{ElementShape.PointerEscape(property.Name)}";
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.WrongPropertyType,
                    "A template parameter must be an object.",
                    source,
                    parameterPointer);
                continue;
            }

            ElementShape.RejectUnknownProperties(property.Value, ParameterProperties, source, parameterPointer, sink);

            var type = ElementShape.OptionalString(property.Value, "type", source, parameterPointer, sink) ?? "string";
            if (!ParameterTypes.Contains(type))
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.InvalidEnumValue,
                    $"Parameter type '{type}' is not one of: {string.Join(", ", ParameterTypes.Order(StringComparer.Ordinal))}.",
                    source,
                    $"{parameterPointer}/type");
                continue;
            }

            var required = false;
            if (property.Value.TryGetProperty("required", out var requiredElement))
            {
                if (requiredElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    Diag.Error(
                        sink,
                        DiagnosticCodes.WrongPropertyType,
                        "Property 'required' must be a boolean.",
                        source,
                        $"{parameterPointer}/required");
                    continue;
                }

                required = requiredElement.GetBoolean();
            }

            var description = ElementShape.OptionalString(property.Value, "description", source, parameterPointer, sink);
            JsonElement? defaultValue = property.Value.TryGetProperty("default", out var defaultElement)
                ? defaultElement.Clone()
                : null;

            result[property.Name] = new TemplateParameterDefinition(type, required, description, defaultValue);
        }

        return result;
    }
}
