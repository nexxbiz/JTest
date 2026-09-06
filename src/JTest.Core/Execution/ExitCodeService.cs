using JTest.Core.Tracing;

namespace JTest.Core.Execution;

/// <summary>Documented process exit codes for JTest (FR-008, cli-contract.md).</summary>
public enum RunExitCode
{
    Success = 0,
    TestFailure = 1,
    ExecutionError = 2,
    ValidationError = 3,
    Aborted = 4
}

/// <summary>
/// Maps an aggregate run result to a deterministic, class-specific exit code. Precedence when
/// multiple classes occur in one run: ExecutionError(2) &gt; ValidationError(3) &gt; Aborted(4) &gt;
/// TestFailure(1). "No results from a non-empty discovery" is an execution error (FR-003).
/// </summary>
public static class ExitCodeService
{
    public static int From(
        Rollup counts,
        bool hadExecutionError = false,
        bool hadValidationError = false,
        bool emptyDiscovery = false)
    {
        if (hadExecutionError || counts.Errored > 0 || emptyDiscovery)
            return (int)RunExitCode.ExecutionError;

        if (hadValidationError)
            return (int)RunExitCode.ValidationError;

        if (counts.Cancelled > 0 || counts.TimedOut > 0)
            return (int)RunExitCode.Aborted;

        if (counts.Failed > 0)
            return (int)RunExitCode.TestFailure;

        return (int)RunExitCode.Success;
    }
}
