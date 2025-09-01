using System.Text.Json;
using JTest.Core.Execution;

namespace JTest.Core.Steps;

/// <summary>
/// Base class for step implementations providing common functionality
/// </summary>
public abstract class BaseStep : IStep
{
    /// <summary>
    /// Gets the step type identifier
    /// </summary>
    public abstract string Type { get; }
    
    /// <summary>
    /// Gets or sets the step ID for context storage
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Executes the step with the provided context
    /// </summary>
    public abstract Task<StepResult> ExecuteAsync(IExecutionContext context);
    
    /// <summary>
    /// Validates the step configuration from JSON
    /// </summary>
    public virtual bool ValidateConfiguration(JsonElement configuration)
    {
        return true;
    }
    
    /// <summary>
    /// Stores step result data in execution context
    /// </summary>
    protected virtual void StoreResultInContext(IExecutionContext context, object data)
    {
        context.Variables["this"] = data;
        if (!string.IsNullOrEmpty(Id)) context.Variables[Id] = data;
    }
}