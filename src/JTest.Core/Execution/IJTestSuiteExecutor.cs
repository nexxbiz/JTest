using JTest.Core.Models;

namespace JTest.Core.Execution;

public interface IJTestSuiteExecutor
{
    Task<IEnumerable<JTestSuiteExecutionResult>> Execute(IEnumerable<JTestSuite> testFiles, CancellationToken cancellationToken = default);

    IEnumerable<JTestSuiteExecutionResult> ExecuteParallel(IEnumerable<JTestSuite> testFiles, int parallelCount, CancellationToken cancellationToken = default);
}
