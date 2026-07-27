using JTest.Language.Diagnostics;
using JTest.Language.Reading;
using JTest.Language.Semantics;
using JTest.Language.Tests.TestSupport;

namespace JTest.Language.Tests.Reading;

/// <summary>
/// Fixture-driven proof that every shipped diagnostic code fires with its
/// exact code and JSON pointer, and that valid documents produce none.
/// </summary>
[TestClass]
public sealed class FixtureValidationTests
{
    [TestMethod]
    public void EveryFixtureProducesExactlyTheExpectedDiagnostics()
    {
        var failures = new List<string>();
        foreach (var entry in FixtureIndex.Load())
        {
            var diagnostics = Run(entry, out var isValid);

            foreach (var expectation in entry.Expect)
            {
                if (!diagnostics.Any(d => d.Code == expectation.Code && d.JsonPointer == expectation.Pointer))
                {
                    failures.Add(
                        $"{entry.File}: expected {expectation.Code} at '{expectation.Pointer}' but got " +
                        $"[{string.Join("; ", diagnostics.Select(d => $"{d.Code}@'{d.JsonPointer}'"))}]");
                }
            }

            if (entry.Expect.Count == 0 && diagnostics.Count > 0)
            {
                failures.Add(
                    $"{entry.File}: expected no diagnostics but got " +
                    $"[{string.Join("; ", diagnostics.Select(d => $"{d.Code}@'{d.JsonPointer}'"))}]");
            }

            if (isValid != entry.IsValid)
            {
                failures.Add($"{entry.File}: expected IsValid={entry.IsValid} but was {isValid}.");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void EveryShippedDiagnosticCodeIsCoveredByAFixture()
    {
        var shipped = typeof(DiagnosticCodes)
            .GetFields()
            .Select(f => (string)f.GetValue(null)!)
            .Where(code => code != DiagnosticCodes.InternalValidationFailure)
            .ToHashSet(StringComparer.Ordinal);

        var covered = FixtureIndex.Load()
            .SelectMany(e => e.Expect)
            .Select(e => e.Code)
            .ToHashSet(StringComparer.Ordinal);

        var uncovered = shipped.Except(covered).Order(StringComparer.Ordinal).ToList();
        Assert.AreEqual(0, uncovered.Count, $"Codes without fixtures: {string.Join(", ", uncovered)}");
    }

    private static IReadOnlyList<LanguageDiagnostic> Run(FixtureIndexEntry entry, out bool isValid)
    {
        var json = FixtureIndex.ReadFixture(entry.File);

        if (entry.Kind == "templates")
        {
            var reader = new TemplateFileReader();
            var result = reader.Read(entry.File, json);
            isValid = result.IsValid;
            return result.Diagnostics;
        }

        var suiteReader = new SuiteDocumentReader();
        var suiteResult = suiteReader.Read(entry.File, json);
        if (!entry.Bundle || suiteResult.Document is null)
        {
            isValid = suiteResult.IsValid;
            return suiteResult.Diagnostics;
        }

        var templateReader = new TemplateFileReader();
        var templateFiles = new List<(string, JTest.Language.Documents.TemplateFileDocument)>();
        var diagnostics = new List<LanguageDiagnostic>(suiteResult.Diagnostics);
        foreach (var reference in suiteResult.Document.Using)
        {
            var path = FixtureIndex.ResolveSibling(entry.File, reference);
            var templateResult = templateReader.Read(reference, File.ReadAllText(path));
            diagnostics.AddRange(templateResult.Diagnostics);
            if (templateResult.Document is not null)
            {
                templateFiles.Add((reference, templateResult.Document));
            }
        }

        var validator = new SuiteBundleValidator();
        diagnostics.AddRange(validator.Validate(
            new SuiteBundle(entry.File, suiteResult.Document, templateFiles)));

        isValid = diagnostics.All(d => d.Severity != DiagnosticSeverity.Error);
        return diagnostics;
    }
}
