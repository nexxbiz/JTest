using JTest.Core.Assertions;
using JTest.Core.Debugging;
using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;
using JTest.Core.Utilities;
using System.Diagnostics;
using System.Text.Json;

namespace JTest.Core.Steps;

public sealed class StepProcessor(IAssertionProcessor assertionProcessor) : IStepProcessor
{
    public async Task<StepResult> ProcessStep(IStep step, StepConfiguration? stepConfiguration, IExecutionContext executionContext, CancellationToken cancellationToken)
    {
        var contextBefore = CloneContext(executionContext);

        var stopWatch = Stopwatch.StartNew();
        var resultData = await step.ExecuteAsync(executionContext, cancellationToken);
        stopWatch.Stop();

        return await ProcessStepCompletionAsync(
            step,
            stepConfiguration,
            executionContext,
            contextBefore,
            stopWatch,
            resultData
        );
    }

    /// <summary>
    /// Processes assertions if present in step configuration
    /// </summary>
    private async Task<IEnumerable<AssertionResult>> ProcessAssertionsAsync(StepConfiguration? stepConfiguration, IExecutionContext context)
    {
        if (stepConfiguration is null || !stepConfiguration.Assert.Any())
        {
            return [];
        }

        return await assertionProcessor.ProcessAssertionsAsync(stepConfiguration.Assert, context);
    }

    /// <summary>
    /// Stores step result data in execution context
    /// </summary>
    private void StoreResultInContext(IStep step, StepConfiguration? stepConfiguration, IExecutionContext context, object? data)
    {
        context.Variables["this"] = data;

        if (!string.IsNullOrEmpty(step.Id))
        {
            context.Variables[step.Id] = data;
        }

        ProcessSaveOperations(stepConfiguration, context);
    }

    /// <summary>
    /// Processes save operations from step configuration
    /// </summary>
    private void ProcessSaveOperations(StepConfiguration? stepConfiguration, IExecutionContext context)
    {
        if (stepConfiguration is null || stepConfiguration.Save.Count == 0)
        {
            return;
        }

        var save = JsonSerializer.SerializeToElement(stepConfiguration.Save);
        foreach (var saveProperty in save.EnumerateObject())
        {
            try
            {
                var resolvedValue = GetSaveValue(saveProperty.Value, context);

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
    private static void ApplySaveOperation(IExecutionContext context, string targetPath, object? value)
    {
        // Handle JSONPath style target paths like $.globals.token, $.case.userId, etc.
        if (!targetPath.StartsWith("$."))
        {
            // Simple variable name
            context.Variables[targetPath] = value;
            return;
        }

        var pathParts = targetPath[2..].Split('.');
        if (pathParts.Length == 1)
        {
            // Simple variable like $.token
            context.Variables[pathParts[0]] = value;
            return;
        }

        var scope = pathParts[0];
        var key = pathParts[1];

        // Ensure the scope exists as a dictionary
        if (!context.Variables.TryGetValue(scope, out object? scopeValue))
        {
            scopeValue = new Dictionary<string, object?>();
            context.Variables[scope] = scopeValue;
        }

        if (scopeValue is Dictionary<string, object?> scopeDict)
        {
            scopeDict[key] = value;
        }
        else
        {
            context.Log.Add($"Warning: Cannot save to '{targetPath}' - '{scope}' is not a dictionary");
        }
    }

    /// <summary>
    /// Detects context changes between before and after step execution
    /// </summary>
    private static ContextChanges DetectContextChanges(Dictionary<string, object?> before, Dictionary<string, object?> after)
    {
        var changes = new ContextChanges();

        DetectAddedVariables(before, after, changes);
        DetectModifiedVariables(before, after, changes);
        return changes;
    }

    /// <summary>
    /// Detects variables that were added during step execution
    /// </summary>
    private static void DetectAddedVariables(Dictionary<string, object?> before, Dictionary<string, object?> after, ContextChanges changes)
    {
        foreach (var kvp in after)
        {
            if (!before.ContainsKey(kvp.Key))
            {

                changes.Added.Add(kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// Detects variables that were modified during step execution
    /// </summary>
    private static void DetectModifiedVariables(Dictionary<string, object?> before, Dictionary<string, object?> after, ContextChanges changes)
    {
        foreach (var kvp in after)
        {
            if (before.TryGetValue(kvp.Key, out object? value) && !ReferenceEquals(value, kvp.Value))
            {
                changes.Modified.Add(kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// Creates a copy of the context variables for change detection
    /// </summary>
    private static Dictionary<string, object?> CloneContext(IExecutionContext context)
    {
        return new Dictionary<string, object?>(context.Variables);
    }

    /// <summary>
    /// Processes the common step completion logic including storing results, processing assertions, 
    /// logging debug info, and creating the final step result
    /// </summary>
    private async Task<StepResult> ProcessStepCompletionAsync(
        IStep step,
        StepConfiguration? stepConfiguration,
        IExecutionContext context,
        Dictionary<string, object?> contextBefore,
        Stopwatch stopwatch,
        object? resultData)
    {
        // Store result in context and process save operations
        StoreResultInContext(step, stepConfiguration, context, resultData);

        // Detect context changes after save operations
        var contextChanges = DetectContextChanges(contextBefore, context.Variables);

        // Process assertions after storing result data
        var assertionResults = await ProcessAssertionsAsync(stepConfiguration, context);

        // Determine if step should be marked as failed based on assertion results
        var hasFailedAssertions = assertionResults.Any(r => !r.Success);

        // If result data is StepResult, then we can immediately return it
        if (resultData is StepResult stepResult)
        {
            if (stepResult.Success && hasFailedAssertions)
            {
                throw new InvalidProgramException($"Step '{step.Type}' returns a StepResult directly, but the success outcome contradicts the processed assertion results.");
            }

            return stepResult;
        }

        // Create result - fail if any assertions failed
        stepResult = new StepResult(context.StepNumber)
        {
            Step = step,
            Success = !hasFailedAssertions,
            ErrorMessage = hasFailedAssertions ? "One or more assertions failed" : null,
            DurationMs = stopwatch.ElapsedMilliseconds,
            AssertionResults = assertionResults ?? [],
            Data = resultData,
            ContextChanges = contextChanges,
            InnerResults = resultData as IEnumerable<StepResult> ?? []
        };

        return stepResult;
    }

    /// <summary>
    /// Gets the save value from JSON element, handling different value types
    /// </summary>
    private object? GetSaveValue(JsonElement element, IExecutionContext context)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => VariableInterpolator.ResolveVariableTokens(element.GetString() ?? "", context),
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => JsonElementToDictionary(element),
            JsonValueKind.Array => JsonElementToArray(element),
            _ => element.GetString() ?? ""
        };
    }

    /// <summary>
    /// Converts JSON object to dictionary
    /// </summary>
    private Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
    {
        var dictionary = new Dictionary<string, object?>();
        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = GetValueFromJsonElement(property.Value);
        }
        return dictionary;
    }

    /// <summary>
    /// Converts JSON array to object array
    /// </summary>
    private object?[] JsonElementToArray(JsonElement element)
    {
        var list = new List<object?>();
        foreach (var item in element.EnumerateArray())
        {
            list.Add(GetValueFromJsonElement(item));
        }
        return list.ToArray();
    }

    /// <summary>
    /// Gets value from JSON element without variable interpolation
    /// </summary>
    private object? GetValueFromJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => JsonElementToDictionary(element),
            JsonValueKind.Array => JsonElementToArray(element),
            _ => element.GetString() ?? ""
        };
    }
}
