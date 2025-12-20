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
    /// Gets or sets the step ID for context storage
    /// </summary>
    string? Id { get; }

    /// <summary>
    /// Gets or stes the name of the step. This is for display purposes only
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets or stes the description of the step. This is for display purposes only
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// The input this step needs to perform its execution
    /// </summary>
    StepConfiguration? Configuration { get; }

    /// <summary>
    /// Executes the step with the provided context
    /// </summary>
    Task<object?> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the step configuration
    /// </summary>
    bool ValidateConfiguration(IServiceProvider serviceProvider, IExecutionContext context, List<string> validationErrors);
}