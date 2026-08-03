namespace JTest.Core.Models;

public sealed record JTestSuiteExecutionResult(string FilePath, string? TestSuiteName, string? TestSuiteDescription, IEnumerable<JTestCaseResult> TestCaseResults)
{
    public int CasesPassed => TestCaseResults.Count(r => r.Success);

    public int CasesFailed => TestCaseResults.Count(r => !r.Success);

    /// <summary>
    /// Set when the suite failed to load/execute (crash, bad template, deserialization). The suite
    /// is captured as errored rather than dropped, so it drives a non-zero exit (FR-002).
    /// </summary>
    public string? ExecutionError { get; init; }

    public bool Errored => ExecutionError is not null;

    /// <summary>A suite succeeds only if it did not error and every case passed.</summary>
    public bool Success => !Errored && CasesFailed == 0;
}
