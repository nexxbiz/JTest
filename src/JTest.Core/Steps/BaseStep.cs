using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;
using JTest.Core.Utilities;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// Base class for step implementations providing common functionality
/// </summary>
public abstract class BaseStep<TConfiguration>(TConfiguration configuration) : IStep
    where TConfiguration : StepConfiguration
{
    /// <summary>
    /// Gets the step type identifier
    /// </summary>
    public string TypeName => GetType().Name
        .Replace("Step", string.Empty)
        .ToLowerInvariant();

    /// <summary>
    /// Gets or sets the step ID for context storage. Value derived from configuration
    /// </summary>
    public string? Id => Configuration.Id;

    /// <summary>
    /// Gets the step configuration JSON element
    /// </summary>
    protected TConfiguration Configuration { get; } = configuration;

    /// <summary>
    /// Name of the step. Value derived from configuration
    /// </summary>
    public string? Name => Configuration?.Name ?? string.Empty;

    /// <summary>
    /// Step description; can be assigned by derived classes. Initial value derived from configuration
    /// </summary>
    public string? Description { get; protected set; } = configuration?.Description ?? string.Empty;

    StepConfiguration? IStep.Configuration => Configuration;

    /// <summary>
    /// Executes the step with the provided context. Returns output data of the step; or null if the step does not return output
    /// </summary>
    public abstract Task<object?> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Validates the configuration of this step
    /// </summary>
    /// <param name="validationErrors"></param>
    /// <returns></returns>
    public bool ValidateConfiguration(IServiceProvider serviceProvider, IExecutionContext context, List<string> validationErrors)
    {
        Configuration.ValidateConfiguration(serviceProvider, context, validationErrors);
        return validationErrors.Count == 0;
    }

    protected static string ResolveStringValue(string value, IExecutionContext context)
    {
        return VariableInterpolator.ResolveVariableTokens(value, context).ToString() ?? string.Empty;
    }

    protected static JsonElement SerializeToJsonElement(object? value)
    {
        if (value is JsonElement jsonElement)
            return jsonElement;

        return JsonSerializer.SerializeToElement(value, JsonSerializerOptionsCache.Default);
    }
}