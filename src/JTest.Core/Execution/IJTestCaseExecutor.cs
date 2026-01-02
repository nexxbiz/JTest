using JTest.Core.Models;

namespace JTest.Core.Execution;

public interface IJTestCaseExecutor
{
    Task<IEnumerable<JTestCaseResult>> ExecuteAsync(JTestCase testCase, TestExecutionContext baseContext, int testNumber, CancellationToken cancellationToken = default);
}
