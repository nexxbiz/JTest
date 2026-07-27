using System.Text.Json;
using JTest.Language.Diagnostics;
using JTest.Language.Documents;
using JTest.Language.Expressions;
using JTest.Language.Scopes;

namespace JTest.Language.Reading;

/// <summary>
/// Static binding of step objects into the closed set of step definitions.
/// Binding is fail-closed: every rejected shape produces a diagnostic and
/// never a partially trusted step.
/// </summary>
internal static class StepBinder
{
    private static readonly HashSet<string> HttpProperties = Known(
        "method", "url", "headers", "query", "body", "file", "formFiles", "timeoutMs");

    private static readonly HashSet<string> AssertProperties = Known();
    private static readonly HashSet<string> WaitProperties = Known("ms");
    private static readonly HashSet<string> UseProperties = Known("template", "with");
    private static readonly HashSet<string> ForProperties = Known("items", "as", "indexAs", "steps", "delayMs");
    private static readonly HashSet<string> WhileProperties = Known("condition", "timeoutMs", "delayMs", "steps");
    private static readonly HashSet<string> FormFileProperties =
        new(StringComparer.Ordinal) { "name", "path", "contentType" };

    internal static List<StepDefinition> BindList(
        JsonElement arrayElement,
        string source,
        string jsonPointer,
        bool insideTemplate,
        IReadOnlySet<string> enclosingBindings,
        ICollection<LanguageDiagnostic> sink)
    {
        var steps = new List<StepDefinition>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;

        foreach (var item in arrayElement.EnumerateArray())
        {
            var stepPointer = $"{jsonPointer}/{index}";
            var step = Bind(item, source, stepPointer, insideTemplate, enclosingBindings, sink);
            if (step is not null)
            {
                if (step.Id is not null && !seenIds.Add(step.Id))
                {
                    Diag.Error(
                        sink,
                        DiagnosticCodes.DuplicateStepId,
                        $"Step id '{step.Id}' is declared more than once in this frame.",
                        source,
                        $"{stepPointer}/id");
                }

                steps.Add(step);
            }

            index++;
        }

        return steps;
    }

    private static StepDefinition? Bind(
        JsonElement element,
        string source,
        string jsonPointer,
        bool insideTemplate,
        IReadOnlySet<string> enclosingBindings,
        ICollection<LanguageDiagnostic> sink)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            Diag.Error(sink, DiagnosticCodes.WrongPropertyType, "A step must be an object.", source, jsonPointer);
            return null;
        }

        var type = ElementShape.RequiredString(element, "type", source, jsonPointer, sink);
        if (type is null)
        {
            return null;
        }

        return type switch
        {
            "http" => BindHttp(element, source, jsonPointer, insideTemplate, sink),
            "assert" => BindAssert(element, source, jsonPointer, insideTemplate, sink),
            "wait" => BindWait(element, source, jsonPointer, insideTemplate, sink),
            "use" => BindUse(element, source, jsonPointer, insideTemplate, sink),
            "for" => BindFor(element, source, jsonPointer, insideTemplate, enclosingBindings, sink),
            "while" => BindWhile(element, source, jsonPointer, insideTemplate, enclosingBindings, sink),
            _ => UnknownStep(type, source, jsonPointer, sink),
        };
    }

    private static StepDefinition? UnknownStep(
        string type,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        Diag.Error(
            sink,
            DiagnosticCodes.UnknownStepType,
            $"Unknown step type '{type}'.",
            source,
            $"{jsonPointer}/type",
            "Known step types: assert, for, http, use, wait, while.");
        return null;
    }

    private static HttpStepDefinition? BindHttp(
        JsonElement element,
        string source,
        string jsonPointer,
        bool insideTemplate,
        ICollection<LanguageDiagnostic> sink)
    {
        ElementShape.RejectUnknownProperties(element, HttpProperties, source, jsonPointer, sink);
        var parts = CommonStepParts.Bind(element, source, jsonPointer, insideTemplate, sink);

        var method = ElementShape.RequiredString(element, "method", source, jsonPointer, sink);
        var url = ElementShape.RequiredString(element, "url", source, jsonPointer, sink);
        if (method is null || url is null)
        {
            return null;
        }

        ExpressionSyntax.ValidateString(method, source, $"{jsonPointer}/method", sink);
        ExpressionSyntax.ValidateString(url, source, $"{jsonPointer}/url", sink);

        var headers = ElementShape.StringMap(element, "headers", source, jsonPointer, sink);
        foreach (var header in headers)
        {
            ExpressionSyntax.ValidateString(
                header.Value, source, $"{jsonPointer}/headers/{ElementShape.PointerEscape(header.Key)}", sink);
        }

        var query = ElementShape.StringMap(element, "query", source, jsonPointer, sink);
        foreach (var parameter in query)
        {
            ExpressionSyntax.ValidateString(
                parameter.Value, source, $"{jsonPointer}/query/{ElementShape.PointerEscape(parameter.Key)}", sink);
        }

        JsonElement? body = element.TryGetProperty("body", out var bodyElement) ? bodyElement.Clone() : null;
        if (body is not null)
        {
            ExpressionSyntax.ValidateValue(body.Value, source, $"{jsonPointer}/body", sink);
        }

        var file = ElementShape.OptionalString(element, "file", source, jsonPointer, sink);
        if (file is not null)
        {
            ExpressionSyntax.ValidateString(file, source, $"{jsonPointer}/file", sink);
        }

        var formFiles = BindFormFiles(element, source, jsonPointer, sink);

        var bodySources = (body is not null ? 1 : 0) + (file is not null ? 1 : 0) + (formFiles.Count > 0 ? 1 : 0);
        if (bodySources > 1)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.ConflictingBodySources,
                "An http step may declare only one of 'body', 'file', or 'formFiles'.",
                source,
                jsonPointer);
        }

        var timeoutMs = ElementShape.OptionalNumber(element, "timeoutMs", source, jsonPointer, sink, min: 1);

        return new HttpStepDefinition(
            parts.Id, parts.Name, parts.Description, parts.Save, parts.Assert,
            method, url, headers, query, body, file, formFiles, timeoutMs);
    }

    private static List<HttpFormFileDefinition> BindFormFiles(
        JsonElement element,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        var result = new List<HttpFormFileDefinition>();
        var array = ElementShape.OptionalArray(element, "formFiles", source, jsonPointer, sink);
        if (array is null)
        {
            return result;
        }

        var index = 0;
        foreach (var item in array.Value.EnumerateArray())
        {
            var itemPointer = $"{jsonPointer}/formFiles/{index}";
            if (item.ValueKind != JsonValueKind.Object)
            {
                Diag.Error(sink, DiagnosticCodes.WrongPropertyType, "A form file must be an object.", source, itemPointer);
                index++;
                continue;
            }

            ElementShape.RejectUnknownProperties(item, FormFileProperties, source, itemPointer, sink);
            var name = ElementShape.RequiredString(item, "name", source, itemPointer, sink);
            var path = ElementShape.RequiredString(item, "path", source, itemPointer, sink);
            var contentType = ElementShape.OptionalString(item, "contentType", source, itemPointer, sink);
            if (name is not null && path is not null)
            {
                ExpressionSyntax.ValidateString(path, source, $"{itemPointer}/path", sink);
                result.Add(new HttpFormFileDefinition(name, path, contentType));
            }

            index++;
        }

        return result;
    }

    private static AssertStepDefinition? BindAssert(
        JsonElement element,
        string source,
        string jsonPointer,
        bool insideTemplate,
        ICollection<LanguageDiagnostic> sink)
    {
        ElementShape.RejectUnknownProperties(element, AssertProperties, source, jsonPointer, sink);
        var parts = CommonStepParts.Bind(element, source, jsonPointer, insideTemplate, sink);

        if (parts.Assert.Count == 0)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.EmptyRequiredArray,
                "An assert step must declare at least one assertion.",
                source,
                jsonPointer);
            return null;
        }

        return new AssertStepDefinition(parts.Id, parts.Name, parts.Description, parts.Save, parts.Assert);
    }

    private static WaitStepDefinition? BindWait(
        JsonElement element,
        string source,
        string jsonPointer,
        bool insideTemplate,
        ICollection<LanguageDiagnostic> sink)
    {
        ElementShape.RejectUnknownProperties(element, WaitProperties, source, jsonPointer, sink);
        var parts = CommonStepParts.Bind(element, source, jsonPointer, insideTemplate, sink);

        if (!element.TryGetProperty("ms", out var ms))
        {
            Diag.Error(sink, DiagnosticCodes.MissingProperty, "Required property 'ms' is missing.", source, jsonPointer);
            return null;
        }

        if (ms.ValueKind == JsonValueKind.Number)
        {
            if (ms.GetDouble() < 0)
            {
                Diag.Error(sink, DiagnosticCodes.ValueOutOfRange, "Property 'ms' must be at least 0.", source, $"{jsonPointer}/ms");
                return null;
            }
        }
        else if (ms.ValueKind == JsonValueKind.String && ExpressionSyntax.ContainsToken(ms.GetString() ?? string.Empty))
        {
            ExpressionSyntax.ValidateString(ms.GetString() ?? string.Empty, source, $"{jsonPointer}/ms", sink);
        }
        else
        {
            Diag.Error(
                sink,
                DiagnosticCodes.WrongPropertyType,
                "Property 'ms' must be a non-negative number or an expression string.",
                source,
                $"{jsonPointer}/ms");
            return null;
        }

        return new WaitStepDefinition(parts.Id, parts.Name, parts.Description, parts.Save, parts.Assert, ms.Clone());
    }

    private static UseStepDefinition? BindUse(
        JsonElement element,
        string source,
        string jsonPointer,
        bool insideTemplate,
        ICollection<LanguageDiagnostic> sink)
    {
        ElementShape.RejectUnknownProperties(element, UseProperties, source, jsonPointer, sink);
        var parts = CommonStepParts.Bind(element, source, jsonPointer, insideTemplate, sink);

        var template = ElementShape.RequiredString(element, "template", source, jsonPointer, sink);
        if (template is null)
        {
            return null;
        }

        var with = ElementShape.ElementMap(element, "with", source, jsonPointer, sink);
        foreach (var argument in with)
        {
            ExpressionSyntax.ValidateValue(
                argument.Value, source, $"{jsonPointer}/with/{ElementShape.PointerEscape(argument.Key)}", sink);
        }

        return new UseStepDefinition(parts.Id, parts.Name, parts.Description, parts.Save, parts.Assert, template, with);
    }

    private static ForStepDefinition? BindFor(
        JsonElement element,
        string source,
        string jsonPointer,
        bool insideTemplate,
        IReadOnlySet<string> enclosingBindings,
        ICollection<LanguageDiagnostic> sink)
    {
        ElementShape.RejectUnknownProperties(element, ForProperties, source, jsonPointer, sink);
        var parts = CommonStepParts.Bind(element, source, jsonPointer, insideTemplate, sink);

        if (!element.TryGetProperty("items", out var items))
        {
            Diag.Error(sink, DiagnosticCodes.MissingProperty, "Required property 'items' is missing.", source, jsonPointer);
            return null;
        }

        if (items.ValueKind == JsonValueKind.String && ExpressionSyntax.ContainsToken(items.GetString() ?? string.Empty))
        {
            ExpressionSyntax.ValidateString(items.GetString() ?? string.Empty, source, $"{jsonPointer}/items", sink);
        }
        else if (items.ValueKind == JsonValueKind.Array)
        {
            ExpressionSyntax.ValidateValue(items, source, $"{jsonPointer}/items", sink);
        }
        else
        {
            Diag.Error(
                sink,
                DiagnosticCodes.WrongPropertyType,
                "Property 'items' must be an array or an expression string.",
                source,
                $"{jsonPointer}/items");
            return null;
        }

        var itemBinding = BindLoopName(element, "as", "item", source, jsonPointer, enclosingBindings, sink);
        var indexBinding = BindLoopName(element, "indexAs", "index", source, jsonPointer, enclosingBindings, sink);

        var delayMs = ElementShape.OptionalNumber(element, "delayMs", source, jsonPointer, sink, min: 0);

        var steps = BindRequiredChildSteps(
            element, source, jsonPointer, insideTemplate,
            Combine(enclosingBindings, itemBinding, indexBinding), sink);
        if (steps is null)
        {
            return null;
        }

        return new ForStepDefinition(
            parts.Id, parts.Name, parts.Description, parts.Save, parts.Assert,
            items.Clone(), itemBinding, indexBinding, steps, delayMs);
    }

    private static WhileStepDefinition? BindWhile(
        JsonElement element,
        string source,
        string jsonPointer,
        bool insideTemplate,
        IReadOnlySet<string> enclosingBindings,
        ICollection<LanguageDiagnostic> sink)
    {
        ElementShape.RejectUnknownProperties(element, WhileProperties, source, jsonPointer, sink);
        var parts = CommonStepParts.Bind(element, source, jsonPointer, insideTemplate, sink);

        if (!element.TryGetProperty("condition", out var conditionElement))
        {
            Diag.Error(sink, DiagnosticCodes.MissingProperty, "Required property 'condition' is missing.", source, jsonPointer);
            return null;
        }

        var condition = AssertionBinder.Bind(conditionElement, source, $"{jsonPointer}/condition", sink);

        var timeoutMs = element.TryGetProperty("timeoutMs", out _)
            ? ElementShape.OptionalNumber(element, "timeoutMs", source, jsonPointer, sink, min: 1)
            : MissingTimeout(source, jsonPointer, sink);

        var delayMs = ElementShape.OptionalNumber(element, "delayMs", source, jsonPointer, sink, min: 0);

        var steps = BindRequiredChildSteps(element, source, jsonPointer, insideTemplate, enclosingBindings, sink);
        if (condition is null || timeoutMs is null || steps is null)
        {
            return null;
        }

        return new WhileStepDefinition(
            parts.Id, parts.Name, parts.Description, parts.Save, parts.Assert,
            condition, timeoutMs.Value, delayMs, steps);
    }

    private static double? MissingTimeout(string source, string jsonPointer, ICollection<LanguageDiagnostic> sink)
    {
        Diag.Error(sink, DiagnosticCodes.MissingProperty, "Required property 'timeoutMs' is missing.", source, jsonPointer);
        return null;
    }

    private static List<StepDefinition>? BindRequiredChildSteps(
        JsonElement element,
        string source,
        string jsonPointer,
        bool insideTemplate,
        IReadOnlySet<string> enclosingBindings,
        ICollection<LanguageDiagnostic> sink)
    {
        if (!element.TryGetProperty("steps", out var stepsElement) || stepsElement.ValueKind != JsonValueKind.Array)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.MissingProperty,
                "Required property 'steps' is missing or not an array.",
                source,
                jsonPointer);
            return null;
        }

        if (stepsElement.GetArrayLength() == 0)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.EmptyRequiredArray,
                "Property 'steps' must contain at least one step.",
                source,
                $"{jsonPointer}/steps");
            return null;
        }

        return BindList(stepsElement, source, $"{jsonPointer}/steps", insideTemplate, enclosingBindings, sink);
    }

    private static string BindLoopName(
        JsonElement element,
        string property,
        string fallback,
        string source,
        string jsonPointer,
        IReadOnlySet<string> enclosingBindings,
        ICollection<LanguageDiagnostic> sink)
    {
        var name = ElementShape.OptionalString(element, property, source, jsonPointer, sink) ?? fallback;
        if (ScopeNames.All.Contains(name))
        {
            Diag.Error(
                sink,
                DiagnosticCodes.ReservedName,
                $"Loop binding '{name}' is a reserved scope name.",
                source,
                $"{jsonPointer}/{property}");
            return fallback;
        }

        if (enclosingBindings.Contains(name))
        {
            Diag.Warning(
                sink,
                DiagnosticCodes.ShadowedBinding,
                $"Loop binding '{name}' shadows a binding of an enclosing loop.",
                source,
                $"{jsonPointer}/{property}");
        }

        return name;
    }

    private static HashSet<string> Combine(IReadOnlySet<string> enclosing, params string[] names)
    {
        var combined = new HashSet<string>(enclosing, StringComparer.Ordinal);
        foreach (var name in names)
        {
            combined.Add(name);
        }

        return combined;
    }

    private static HashSet<string> Known(params string[] specific)
    {
        var set = new HashSet<string>(CommonStepParts.PropertyNames, StringComparer.Ordinal);
        foreach (var name in specific)
        {
            set.Add(name);
        }

        return set;
    }
}
