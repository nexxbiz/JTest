using JTest.Core.Language.Validation;
using JTest.Core.Utilities;
using Spectre.Console;

namespace JTest.Core;

public sealed class JTestSuiteValidator(IAnsiConsole console) : IJTestSuiteValidator
{
    private readonly SchemaValidator schemaValidator = new();

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
