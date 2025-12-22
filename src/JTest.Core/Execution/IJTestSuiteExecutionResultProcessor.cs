using JTest.Core.Models;

namespace JTest.Core.Execution;

public interface IJTestSuiteExecutionResultProcessor
{
    void Process(IEnumerable<JTestSuiteExecutionResult> results, string outputDirectoryPath, bool isDebug, bool skipOutput, string? outputFormat);
}
