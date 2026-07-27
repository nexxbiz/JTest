using JTest.Language.Diagnostics;
using JTest.Language.Documents;
using JTest.Language.Reading;

namespace JTest.Language.Semantics;

/// <summary>Default fail-closed cross-file semantics validator.</summary>
public sealed class SuiteBundleValidator : ISuiteBundleValidator
{
    /// <inheritdoc />
    public IReadOnlyList<LanguageDiagnostic> Validate(SuiteBundle bundle)
    {
        var sink = new List<LanguageDiagnostic>();
        var templates = CollectTemplates(bundle, sink);

        var caseIndex = 0;
        foreach (var testCase in bundle.Suite.Tests)
        {
            ValidateSteps(
                testCase.Steps,
                templates,
                bundle.SuiteSource,
                $"/tests/{caseIndex}/steps",
                sink);
            caseIndex++;
        }

        foreach (var (source, document) in bundle.TemplateFiles)
        {
            var templateIndex = 0;
            foreach (var template in document.Templates)
            {
                ValidateSteps(
                    template.Steps,
                    templates,
                    source,
                    $"/components/templates/{templateIndex}/steps",
                    sink);
                templateIndex++;
            }
        }

        DetectCycles(templates, bundle, sink);
        return sink;
    }

    private static Dictionary<string, TemplateDefinition> CollectTemplates(
        SuiteBundle bundle,
        ICollection<LanguageDiagnostic> sink)
    {
        var templates = new Dictionary<string, TemplateDefinition>(StringComparer.Ordinal);
        foreach (var (source, document) in bundle.TemplateFiles)
        {
            var index = 0;
            foreach (var template in document.Templates)
            {
                if (!templates.TryAdd(template.Name, template))
                {
                    Diag.Error(
                        sink,
                        DiagnosticCodes.DuplicateTemplateName,
                        $"Template name '{template.Name}' is declared by more than one loaded file.",
                        source,
                        $"/components/templates/{index}/name");
                }

                index++;
            }
        }

        return templates;
    }

    private static void ValidateSteps(
        IReadOnlyList<StepDefinition> steps,
        IReadOnlyDictionary<string, TemplateDefinition> templates,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        var index = 0;
        foreach (var step in steps)
        {
            var stepPointer = $"{jsonPointer}/{index}";
            switch (step)
            {
                case UseStepDefinition use:
                    ValidateUse(use, templates, source, stepPointer, sink);
                    break;
                case ForStepDefinition loop:
                    ValidateSteps(loop.Steps, templates, source, $"{stepPointer}/steps", sink);
                    break;
                case WhileStepDefinition loop:
                    ValidateSteps(loop.Steps, templates, source, $"{stepPointer}/steps", sink);
                    break;
                default:
                    break;
            }

            index++;
        }
    }

    private static void ValidateUse(
        UseStepDefinition use,
        IReadOnlyDictionary<string, TemplateDefinition> templates,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        if (!templates.TryGetValue(use.Template, out var template))
        {
            Diag.Error(
                sink,
                DiagnosticCodes.UnknownTemplate,
                $"Template '{use.Template}' is not declared by any loaded template file.",
                source,
                $"{jsonPointer}/template",
                templates.Count == 0
                    ? "The suite loads no template files; add a 'using' entry."
                    : $"Loaded templates: {string.Join(", ", templates.Keys.Order(StringComparer.Ordinal))}.");
            return;
        }

        foreach (var argument in use.With.Keys)
        {
            if (!template.Parameters.ContainsKey(argument))
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.UnknownTemplateParameter,
                    $"Template '{use.Template}' declares no parameter '{argument}'.",
                    source,
                    $"{jsonPointer}/with/{ElementShape.PointerEscape(argument)}");
            }
        }

        foreach (var parameter in template.Parameters)
        {
            if (parameter.Value.Required &&
                parameter.Value.Default is null &&
                !use.With.ContainsKey(parameter.Key))
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.MissingTemplateParameter,
                    $"Required parameter '{parameter.Key}' of template '{use.Template}' has no argument.",
                    source,
                    jsonPointer);
            }
        }
    }

    private static void DetectCycles(
        IReadOnlyDictionary<string, TemplateDefinition> templates,
        SuiteBundle bundle,
        ICollection<LanguageDiagnostic> sink)
    {
        var states = new Dictionary<string, byte>(StringComparer.Ordinal);
        foreach (var name in templates.Keys)
        {
            if (Visit(name, templates, states, out var cycleName))
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.TemplateCycle,
                    $"Template '{cycleName}' participates in an invocation cycle.",
                    bundle.SuiteSource,
                    string.Empty);
                return;
            }
        }
    }

    private static bool Visit(
        string name,
        IReadOnlyDictionary<string, TemplateDefinition> templates,
        Dictionary<string, byte> states,
        out string cycleName)
    {
        cycleName = name;
        if (states.TryGetValue(name, out var state))
        {
            return state == 1;
        }

        if (!templates.TryGetValue(name, out var template))
        {
            return false;
        }

        states[name] = 1;
        foreach (var referenced in EnumerateUses(template.Steps))
        {
            if (Visit(referenced, templates, states, out cycleName))
            {
                return true;
            }
        }

        states[name] = 2;
        return false;
    }

    private static IEnumerable<string> EnumerateUses(IReadOnlyList<StepDefinition> steps)
    {
        foreach (var step in steps)
        {
            switch (step)
            {
                case UseStepDefinition use:
                    yield return use.Template;
                    break;
                case ForStepDefinition loop:
                    foreach (var nested in EnumerateUses(loop.Steps))
                    {
                        yield return nested;
                    }

                    break;
                case WhileStepDefinition loop:
                    foreach (var nested in EnumerateUses(loop.Steps))
                    {
                        yield return nested;
                    }

                    break;
                default:
                    break;
            }
        }
    }
}
