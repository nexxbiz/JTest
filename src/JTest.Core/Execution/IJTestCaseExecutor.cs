using JTest.Core.Models;

namespace JTest.Core.Execution;

public interface IJTestCaseExecutor
{
    Task<IEnumerable<JTestCaseResult>> ExecuteAsync(JTestCase testCase, TestExecutionContext baseContext, int testNumber = 1, CancellationToken cancellationToken = default);
}
