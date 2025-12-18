namespace JTest.Core.Models;

public sealed record TestFileExecutionResult(string FilePath, string? TestSuiteName, string? TestSuiteDescription, IEnumerable<JTestCaseResult> TestCaseResults)
{
    public int CasesPassed => TestCaseResults.Count(r => r.Success);

    public int CasesFailed => TestCaseResults.Count(r => !r.Success);
}
