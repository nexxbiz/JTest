using JTest.Core.Language.Validation;
using JTest.Core.Models;
using JTest.Core.Utilities;
using Spectre.Console;
using System.Text.Json;

namespace JTest.Core;

public sealed class JTestSuiteValidator(IAnsiConsole console, JsonSerializerOptionsAccessor? serializerOptions = null) : IJTestSuiteValidator
{
    private readonly SchemaValidator schemaValidator = new();

    /// <summary>
    /// Validation must reject anything the runner cannot load. Schema conformance alone does not
    /// guarantee that: the schema describes shape, while the runner also has to bind the document to
    /// its configuration types. Where the two disagreed, `jtest validate` called a file valid and
    /// `jtest run` then failed on it — moving a diagnosable authoring problem to run time. So after
    /// the schema passes, the file is deserialized exactly as a run would, and any failure is reported
    /// as a validation diagnostic (FR-064).
    /// </summary>
    private LanguageDiagnostic? CheckRunnerCanLoad(string json)
    {
        if (serializerOptions is null) return null;

        try
        {
            JsonSerializer.Deserialize<JTestSuite>(json, serializerOptions.Options);
            return null;
        }
        catch (Exception ex)
        {
            return new LanguageDiagnostic("", $"Valid against the schema, but the runner cannot load it: {ex.Message}");
        }
    }

    public async Task<JTestValidationSummary> ValidateJTestSuites(IEnumerable<string> testFilePatterns, IEnumerable<string> categories)
    {
        if (!testFilePatterns.Any())
        {
            throw new InvalidOperationException("Must at least specify one test file pattern.");
        }

        var testFiles = JsonFileSearcher.Search(testFilePatterns, categories);

        if (testFiles.Length == 0)
        {
            throw new InvalidOperationException($"Error: No test files found matching patterns: {string.Join(", ", testFilePatterns)}");
        }

        var validFiles = 0;
        var invalidFiles = 0;

        foreach (var testFile in testFiles)
        {
            console.WriteLine($"\nValidating test file: {testFile}");

            if (!File.Exists(testFile))
            {
                console.WriteLine($"✗ {Path.GetFileName(testFile)} - file not found", new Style(foreground: Color.Red));
                invalidFiles++;
                continue;
            }

            var json = await File.ReadAllTextAsync(testFile);
            var result = schemaValidator.Validate(json);

            if (result.IsValid && CheckRunnerCanLoad(json) is { } loadDiagnostic)
            {
                result = new LanguageValidationResult(false, [loadDiagnostic]);
            }

            if (result.IsValid)
            {
                console.WriteLine($"✓ {Path.GetFileName(testFile)} - valid (JTest language schema {SchemaValidator.SchemaVersion})", new Style(foreground: Color.Green));
                validFiles++;
            }
            else
            {
                console.WriteLine($"✗ {Path.GetFileName(testFile)} - invalid", new Style(foreground: Color.Red));
                foreach (var diagnostic in result.Diagnostics)
                {
                    var where = string.IsNullOrEmpty(diagnostic.Location) ? "(root)" : diagnostic.Location;
                    console.WriteLine($"  {where}: {diagnostic.Message}", new Style(foreground: Color.Red));
                }
                invalidFiles++;
            }
        }

        console.WriteLine($"\nVALIDATION SUMMARY");
        console.WriteLine($"Files processed: {validFiles + invalidFiles}");
        console.WriteLine($"Valid files: {validFiles}");
        console.WriteLine($"Invalid files: {invalidFiles}");
        console.WriteLine(
            invalidFiles > 0 ? "Validation completed with errors." : "All files are valid.",
            new Style(foreground: invalidFiles > 0 ? Color.Yellow : Color.Green));

        return new JTestValidationSummary(validFiles, invalidFiles);
    }
}
