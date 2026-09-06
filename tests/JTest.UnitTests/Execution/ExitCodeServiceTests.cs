using JTest.Core.Execution;
using JTest.Core.Tracing;
using Xunit;

namespace JTest.UnitTests.Execution;

public class ExitCodeServiceTests
{
    private static Rollup Counts(
        int passed = 0, int failed = 0, int errored = 0,
        int cancelled = 0, int timedOut = 0, int skipped = 0) => new()
    {
        Total = passed + failed + errored + cancelled + timedOut + skipped,
        Passed = passed,
        Failed = failed,
        Errored = errored,
        Cancelled = cancelled,
        TimedOut = timedOut,
        Skipped = skipped
    };

    [Fact]
    public void AllPassed_IsSuccess() =>
        Assert.Equal(0, ExitCodeService.From(Counts(passed: 3)));

    [Fact]
    public void CaseFailure_IsTestFailure() =>
        Assert.Equal(1, ExitCodeService.From(Counts(passed: 1, failed: 1)));

    [Fact]
    public void ErroredNode_IsExecutionError() =>
        Assert.Equal(2, ExitCodeService.From(Counts(errored: 1)));

    [Fact]
    public void Cancelled_IsAborted() =>
        Assert.Equal(4, ExitCodeService.From(Counts(cancelled: 1)));

    [Fact]
    public void TimedOut_IsAborted() =>
        Assert.Equal(4, ExitCodeService.From(Counts(timedOut: 1)));

    [Fact]
    public void EmptyDiscovery_IsExecutionError() =>
        Assert.Equal(2, ExitCodeService.From(Counts(), emptyDiscovery: true));

    [Fact]
    public void ValidationError_IsValidationCode() =>
        Assert.Equal(3, ExitCodeService.From(Counts(passed: 2), hadValidationError: true));

    // Precedence: execution error (2) > validation (3) > aborted (4) > test failure (1)
    [Fact]
    public void ExecutionError_Outranks_TestFailure() =>
        Assert.Equal(2, ExitCodeService.From(Counts(failed: 1, errored: 1)));

    [Fact]
    public void ExecutionError_Outranks_Validation() =>
        Assert.Equal(2, ExitCodeService.From(Counts(errored: 1), hadValidationError: true));

    [Fact]
    public void Validation_Outranks_Aborted_And_TestFailure() =>
        Assert.Equal(3, ExitCodeService.From(Counts(failed: 1, cancelled: 1), hadValidationError: true));

    [Fact]
    public void Aborted_Outranks_TestFailure() =>
        Assert.Equal(4, ExitCodeService.From(Counts(failed: 1, timedOut: 1)));
}
