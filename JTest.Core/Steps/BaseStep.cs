using System.Text.Json;
using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Utilities;

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
    /// Gets the step configuration JSON element
    /// </summary>
    protected JsonElement Configuration { get; private set; }
    
    /// <summary>
    /// Sets the step configuration
    /// </summary>
    public void SetConfiguration(JsonElement configuration)
    {
        Configuration = configuration;
    }
    
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
    /// Processes assertions if present in step configuration
    /// </summary>
    protected async Task<List<AssertionResult>> ProcessAssertionsAsync(IExecutionContext context)
    {
        if (Configuration.ValueKind == JsonValueKind.Undefined || !Configuration.TryGetProperty("assert", out var assertElement))
        {
            return new List<AssertionResult>();
        }

        var processor = new DefaultAssertionProcessor();
        return await processor.ProcessAssertionsAsync(assertElement, context);
    }
    
    /// <summary>
    /// Stores step result data in execution context
    /// </summary>
    protected virtual void StoreResultInContext(IExecutionContext context, object data)
    {
        context.Variables["this"] = data;
        if (!string.IsNullOrEmpty(Id)) context.Variables[Id] = data;
        
        // Process save operations if present
        ProcessSaveOperations(context);
    }
    
    /// <summary>
    /// Processes save operations from step configuration
    /// </summary>
    protected virtual void ProcessSaveOperations(IExecutionContext context)
    {
        if (Configuration.ValueKind == JsonValueKind.Undefined || !Configuration.TryGetProperty("save", out var saveElement))
        {
            return;
        }

        if (saveElement.ValueKind != JsonValueKind.Object)
        {
            context.Log.Add("Warning: 'save' property must be an object");
            return;
        }

        foreach (var saveProperty in saveElement.EnumerateObject())
        {
            try
            {
                var resolvedValue = VariableInterpolator.ResolveVariableTokens(saveProperty.Value.GetString() ?? "", context);
                
                // Parse the target path to determine where to save
                var targetPath = saveProperty.Name;
                ApplySaveOperation(context, targetPath, resolvedValue);
            }
            catch (Exception ex)
            {
                context.Log.Add($"Warning: Failed to process save operation '{saveProperty.Name}': {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Applies a save operation to the execution context
    /// </summary>
    protected virtual void ApplySaveOperation(IExecutionContext context, string targetPath, object value)
    {
        // Handle JSONPath style target paths like $.globals.token, $.case.userId, etc.
        if (targetPath.StartsWith("$."))
        {
            var pathParts = targetPath.Substring(2).Split('.');
            if (pathParts.Length >= 2)
            {
                var scope = pathParts[0];
                var key = pathParts[1];
                
                // Ensure the scope exists as a dictionary
                if (!context.Variables.ContainsKey(scope))
                {
                    context.Variables[scope] = new Dictionary<string, object>();
                }
                
                if (context.Variables[scope] is Dictionary<string, object> scopeDict)
                {
                    scopeDict[key] = value;
                }
                else
                {
                    context.Log.Add($"Warning: Cannot save to '{targetPath}' - '{scope}' is not a dictionary");
                }
            }
            else if (pathParts.Length == 1)
            {
                // Simple variable like $.token
                context.Variables[pathParts[0]] = value;
            }
        }
        else
        {
            // Simple variable name
            context.Variables[targetPath] = value;
        }
    }
}