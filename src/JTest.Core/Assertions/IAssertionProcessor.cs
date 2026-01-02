using JTest.Core.Execution;

namespace JTest.Core.Assertions;

/// <summary>
/// Interface for assertion processing
/// </summary>
public interface IAssertionProcessor
{
    Task<IEnumerable<AssertionResult>> ProcessAssertionsAsync(IEnumerable<IAssertionOperation> assertOperations, IExecutionContext context);
}
