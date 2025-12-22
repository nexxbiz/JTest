using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;
using JTest.Core.Utilities;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// Base class for step implementations providing common functionality
/// </summary>
public abstract class BaseStep<TConfiguration>(TConfiguration configuration) : IStep
    where TConfiguration : StepConfigurationBase
{
    /// <summary>
    /// Gets the step type identifier
    /// </summary>
    public string TypeName => GetType().Name
        .Replace("Step", string.Empty)
        .ToLowerInvariant();

    /// <summary>
    /// Gets the step configuration JSON element
    /// </summary>
    protected TConfiguration Configuration { get; } = configuration;

    /// <summary>
    /// Step description; can be assigned by derived classes. Initial value derived from configuration
    /// </summary>
    protected string? Description 
    {
        get => Configuration.GetDescription();
        set => Configuration.UpdateDescription(value);
    }

    IStepConfiguration IStep.Configuration => Configuration;

    /// <summary>
    /// Executes the step with the provided context. Returns output data of the step; or null if the step does not return output
    /// </summary>
    public abstract Task<StepExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken);

    public bool Validate(IExecutionContext context, out IEnumerable<string> validationErrors)
    {
        var validationErrorsList = new List<string>();
        Validate(context, validationErrorsList);
        validationErrors = validationErrorsList;

        return !validationErrors.Any();
    }

    protected virtual void Validate(IExecutionContext context, IList<string> validationErrors) { }

    protected static string ResolveStringVariable(string? value, IExecutionContext context)
        => ResolveVariable(value, context)?.ToString() ?? string.Empty;

    protected static object? ResolveVariable(string? value, IExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return VariableInterpolator.ResolveVariableTokens(value, context);
    }

    protected static JsonElement SerializeToJsonElement(object? value)
    {
        if (value is JsonElement jsonElement)
            return jsonElement;

        return JsonSerializer.SerializeToElement(value, JsonSerializerOptionsAccessor.Default);
    }
}