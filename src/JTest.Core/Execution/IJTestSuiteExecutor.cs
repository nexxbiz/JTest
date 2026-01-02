using JTest.Core.Models;

namespace JTest.Core.Execution;

public interface IJTestSuiteExecutor
{
    Task<IEnumerable<JTestSuiteExecutionResult>> Execute(IEnumerable<JTestSuite> testFiles);

    IEnumerable<JTestSuiteExecutionResult> ExecuteParallel(IEnumerable<JTestSuite> testFiles, int parallelCount);
}
