using JTest.Cli.Commands;
using JTest.Cli.Services;
using JTest.Cli.Settings;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Templates;
using JTest.Core.Utilities;
using JTest.Core.Variables;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console;
using Spectre.Console.Cli;

namespace JTest.UnitTests.Cli;

/// <summary>
/// A suite that cannot be loaded (bad JSON, unknown step type, unknown assertion operator) must be
/// captured as an errored suite — not abort the whole run. Previously every discovered file was
/// deserialized eagerly, outside the executor's per-suite boundary, so one malformed file threw
/// before execution: the other suites never ran and no trace or report was written at all (FR-002).
/// </summary>
public class SuiteLoadIsolationTests : IDisposable
{
    private readonly string _directory =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"jtest-load-{Guid.NewGuid():N}")).FullName;

    // Suite discovery globs relative to the working directory. Tests share one collection
    // (CollectionPerAssembly), so they do not run concurrently and this is safe.
    private readonly string _originalDirectory = Directory.GetCurrentDirectory();

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task BadSuite_IsErrored_AndGoodSuiteStillRuns_AndArtifactsAreWritten()
    {
        WriteSuite("good.json", """
        { "version": "1.0", "tests": [ { "name": "ok", "steps": [ { "type": "wait", "ms": 1 } ] } ] }
        """);

        WriteSuite("bad.json", """
        { "version": "1.0", "tests": [ { "name": "typo", "steps": [
          { "type": "assert", "assert": [ { "op": "isEqual", "actualValue": 1, "expectedValue": 1 } ] } ] } ] }
        """);

        var executed = new List<JTestSuite>();
        var executor = Substitute.For<IJTestSuiteExecutor>();
        executor.Execute(Arg.Any<IEnumerable<JTestSuite>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                executed.AddRange(call.Arg<IEnumerable<JTestSuite>>());
                return Task.FromResult(executed.Select(s =>
                    new JTestSuiteExecutionResult(s.FilePath!, s.Info?.Name, null, Array.Empty<JTestCaseResult>())));
            });

        var outputDirectory = Path.Combine(_directory, "artifacts");
        var exitCode = await Run(executor, outputDirectory);

        // The loadable suite still executed...
        var executedSuite = Assert.Single(executed);
        Assert.EndsWith("good.json", executedSuite.FilePath);

        // ...the run failed because of the unloadable one...
        Assert.Equal(2, exitCode);

        // ...and the artifacts exist, naming the bad file and the bad operator.
        var trace = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "trace.json"));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "report.html")));
        Assert.Contains("bad.json", trace);
        Assert.Contains("isEqual", trace);
        Assert.Contains("errored", trace);
    }

    [Fact]
    public async Task MalformedJson_IsAlsoIsolated()
    {
        WriteSuite("good.json", """
        { "version": "1.0", "tests": [ { "name": "ok", "steps": [ { "type": "wait", "ms": 1 } ] } ] }
        """);
        WriteSuite("broken.json", "{ not json ");

        var executor = Substitute.For<IJTestSuiteExecutor>();
        executor.Execute(Arg.Any<IEnumerable<JTestSuite>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<IEnumerable<JTestSuite>>().Select(s =>
                new JTestSuiteExecutionResult(s.FilePath!, s.Info?.Name, null, Array.Empty<JTestCaseResult>()))));

        var outputDirectory = Path.Combine(_directory, "artifacts");
        var exitCode = await Run(executor, outputDirectory);

        Assert.Equal(2, exitCode);
        Assert.Contains("broken.json", await File.ReadAllTextAsync(Path.Combine(outputDirectory, "trace.json")));
    }

    private void WriteSuite(string name, string json) =>
        File.WriteAllText(Path.Combine(_directory, name), json);

    private async Task<int> Run(IJTestSuiteExecutor executor, string outputDirectory)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new HttpClient());
        services.AddSingleton(AnsiConsole.Console);
        services.AddSingleton<JTest.Core.TypeDescriptors.ITypeDescriptorRegistryProvider,
            JTest.Core.TypeDescriptors.TypeDescriptorRegistryProvider>();
        services.AddSingleton(Substitute.For<ITemplateContext>());
        services.AddSingleton<JsonSerializerOptionsAccessor>();
        var provider = services.BuildServiceProvider();

        var command = new RunCommand(
            AnsiConsole.Console,
            Substitute.For<IErrorHandlingService>(),
            Substitute.For<IJTestSuiteExecutionResultProcessor>(),
            executor,
            new VariablesContext(),
            provider.GetRequiredService<ITemplateContext>(),
            provider.GetRequiredService<JsonSerializerOptionsAccessor>());

        Directory.SetCurrentDirectory(_directory);

        var settings = new RunCommandSettings
        {
            TestFilePatterns = ["*.json"],
            OutputDirectoryPath = outputDirectory
        };

        return await command.ExecuteAsync(
            new CommandContext(Array.Empty<string>(), Substitute.For<IRemainingArguments>(), "run", null),
            settings,
            CancellationToken.None);
    }
}
