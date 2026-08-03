using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Templates;
using JTest.Core.Variables;
using NSubstitute;
using Spectre.Console;
using Xunit;

namespace JTest.UnitTests.Execution;

public class FalseGreenTests
{
    private static IAnsiConsole SilentConsole() =>
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(new StringWriter()) });

    [Fact]
    public async Task CrashingSuite_IsCapturedAsErrored_NotDropped()
    {
        var caseExecutor = Substitute.For<IJTestCaseExecutor>();
        var variables = Substitute.For<IVariablesContext>();
        var templates = Substitute.For<ITemplateContext>();
        // Simulate a bad template load / setup crash.
        templates.When(t => t.Load(Arg.Any<JTestSuite>()))
            .Do(_ => throw new InvalidOperationException("bad template"));

        var executor = new JTestSuiteExecutor(caseExecutor, variables, templates, SilentConsole());

        var results = (await executor.Execute(new[] { new JTestSuite { FilePath = "bad.json" } })).ToList();

        var suite = Assert.Single(results);
        Assert.True(suite.Errored);
        Assert.False(suite.Success);
        Assert.Equal("bad template", suite.ExecutionError);
    }

    [Fact]
    public async Task CrashingSuite_DrivesExitCode2()
    {
        var templates = Substitute.For<ITemplateContext>();
        templates.When(t => t.Load(Arg.Any<JTestSuite>()))
            .Do(_ => throw new InvalidOperationException("boom"));

        var executor = new JTestSuiteExecutor(
            Substitute.For<IJTestCaseExecutor>(),
            Substitute.For<IVariablesContext>(),
            templates,
            SilentConsole());

        var results = (await executor.Execute(new[] { new JTestSuite { FilePath = "bad.json" } })).ToList();

        // The central defect: a crash must never exit 0.
        Assert.Equal(2, RunResultEvaluator.ExitCode(results, noFilesMatched: false));
    }
}
