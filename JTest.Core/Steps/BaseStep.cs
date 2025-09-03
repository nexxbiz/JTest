using System.Diagnostics;
using System.Text.Json;
using JTest.Core.Assertions;
using JTest.Core.Debugging;
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
    /// Gets the debug logger instance for this step
    /// </summary>
    protected IDebugLogger? DebugLogger { get; private set; }
    
    /// <summary>
    /// Sets the debug logger for this step
    /// </summary>
    public void SetDebugLogger(IDebugLogger? debugLogger)
    {
        DebugLogger = debugLogger;
    }
    
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
    /// Checks if any assertions failed
    /// </summary>
    protected static bool HasFailedAssertions(List<AssertionResult> assertionResults)
    {
        return assertionResults.Any(r => !r.Success);
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
    
    /// <summary>
    /// Logs debug information for step execution
    /// </summary>
    protected virtual void LogDebugInformation(IExecutionContext context, Dictionary<string, object> contextBefore, Stopwatch stopwatch, bool success, List<AssertionResult>? assertionResults = null)
    {
        if (DebugLogger == null) return;
        
        var stepInfo = CreateStepDebugInfo(context, stopwatch, success);
        var contextChanges = DetectContextChanges(contextBefore, context.Variables);
        
        DebugLogger.LogStepExecution(stepInfo);
        DebugLogger.LogContextChanges(contextChanges);
        if (assertionResults != null && assertionResults.Count > 0)
        {
            DebugLogger.LogAssertionResults(assertionResults);
        }
        DebugLogger.LogRuntimeContext(context.Variables);
    }
    
    /// <summary>
    /// Creates step debug information using context values for test and step numbers
    /// </summary>
    protected virtual StepDebugInfo CreateStepDebugInfo(IExecutionContext context, Stopwatch stopwatch, bool success)
    {
        return new StepDebugInfo
        {
            TestNumber = context.TestNumber,
            StepNumber = context.StepNumber,
            StepType = Type,
            StepId = Id ?? "",
            Enabled = true,
            Result = success ? "Success" : "Failed",
            Duration = stopwatch.Elapsed,
            Description = GetStepDescription()
        };
    }
    
    /// <summary>
    /// Gets the step description for debug output. Override in derived classes for custom descriptions.
    /// </summary>
    protected virtual string GetStepDescription()
    {
        return "";
    }
    
    /// <summary>
    /// Detects context changes between before and after step execution
    /// </summary>
    protected virtual ContextChanges DetectContextChanges(Dictionary<string, object> before, Dictionary<string, object> after)
    {
        var changes = new ContextChanges();
        
        DetectAddedVariables(before, after, changes);
        DetectModifiedVariables(before, after, changes);
        GenerateAvailableExpressions(after, changes);
        
        return changes;
    }

    /// <summary>
    /// Detects variables that were added during step execution
    /// </summary>
    protected virtual void DetectAddedVariables(Dictionary<string, object> before, Dictionary<string, object> after, ContextChanges changes)
    {
        foreach (var kvp in after)
        {
            if (!before.ContainsKey(kvp.Key))
            {
                var description = DescribeValue(kvp.Value);
                changes.Added.Add($"`$.{kvp.Key}` = {description}");
            }
        }
    }

    /// <summary>
    /// Detects variables that were modified during step execution
    /// </summary>
    protected virtual void DetectModifiedVariables(Dictionary<string, object> before, Dictionary<string, object> after, ContextChanges changes)
    {
        foreach (var kvp in after)
        {
            if (before.ContainsKey(kvp.Key) && !ReferenceEquals(before[kvp.Key], kvp.Value))
            {
                var description = DescribeValue(kvp.Value);
                changes.Modified.Add($"`$.{kvp.Key}` = {description}");
            }
        }
    }

    /// <summary>
    /// Generates available JSONPath expressions for the context
    /// </summary>
    protected virtual void GenerateAvailableExpressions(Dictionary<string, object> context, ContextChanges changes)
    {
        foreach (var key in context.Keys)
        {
            changes.Available.Add($"$.{key}");
        }
    }

    /// <summary>
    /// Creates a description of a variable value for debug output
    /// </summary>
    protected virtual string DescribeValue(object value)
    {
        if (value == null) return "null";
        
        return value switch
        {
            string str when str.Length <= 50 => $"\"{str}\"",
            string str => $"\"{str[..47]}...\"",
            bool b => b.ToString().ToLowerInvariant(),
            int or long or double or float or decimal => value.ToString() ?? "null",
            Dictionary<string, object> dict => $"{{object with {dict.Count} properties}}",
            Array array => $"[array with {array.Length} items]",
            System.Collections.IList list => $"[array with {list.Count} items]",
            _ => $"{{object of type {value.GetType().Name}}}"
        };
    }
    
    /// <summary>
    /// Creates a copy of the context variables for change detection
    /// </summary>
    protected virtual Dictionary<string, object> CloneContext(IExecutionContext context)
    {
        return new Dictionary<string, object>(context.Variables);
    }
}