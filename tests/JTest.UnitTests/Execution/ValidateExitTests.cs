using JTest.Core;
using Spectre.Console;
using Xunit;

namespace JTest.UnitTests.Execution;

public class ValidateExitTests
{
    [Fact]
    public async Task InvalidFile_ReportsHonestCounts_AndMapsToExitThree()
    {
        var cwd = Directory.GetCurrentDirectory();
        var rel = "validate-test-" + Guid.NewGuid().ToString("N");
        var dir = Path.Combine(cwd, rel);
        Directory.CreateDirectory(dir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "valid.json"),
                "{\"version\":\"1.0\",\"tests\":[{\"name\":\"t\",\"steps\":[]}]}");
            await File.WriteAllTextAsync(Path.Combine(dir, "invalid.json"),
                "{\"version\":\"1.0\"}"); // missing required 'tests'

            var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(new StringWriter()) });
            var validator = new JTestSuiteValidator(console);

            var summary = await validator.ValidateJTestSuites(
                new[] { $"{rel}/**/*.json" }, Array.Empty<string>());

            // Honest counts (1.0 always reported 0 valid files).
            Assert.Equal(1, summary.Valid);
            Assert.Equal(1, summary.Invalid);
            Assert.Equal(2, summary.Total);
            Assert.True(summary.HasInvalid);

            // ValidateCommand maps HasInvalid → exit 3 (FR-004).
            var exitCode = summary.HasInvalid ? (int)JTest.Core.Execution.RunExitCode.ValidationError : 0;
            Assert.Equal(3, exitCode);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AllValid_MapsToExitZero()
    {
        var cwd = Directory.GetCurrentDirectory();
        var rel = "validate-test-" + Guid.NewGuid().ToString("N");
        var dir = Path.Combine(cwd, rel);
        Directory.CreateDirectory(dir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "ok.json"),
                "{\"version\":\"1.0\",\"tests\":[{\"name\":\"t\",\"steps\":[]}]}");

            var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(new StringWriter()) });
            var validator = new JTestSuiteValidator(console);

            var summary = await validator.ValidateJTestSuites(
                new[] { $"{rel}/**/*.json" }, Array.Empty<string>());

            Assert.Equal(1, summary.Valid);
            Assert.False(summary.HasInvalid);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// Validation must reject anything the runner cannot load (FR-064). Schema conformance describes
    /// shape only; the runner additionally binds the document to its configuration types. Where the
    /// two disagreed, a file validated clean and then failed at run time — the failure moved from
    /// authoring time to run time and arrived as a crash rather than a diagnostic.
    /// </summary>
    [Fact]
    public async Task FileTheRunnerCannotLoad_IsInvalid_EvenWhenSchemaValid()
    {
        var cwd = Directory.GetCurrentDirectory();
        var rel = "validate-load-" + Guid.NewGuid().ToString("N");
        var dir = Path.Combine(cwd, rel);
        Directory.CreateDirectory(dir);

        try
        {
            // Schema-valid shape, but a formFiles entry is missing the fields the runner requires.
            await File.WriteAllTextAsync(Path.Combine(dir, "unloadable.json"),
                "{\"version\":\"1.0\",\"tests\":[{\"name\":\"t\",\"steps\":[" +
                "{\"type\":\"http\",\"method\":\"POST\",\"url\":\"https://api.test/x\",\"formFiles\":[{}]}]}]}");

            var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(new StringWriter()) });
            var validator = new JTestSuiteValidator(console, TestHelpers.JsonSerializerHelper.OptionsAccessor);

            var summary = await validator.ValidateJTestSuites(
                new[] { $"{rel}/**/*.json" }, Array.Empty<string>());

            Assert.Equal(1, summary.Invalid);
            Assert.True(summary.HasInvalid);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
