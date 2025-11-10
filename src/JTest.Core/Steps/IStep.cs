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
    string? Id { get; set; }

    /// <summary>
    /// Executes the step with the provided context
    /// </summary>
    Task<StepResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the step configuration from JSON
    /// </summary>
    bool ValidateConfiguration(JsonElement configuration);

    /// <summary>
    /// returns a string description for logging purposes
    /// </summary>
    /// <returns></returns>
    string GetStepDescription();

}