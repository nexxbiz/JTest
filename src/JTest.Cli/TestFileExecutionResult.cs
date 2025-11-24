using JTest.Core.Models;

namespace JTest.Cli
{
    public sealed class TestFileExecutionResult(string filePath, string? testSuiteName, string? testSuiteDescription, IEnumerable<JTestCaseResult> testCaseResults)
    {
        public string FilePath { get; } = filePath;
        public string? TestSuiteName { get; } = testSuiteName;
        public string? TestSuiteDescription { get; } = testSuiteDescription;
        public IEnumerable<JTestCaseResult> TestCaseResults { get; } = testCaseResults;

        public int CasesPassed => TestCaseResults.Count(r => r.Success);

        public int CasesFailed => TestCaseResults.Count(r => !r.Success);
    }
}
