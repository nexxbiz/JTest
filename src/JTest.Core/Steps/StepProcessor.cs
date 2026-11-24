using JTest.Core.Assertions;
using JTest.Core.Debugging;
using JTest.Core.Exceptions;
using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;
using JTest.Core.Utilities;
using Spectre.Console;
using System.Diagnostics;
using System.Text.Json;

namespace JTest.Core.Steps;

public sealed class StepProcessor(IAssertionProcessor assertionProcessor) : IStepProcessor
{
    public static readonly IStepProcessor Default = new StepProcessor(new AssertionProcessor());

    public async Task<StepProcessedResult> ProcessStep(IStep step, IExecutionContext executionContext, CancellationToken cancellationToken = default)
    {
        if (!step.Validate(executionContext, out var validationErrors))
        {
            throw new StepConfigurationValidationException(step.TypeName, validationErrors);
        }

        var contextBefore = CloneContext(executionContext);
        var stopWatch = Stopwatch.StartNew();
        var resultData = await step.ExecuteAsync(executionContext, cancellationToken);
        stopWatch.Stop();

        return await ProcessStepCompletionAsync(
            step,
            executionContext,
            contextBefore,
            stopWatch,
            resultData
        );
    }

    /// <summary>
    /// Processes assertions if present in step configuration
    /// </summary>
    private async Task<IEnumerable<AssertionResult>> ProcessAssertionsAsync(IStepConfiguration? stepConfiguration, IExecutionContext context)
    {
        if (stepConfiguration is null || stepConfiguration.Assert?.Any() != true)
        {
            return [];
        }

        return await assertionProcessor.ProcessAssertionsAsync(stepConfiguration.Assert, context);
    }

    /// <summary>
    /// Stores step result data in execution context. Returns any FR-049 warnings raised by save
    /// sources whose JSONPath matched nothing.
    /// </summary>
    private IReadOnlyList<string> StoreResultInContext(IStep step, IExecutionContext context, object? data)
    {
        context.Variables["this"] = data;

        if (!string.IsNullOrEmpty(step.Configuration.Id))
        {
            context.Variables[step.Configuration.Id] = data;
        }

        return ProcessSaveOperations(step.Configuration, context);
    }

    /// <summary>
    /// Processes save operations from step configuration
    /// </summary>
    private IReadOnlyList<string> ProcessSaveOperations(IStepConfiguration? stepConfiguration, IExecutionContext context)
    {
        if (stepConfiguration is null || !(stepConfiguration.Save?.Count > 0))
        {
            return [];
        }

        var warnings = new List<string>();
        var save = JsonSerializer.SerializeToElement(stepConfiguration.Save);
        foreach (var saveProperty in save.EnumerateObject())
        {
            try
            {
                var resolvedValue = GetSaveValue(saveProperty.Value, context, out var unresolvedPaths);

                foreach (var path in unresolvedPaths)
                {
                    warnings.Add($"save '{saveProperty.Name}': {VariableInterpolator.DescribeUnresolvedPath(path)}");
                }

                // Parse the target path to determine where to save
                var targetPath = saveProperty.Name;
                ApplySaveOperation(context, targetPath, resolvedValue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to process save operation '{saveProperty.Name}': {ex.Message}");
            }
        }

        return warnings;
    }

    /// <summary>
    /// Applies a save operation to the execution context. A target whose last segment ends in
    /// <c>[]</c> (e.g. <c>$.instances[]</c>, <c>$.ctx.list[]</c>) appends the value to the array at
    /// that path, creating it when absent; any other target replaces the value.
    /// </summary>
    private static void ApplySaveOperation(IExecutionContext context, string targetPath, object? value)
    {
        targetPath = $"{VariableInterpolator.ResolveVariableTokens(targetPath, context)}";

        var append = targetPath.EndsWith("[]", StringComparison.Ordinal);
        var path = append ? targetPath[..^2] : targetPath;

        // Handle JSONPath style target paths like $.globals.token, $.case.userId, etc.
        if (!path.StartsWith("$."))
        {
            // Simple variable name
            Assign(context.Variables, path);
            return;
        }

        var pathParts = path[2..].Split('.');
        if (pathParts.Length == 1)
        {
            // Simple variable like $.token
            Assign(context.Variables, pathParts[0]);
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
            Assign(scopeDict, key);
        }
        else
        {
            throw new InvalidOperationException($"Cannot save to '{targetPath}' - '{scope}' is not a dictionary");
        }

        void Assign(IDictionary<string, object?> target, string name)
        {
            target[name] = append
                ? AppendToArray(target.TryGetValue(name, out var existing) ? existing : null, value, path)
                : value;
        }
    }

    /// <summary>
    /// Returns a new array holding the existing items plus <paramref name="value"/>. A new array is
    /// built on every append (rather than mutating in place) so the saved value never aliases data it
    /// was copied from and the step's context-change detection sees the update.
    /// </summary>
    private static object?[] AppendToArray(object? existing, object? value, string path)
    {
        IEnumerable<object?> items = existing switch
        {
            null => [],
            JsonElement { ValueKind: JsonValueKind.Array } element => element.EnumerateArray().Select(e => (object?)e),
            JsonElement { ValueKind: JsonValueKind.Null } => [],
            string or System.Collections.IDictionary => throw NotAnArray(),
            System.Collections.IEnumerable enumerable => enumerable.Cast<object?>(),
            _ => throw NotAnArray()
        };

        return [.. items, value];

        InvalidOperationException NotAnArray() =>
            new($"Cannot append to '{path}' - the existing value is not an array");
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
    private async Task<StepProcessedResult> ProcessStepCompletionAsync(
        IStep step,
        IExecutionContext context,
        Dictionary<string, object?> contextBefore,
        Stopwatch stopwatch,
        StepExecutionResult stepExecutionResult)
    {
        // Store result in context and process save operations
        var saveWarnings = StoreResultInContext(step, context, stepExecutionResult.Data);

        // Detect context changes after save operations
        var contextChanges = DetectContextChanges(contextBefore, context.Variables);

        // Process assertions after storing result data
        var assertionResults = await ProcessAssertionsAsync(step.Configuration, context);

        // Determine if step should be marked as failed based on assertion and inner step results
        var hasFailedAssertions = assertionResults.Any(r => !r.Success);
        var hasFailedInnerSteps = stepExecutionResult.InnerProcessedResults.Any(x => !x.Success);
        
        var errorMessage = hasFailedAssertions
                ? GetFailedAssertionsErrorMessages(assertionResults)
                : string.Empty;
        if(hasFailedInnerSteps)
        {
            errorMessage += GetFailedInnerStepsErrorMessages(stepExecutionResult.InnerProcessedResults);
        }

        // Create result - fail if any assertions failed
        var stepResult = new StepProcessedResult(context.StepNumber)
        {
            Step = step,
            // A timeout or cancellation is never a pass.
            Success = !hasFailedAssertions && !hasFailedInnerSteps
                      && !stepExecutionResult.TimedOut && !stepExecutionResult.Cancelled,
            ErrorMessage = errorMessage,
            DurationMs = stopwatch.ElapsedMilliseconds,
            AssertionResults = assertionResults ?? [],
            Data = stepExecutionResult.Data,
            ContextChanges = contextChanges,
            Warnings = saveWarnings,
            InnerResults = stepExecutionResult.InnerProcessedResults ?? [],
            Iterations = stepExecutionResult.Iterations ?? [],
            TimedOut = stepExecutionResult.TimedOut,
            Cancelled = stepExecutionResult.Cancelled
        };

        return stepResult;
    }

    private static string GetFailedAssertionsErrorMessages(IEnumerable<AssertionResult> assertionResults)
    {
        var errorMessages = assertionResults
            .Where(x => !x.Success)
            .Select(x => x.ErrorMessage);

        var joinedErrorMessages = string.Join(", ", errorMessages);

        return $"Assertions failed: {joinedErrorMessages}";
    }

    private static string GetFailedInnerStepsErrorMessages(IEnumerable<StepProcessedResult> innerSteps)
    {
        var errorMessages = innerSteps
            .Where(x => !x.Success)
            .Select(x => x.ErrorMessage);

        var joinedErrorMessages = string.Join(", ", errorMessages);

        return joinedErrorMessages;
    }


    /// <summary>
    /// Resolves a save value. Strings are interpolated; JSON objects and arrays are resolved eagerly,
    /// every nested string interpolated in the current context, so a structured value saved inside a
    /// loop captures that iteration's values (not tokens re-resolved later against the last
    /// iteration). Every JSONPath that matched nothing is reported in <paramref name="unresolvedPaths"/>.
    /// </summary>
    private static object? GetSaveValue(JsonElement element, IExecutionContext context, out IReadOnlyList<string> unresolvedPaths)
    {
        var collected = new List<string>();
        var value = ResolveSaveValue(element, context, collected);
        unresolvedPaths = collected;
        return value;
    }

    private static object? ResolveSaveValue(JsonElement element, IExecutionContext context, List<string> unresolved)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var resolved = VariableInterpolator.ResolveVariableTokens(element.GetString() ?? "", context, out var paths);
                unresolved.AddRange(paths);
                return resolved;
            case JsonValueKind.Number:
                return element.TryGetInt32(out var intValue) ? intValue : element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Object:
                var dictionary = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    dictionary[property.Name] = ResolveSaveValue(property.Value, context, unresolved);
                }
                return dictionary;
            case JsonValueKind.Array:
                return element.EnumerateArray().Select(item => ResolveSaveValue(item, context, unresolved)).ToArray();
            default:
                return element.GetString() ?? "";
        }
    }
}
