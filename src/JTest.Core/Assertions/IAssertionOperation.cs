using JTest.Core.Execution;

namespace JTest.Core.Assertions;

/// <summary>
/// Base interface for assertion operations
/// </summary>
public interface IAssertionOperation
{
    object? ActualValue { get; }

    object? ExpectedValue { get; }

    string OperationName { get; }

    string? Description { get; }

    bool? Mask { get; }

    AssertionResult Execute(IExecutionContext context);
}