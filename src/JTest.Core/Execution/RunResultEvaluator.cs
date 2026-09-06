using JTest.Core.Models;
using JTest.Core.Tracing;

namespace JTest.Core.Execution;

/// <summary>
/// Turns a set of suite results into the deterministic process exit code (FR-001/002/003).
/// Shared by <c>RunCommand</c> and its tests so the no-false-green contract is verified directly.
/// </summary>
public static class RunResultEvaluator
{
    /// <param name="noFilesMatched">True when discovery matched no files at all.</param>
    public static int ExitCode(IReadOnlyCollection<JTestSuiteExecutionResult> results, bool noFilesMatched)
    {
        if (noFilesMatched)
            return ExitCodeService.From(Rollup.Empty, emptyDiscovery: true);

        var hadExecutionError = results.Any(r => r.Errored);
        var passed = results.Sum(r => r.CasesPassed);
        var failed = results.Sum(r => r.CasesFailed);

        var counts = new Rollup { Total = passed + failed, Passed = passed, Failed = failed };

        // Files were found but produced zero case results and no error → still a failure (FR-003).
        var emptyDiscovery = !hadExecutionError && passed + failed == 0;

        return ExitCodeService.From(counts, hadExecutionError: hadExecutionError, emptyDiscovery: emptyDiscovery);
    }
}
