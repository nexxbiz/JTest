using JTest.Core.Models;

namespace JTest.Core.Execution;

public interface IJTestSuiteExecutionResultProcessor
{
    /// <summary>
    /// Writes the human-readable run summary to the console. File artifacts (HTML report and the
    /// canonical trace JSON) are projections of the trace and are written by the run command, not here.
    /// </summary>
    void WriteConsoleSummary(IEnumerable<JTestSuiteExecutionResult> results);
}
