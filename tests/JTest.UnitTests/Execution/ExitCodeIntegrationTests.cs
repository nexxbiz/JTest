using JTest.Core.Execution;
using JTest.Core.Models;
using Xunit;

namespace JTest.UnitTests.Execution;

public class ExitCodeIntegrationTests
{
    private static JTestCaseResult Pass() => new();

    private static JTestCaseResult Fail()
    {
        var c = new JTestCaseResult();
        c.AddError("assertion failed");
        return c;
    }

    private static JTestSuiteExecutionResult Suite(params JTestCaseResult[] cases) =>
        new("f.json", null, null, cases);

    private static JTestSuiteExecutionResult Errored() =>
        new("f.json", null, null, Array.Empty<JTestCaseResult>()) { ExecutionError = "crash" };

    [Fact]
    public void AllPassed_ExitsZero() =>
        Assert.Equal(0, RunResultEvaluator.ExitCode(new[] { Suite(Pass(), Pass()) }, noFilesMatched: false));

    [Fact]
    public void AnyCaseFailure_ExitsOne() =>
        Assert.Equal(1, RunResultEvaluator.ExitCode(new[] { Suite(Pass(), Fail()) }, noFilesMatched: false));

    [Fact]
    public void ErroredSuite_ExitsTwo() =>
        Assert.Equal(2, RunResultEvaluator.ExitCode(new[] { Errored() }, noFilesMatched: false));

    [Fact]
    public void NoFilesMatched_ExitsTwo_NotSuccess() =>
        Assert.Equal(2, RunResultEvaluator.ExitCode(Array.Empty<JTestSuiteExecutionResult>(), noFilesMatched: true));

    [Fact]
    public void FilesMatchedButZeroResults_ExitsTwo() =>
        Assert.Equal(2, RunResultEvaluator.ExitCode(new[] { Suite() }, noFilesMatched: false));

    [Fact]
    public void ExecutionError_Outranks_TestFailure() =>
        Assert.Equal(2, RunResultEvaluator.ExitCode(new[] { Suite(Fail()), Errored() }, noFilesMatched: false));
}
