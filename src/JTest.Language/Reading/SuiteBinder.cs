using System.Text.Json;
using JTest.Language.Diagnostics;
using JTest.Language.Documents;
using JTest.Language.Expressions;

namespace JTest.Language.Reading;

/// <summary>Static binding of a suite document root.</summary>
internal static class SuiteBinder
{
    private static readonly HashSet<string> RootProperties =
        new(StringComparer.Ordinal) { "jtest", "info", "using", "env", "globals", "secrets", "tests" };

    private static readonly HashSet<string> InfoProperties =
        new(StringComparer.Ordinal) { "name", "description" };

    private static readonly HashSet<string> CaseProperties =
        new(StringComparer.Ordinal) { "name", "description", "steps", "datasets" };

    private static readonly HashSet<string> DatasetProperties =
        new(StringComparer.Ordinal) { "name", "case" };

    private static readonly HashSet<string> NoBindings = new(StringComparer.Ordinal);

    internal static JTestSuiteDocument? Bind(
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

        var info = BindInfo(root, source, sink);
        var usingFiles = BindUsing(root, source, sink);
        var env = ElementShape.ElementMap(root, "env", source, string.Empty, sink);
        var globals = ElementShape.ElementMap(root, "globals", source, string.Empty, sink);

        foreach (var entry in env)
        {
            ExpressionSyntax.ValidateValue(entry.Value, source, $"/env/{ElementShape.PointerEscape(entry.Key)}", sink);
        }

        foreach (var entry in globals)
        {
            ExpressionSyntax.ValidateValue(entry.Value, source, $"/globals/{ElementShape.PointerEscape(entry.Key)}", sink);
        }

        var secrets = BindSecrets(root, source, sink);
        var tests = BindTests(root, source, sink);
        if (tests is null)
        {
            return null;
        }

        return new JTestSuiteDocument(languageVersion, info, usingFiles, env, globals, secrets, tests);
    }

    private static SuiteInfo? BindInfo(JsonElement root, string source, ICollection<LanguageDiagnostic> sink)
    {
        var info = ElementShape.OptionalObject(root, "info", source, string.Empty, sink);
        if (info is null)
        {
            return null;
        }

        ElementShape.RejectUnknownProperties(info.Value, InfoProperties, source, "/info", sink);
        return new SuiteInfo(
            ElementShape.OptionalString(info.Value, "name", source, "/info", sink),
            ElementShape.OptionalString(info.Value, "description", source, "/info", sink));
    }

    private static List<string> BindUsing(JsonElement root, string source, ICollection<LanguageDiagnostic> sink)
    {
        var result = new List<string>();
        var array = ElementShape.OptionalArray(root, "using", source, string.Empty, sink);
        if (array is null)
        {
            return result;
        }

        var index = 0;
        foreach (var item in array.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.WrongPropertyType,
                    "Every 'using' entry must be a non-empty file path string.",
                    source,
                    $"/using/{index}");
            }
            else
            {
                result.Add(item.GetString()!);
            }

            index++;
        }

        return result;
    }

    private static List<string> BindSecrets(JsonElement root, string source, ICollection<LanguageDiagnostic> sink)
    {
        var result = new List<string>();
        var array = ElementShape.OptionalArray(root, "secrets", source, string.Empty, sink);
        if (array is null)
        {
            return result;
        }

        var index = 0;
        foreach (var item in array.Value.EnumerateArray())
        {
            var path = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (path is null || !path.StartsWith("$.", StringComparison.Ordinal) || path.Length <= 2)
            {
                Diag.Error(
                    sink,
                    DiagnosticCodes.WrongPropertyType,
                    "Every 'secrets' entry must be a context path string starting with '$.'.",
                    source,
                    $"/secrets/{index}");
            }
            else
            {
                result.Add(path);
            }

            index++;
        }

        return result;
    }

    private static List<TestCaseDefinition>? BindTests(
        JsonElement root,
        string source,
        ICollection<LanguageDiagnostic> sink)
    {
        if (!root.TryGetProperty("tests", out var tests) || tests.ValueKind != JsonValueKind.Array)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.MissingProperty,
                "Required property 'tests' is missing or not an array.",
                source,
                string.Empty);
            return null;
        }

        if (tests.GetArrayLength() == 0)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.EmptyRequiredArray,
                "Property 'tests' must contain at least one test case.",
                source,
                "/tests");
            return null;
        }

        var result = new List<TestCaseDefinition>();
        var index = 0;
        foreach (var item in tests.EnumerateArray())
        {
            var bound = BindCase(item, source, $"/tests/{index}", sink);
            if (bound is not null)
            {
                result.Add(bound);
            }

            index++;
        }

        return result;
    }

    private static TestCaseDefinition? BindCase(
        JsonElement element,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            Diag.Error(sink, DiagnosticCodes.WrongPropertyType, "A test case must be an object.", source, jsonPointer);
            return null;
        }

        ElementShape.RejectUnknownProperties(element, CaseProperties, source, jsonPointer, sink);

        var name = ElementShape.RequiredString(element, "name", source, jsonPointer, sink);
        var description = ElementShape.OptionalString(element, "description", source, jsonPointer, sink);

        if (!element.TryGetProperty("steps", out var steps) || steps.ValueKind != JsonValueKind.Array)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.MissingProperty,
                "Required property 'steps' is missing or not an array.",
                source,
                jsonPointer);
            return null;
        }

        if (steps.GetArrayLength() == 0)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.EmptyRequiredArray,
                "Property 'steps' must contain at least one step.",
                source,
                $"{jsonPointer}/steps");
            return null;
        }

        var boundSteps = StepBinder.BindList(steps, source, $"{jsonPointer}/steps", insideTemplate: false, NoBindings, sink);
        var datasets = BindDatasets(element, source, jsonPointer, sink);

        return name is null
            ? null
            : new TestCaseDefinition(name, description, boundSteps, datasets);
    }

    private static List<DatasetDefinition> BindDatasets(
        JsonElement element,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        var result = new List<DatasetDefinition>();
        var array = ElementShape.OptionalArray(element, "datasets", source, jsonPointer, sink);
        if (array is null)
        {
            return result;
        }

        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var item in array.Value.EnumerateArray())
        {
            var datasetPointer = $"{jsonPointer}/datasets/{index}";
            if (item.ValueKind != JsonValueKind.Object)
            {
                Diag.Error(sink, DiagnosticCodes.WrongPropertyType, "A dataset must be an object.", source, datasetPointer);
                index++;
                continue;
            }

            ElementShape.RejectUnknownProperties(item, DatasetProperties, source, datasetPointer, sink);
            var name = ElementShape.RequiredString(item, "name", source, datasetPointer, sink);
            var caseValues = ElementShape.OptionalObject(item, "case", source, datasetPointer, sink);
            if (!item.TryGetProperty("case", out _))
            {
                Diag.Error(sink, DiagnosticCodes.MissingProperty, "Required property 'case' is missing.", source, datasetPointer);
            }

            if (name is not null && caseValues is not null)
            {
                if (!seenNames.Add(name))
                {
                    Diag.Error(
                        sink,
                        DiagnosticCodes.DuplicateDatasetName,
                        $"Dataset name '{name}' is declared more than once in this case.",
                        source,
                        $"{datasetPointer}/name");
                }

                var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                foreach (var property in caseValues.Value.EnumerateObject())
                {
                    ExpressionSyntax.ValidateValue(
                        property.Value,
                        source,
                        $"{datasetPointer}/case/{ElementShape.PointerEscape(property.Name)}",
                        sink);
                    values[property.Name] = property.Value.Clone();
                }

                result.Add(new DatasetDefinition(name, values));
            }

            index++;
        }

        return result;
    }
}
