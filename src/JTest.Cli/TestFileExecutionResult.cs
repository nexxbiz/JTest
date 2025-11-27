using JTest.Core.Models;

namespace JTest.Cli;

public sealed record TestFileExecutionResult(string FilePath, string? TestSuiteName, string? TestSuiteDescription, IEnumerable<JTestCaseResult> TestCaseResults)
{
    public int CasesPassed => TestCaseResults.Count(r => r.Success);

    public int CasesFailed => TestCaseResults.Count(r => !r.Success);
}
