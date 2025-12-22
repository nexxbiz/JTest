using JTest.Core.Utilities;
using Spectre.Console;
using System.Text.Json;

namespace JTest.Core;

public sealed class JTestSuiteValidator(IAnsiConsole console) : IJTestSuiteValidator
{
    public async Task ValidateJTestSuites(IEnumerable<string> testFilePatterns, IEnumerable<string> categories)
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

        var processedFiles = 0;
        var validFiles = 0;
        var invalidFiles = 0;

        foreach (var testFile in testFiles)
        {
            try
            {
                if (!File.Exists(testFile))
                {
                    throw new ArgumentException($"Error: Test file not found: {testFile}");
                }

                console.WriteLine($"\n{'=' * 50}");
                console.WriteLine($"Validating test file: {testFile}");
                console.WriteLine($"{'=' * 50}");

                var json = await File.ReadAllTextAsync(testFile);

                // Basic JSON syntax validation
                JsonDocument.Parse(json);
                console.WriteLine("Valid JSON syntax");

                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                // Test suite validation
                ValidateTestSuite(root);

                console.WriteLine("Valid JTEST schema");
                console.WriteLine($"✓ {Path.GetFileName(testFile)} - Valid");

                processedFiles++;
            }
            catch (JsonException ex)
            {
                console.WriteLine($"Invalid JSON: {ex.Message}", new Style(foreground: Color.Red));
                console.WriteLine($"✗ {Path.GetFileName(testFile)} - Error");
                invalidFiles++;
            }
            catch (ArgumentException ex)
            {
                console.WriteLine($"Validation error: {ex.Message}", new Style(foreground: Color.Red));
                console.WriteLine($"✗ {Path.GetFileName(testFile)} - Error");
                invalidFiles++;
            }
            catch (Exception ex)
            {
                console.WriteException(ex, ExceptionFormats.NoStackTrace);
                console.WriteLine($"✗ {Path.GetFileName(testFile)} - Error");
                invalidFiles++;
            }
        }

        // Display overall summary
        console.WriteLine($"\n{'=' * 50}");
        console.WriteLine($"VALIDATION SUMMARY");
        console.WriteLine($"{'=' * 50}");
        console.WriteLine($"Files processed: {processedFiles}");
        console.WriteLine($"Valid files: {validFiles}");
        console.WriteLine($"Invalid files: {invalidFiles}");

        if (invalidFiles > 0)
        {
            console.WriteLine("Validation completed with errors.", new Style(foreground: Color.Yellow));
        }
        else
        {
            console.WriteLine("All files are valid.", new Style(foreground: Color.Green));
        }
    }

    private static void ValidateTestSuite(JsonElement root)
    {
        // Validate required fields
        if (!root.TryGetProperty("version", out _))
            throw new ArgumentException("Test suite misses required property 'version'");

        if (!root.TryGetProperty("tests", out var testsElement) || testsElement.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("Test suite misses required property 'tests', or 'tests' is not an array");

        // Validate each test case in the tests array
        foreach (var testElement in testsElement.EnumerateArray())
        {
            if (!testElement.TryGetProperty("name", out _))
                throw new ArgumentException("Test case misses required property 'name'");

            if (!testElement.TryGetProperty("steps", out var stepsElement) || stepsElement.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("Test case misses required property 'steps'");
        }
    }
}
