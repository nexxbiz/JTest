using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;

namespace JTest.Core.Steps;

/// <summary>
/// Interface for step implementations in the JTest execution engine
/// </summary>
public interface IStep
{
    /// <summary>
    /// Gets the step type name
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// The input this step needs to perform its execution
    /// </summary>
    StepConfiguration Configuration { get; }

    /// <summary>
    /// Executes the step with the provided context
    /// </summary>
    Task<object?> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default);
}