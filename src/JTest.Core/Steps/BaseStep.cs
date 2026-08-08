using JTest.Core.Exceptions;
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
    {
        return ResolveVariable(value, context)?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Resolves tokens in a step field, failing if any path matched nothing (FR-061). Coercing an
    /// unresolved token to an empty string here silently changes what the step does — a URL, route
    /// name, or body value built from a missing value still gets sent, and the run can pass against
    /// the wrong resource.
    /// </summary>
    protected static object? ResolveVariable(string? value, IExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var resolved = VariableInterpolator.ResolveVariableTokens(value, context, out var unresolvedPaths);
        if (unresolvedPaths.Count > 0)
        {
            throw new UnresolvedTokenException(value, unresolvedPaths);
        }

        return resolved;
    }

    protected static JsonElement SerializeToJsonElement(object? value)
    {
        if (value is JsonElement jsonElement)
            return jsonElement;

        return JsonSerializer.SerializeToElement(value, JsonSerializerOptionsAccessor.Default);
    }
}