using JTest.Core.Execution;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// Interface for step implementations in the JTest execution engine
/// </summary>
public interface IStep
{
    /// <summary>
    /// Gets the step type identifier
    /// </summary>
    string Type { get; }

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
    /// Executes the step with the provided context
    /// </summary>
    Task<StepResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the step configuration from JSON
    /// </summary>
    bool ValidateConfiguration(List<string> validationErrors);
}