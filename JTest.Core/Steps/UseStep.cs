using System.Diagnostics;
using System.Text.Json;
using JTest.Core.Debugging;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Utilities;

namespace JTest.Core.Steps;

/// <summary>
/// Step that executes a template with provided parameters in an isolated context
/// </summary>
public class UseStep : BaseStep
{
    private readonly ITemplateProvider _templateProvider;
    private readonly StepFactory _stepFactory;
    private readonly IDebugLogger? _debugLogger;

    public UseStep(ITemplateProvider templateProvider, StepFactory stepFactory, IDebugLogger? debugLogger = null)
    {
        _templateProvider = templateProvider;
        _stepFactory = stepFactory;
        _debugLogger = debugLogger;
    }

    public override string Type => "use";

    public override bool ValidateConfiguration(JsonElement configuration)
    {
        SetConfiguration(configuration);
        
        // Must have template property
        if (!configuration.TryGetProperty("template", out _))
        {
            return false;
        }

        return true;
    }

    public override async Task<StepResult> ExecuteAsync(IExecutionContext context)
    {
        var contextBefore = CloneContext(context);
        var stopwatch = Stopwatch.StartNew();
        TemplateExecutionInfo? templateInfo = null;
        
        try
        {
            var (result, templateExecInfo) = await ExecuteTemplateAsync(context, stopwatch);
            templateInfo = templateExecInfo;
            stopwatch.Stop();
            
            // Store result in context and process save operations (consistent with other steps)
            StoreResultInContext(context, result);
            
            // Capture saved variables after save operations are processed
            CaptureSavedVariables(context, contextBefore, templateInfo);
            
            // Process assertions after storing result data (consistent with other steps)
            var assertionResults = await ProcessAssertionsAsync(context);
            
            LogDebugInformation(context, contextBefore, stopwatch, true, templateInfo);
            var stepResult = StepResult.CreateSuccess(result, stopwatch.ElapsedMilliseconds);
            stepResult.AssertionResults = assertionResults;
            return stepResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            context.Log.Add($"Template execution failed: {ex.Message}");
            LogDebugInformation(context, contextBefore, stopwatch, false, templateInfo);
            return StepResult.CreateFailure(ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<(object result, TemplateExecutionInfo templateInfo)> ExecuteTemplateAsync(IExecutionContext context, Stopwatch stopwatch)
    {
        // Get template name
        var templateName = Configuration.GetProperty("template").GetString()
            ?? throw new InvalidOperationException("Template name is required");

        // Get template definition
        var template = _templateProvider.GetTemplate(templateName)
            ?? throw new InvalidOperationException($"Template '{templateName}' not found");

        // Create isolated execution context for template
        var templateContext = CreateIsolatedTemplateContext(context, template);

        // Capture input parameters for debugging
        var inputParameters = new Dictionary<string, object>();
        if (Configuration.TryGetProperty("with", out var withElement) && withElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var param in withElement.EnumerateObject())
            {
                var resolvedValue = ResolveParameterValue(param.Value, context);
                inputParameters[param.Name] = resolvedValue;
            }
        }

        // Execute template steps
        var templateResults = new List<object>();
        
        foreach (var stepConfig in template.Steps)
        {
            var step = _stepFactory.CreateStep(stepConfig);
            var stepResult = await step.ExecuteAsync(templateContext);
            
            if (!stepResult.Success)
            {
                throw new InvalidOperationException($"Template step failed: {stepResult.ErrorMessage}");
            }
            
            templateResults.Add(stepResult.Data ?? new object());
        }

        // Map template outputs to parent context
        var outputs = MapTemplateOutputs(template, templateContext);
        
        // Return step result data that will be stored by StoreResultInContext()
        var resultData = new Dictionary<string, object>(outputs);
        resultData["type"] = "template";
        resultData["templateName"] = templateName;
        resultData["steps"] = templateResults.Count;

        // Create template execution info for debugging (savedVariables will be filled later)
        var templateInfo = new TemplateExecutionInfo
        {
            TemplateName = templateName,
            InputParameters = inputParameters,
            StepsExecuted = templateResults.Count,
            OutputValues = outputs,
            SavedVariables = new Dictionary<string, object>() // Will be populated after save operations
        };
        
        return (resultData, templateInfo);
    }

    private TestExecutionContext CreateIsolatedTemplateContext(IExecutionContext parentContext, Template template)
    {
        var templateContext = new TestExecutionContext();
        
        // Add template parameters from 'with' configuration
        if (Configuration.TryGetProperty("with", out var withElement) && withElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var param in withElement.EnumerateObject())
            {
                var resolvedValue = ResolveParameterValue(param.Value, parentContext);
                templateContext.Variables[param.Name] = resolvedValue;
            }
        }

        // Validate required parameters
        ValidateRequiredParameters(template, templateContext);

        // Add default values for missing optional parameters
        AddDefaultParameterValues(template, templateContext);

        // Initialize template's internal context
        templateContext.Variables["ctx"] = new Dictionary<string, object>();
        
        return templateContext;
    }

    private object ResolveParameterValue(JsonElement paramValue, IExecutionContext parentContext)
    {
        if (paramValue.ValueKind == JsonValueKind.String)
        {
            var stringValue = paramValue.GetString() ?? "";
            return VariableInterpolator.ResolveVariableTokens(stringValue, parentContext);
        }

        return GetJsonElementValue(paramValue);
    }

    private void ValidateRequiredParameters(Template template, TestExecutionContext templateContext)
    {
        if (template.Params == null) return;

        foreach (var param in template.Params)
        {
            if (param.Value.Required && !templateContext.Variables.ContainsKey(param.Key))
            {
                throw new InvalidOperationException($"Required template parameter '{param.Key}' not provided");
            }
        }
    }

    private void AddDefaultParameterValues(Template template, TestExecutionContext templateContext)
    {
        if (template.Params == null) return;

        foreach (var param in template.Params)
        {
            if (!templateContext.Variables.ContainsKey(param.Key) && param.Value.Default != null)
            {
                templateContext.Variables[param.Key] = param.Value.Default;
            }
        }
    }

    private Dictionary<string, object> MapTemplateOutputs(Template template, TestExecutionContext templateContext)
    {
        var outputs = new Dictionary<string, object>();

        if (template.Output == null) return outputs;

        foreach (var outputMapping in template.Output)
        {
            var resolvedValue = ResolveOutputValue(outputMapping.Value, templateContext);
            outputs[outputMapping.Key] = resolvedValue;
        }

        return outputs;
    }

    private object ResolveOutputValue(object outputValue, TestExecutionContext templateContext)
    {
        return outputValue switch
        {
            string stringValue => VariableInterpolator.ResolveVariableTokens(stringValue, templateContext),
            JsonElement jsonElement => ResolveJsonElementValue(jsonElement, templateContext),
            _ => outputValue
        };
    }

    private object ResolveJsonElementValue(JsonElement element, TestExecutionContext templateContext)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => VariableInterpolator.ResolveVariableTokens(element.GetString() ?? "", templateContext),
            JsonValueKind.Array => element.EnumerateArray().Select(e => ResolveJsonElementValue(e, templateContext)).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ResolveJsonElementValue(p.Value, templateContext)),
            _ => GetJsonElementValue(element)
        };
    }

    private object GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Array => element.EnumerateArray().Select(GetJsonElementValue).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => GetJsonElementValue(p.Value)),
            _ => element.GetRawText()
        };
    }

    private Dictionary<string, object> CloneContext(IExecutionContext context)
    {
        return new Dictionary<string, object>(context.Variables);
    }

    private void CaptureSavedVariables(IExecutionContext context, Dictionary<string, object> contextBefore, TemplateExecutionInfo templateInfo)
    {
        if (Configuration.TryGetProperty("save", out var saveElement) && saveElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var saveProperty in saveElement.EnumerateObject())
            {
                var targetPath = saveProperty.Name;
                
                // Try to get the actual saved value from the context
                if (TryGetValueFromPath(context.Variables, targetPath, out var savedValue))
                {
                    templateInfo.SavedVariables[targetPath] = savedValue ?? "";
                }
            }
        }
    }

    private bool TryGetValueFromPath(Dictionary<string, object> context, string path, out object? value)
    {
        value = null;
        
        if (path.StartsWith("$."))
        {
            var pathParts = path.Substring(2).Split('.');
            if (pathParts.Length >= 2)
            {
                var scope = pathParts[0];
                var key = pathParts[1];
                
                if (context.ContainsKey(scope) && context[scope] is Dictionary<string, object> scopeDict)
                {
                    if (scopeDict.ContainsKey(key))
                    {
                        value = scopeDict[key];
                        return true;
                    }
                }
            }
            else if (pathParts.Length == 1)
            {
                if (context.ContainsKey(pathParts[0]))
                {
                    value = context[pathParts[0]];
                    return true;
                }
            }
        }
        else if (context.ContainsKey(path))
        {
            value = context[path];
            return true;
        }
        
        return false;
    }

    private void LogDebugInformation(IExecutionContext context, Dictionary<string, object> contextBefore, Stopwatch stopwatch, bool success, TemplateExecutionInfo? templateInfo = null)
    {
        if (_debugLogger == null) return;
        
        var stepInfo = CreateStepDebugInfo(stopwatch, success, templateInfo);
        var contextChanges = DetectContextChanges(contextBefore, context.Variables);
        
        _debugLogger.LogStepExecution(stepInfo);
        _debugLogger.LogContextChanges(contextChanges);
        _debugLogger.LogRuntimeContext(context.Variables);
    }

    private StepDebugInfo CreateStepDebugInfo(Stopwatch stopwatch, bool success, TemplateExecutionInfo? templateInfo = null)
    {
        var templateName = Configuration.TryGetProperty("template", out var templateElement) 
            ? templateElement.GetString() ?? "unknown" 
            : "unknown";

        return new StepDebugInfo
        {
            TestNumber = 1, // TODO: Get from context
            StepNumber = 1, // TODO: Get from context  
            StepType = "UseStep",
            StepId = Id ?? "",
            Enabled = true,
            Result = success ? "Success" : "Failed",
            Duration = stopwatch.Elapsed,
            Description = $"Execute template '{templateName}'",
            TemplateExecution = templateInfo
        };
    }

    private ContextChanges DetectContextChanges(Dictionary<string, object> before, Dictionary<string, object> after)
    {
        var changes = new ContextChanges();
        
        DetectAddedVariables(before, after, changes);
        DetectModifiedVariables(before, after, changes);
        GenerateAvailableExpressions(after, changes);
        
        return changes;
    }

    private void DetectAddedVariables(Dictionary<string, object> before, Dictionary<string, object> after, ContextChanges changes)
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

    private void DetectModifiedVariables(Dictionary<string, object> before, Dictionary<string, object> after, ContextChanges changes)
    {
        foreach (var kvp in after)
        {
            if (before.ContainsKey(kvp.Key) && !object.Equals(before[kvp.Key], kvp.Value))
            {
                var beforeDesc = DescribeValue(before[kvp.Key]);
                var afterDesc = DescribeValue(kvp.Value);
                changes.Modified.Add($"`$.{kvp.Key}`: {beforeDesc} → {afterDesc}");
            }
        }
    }

    private void GenerateAvailableExpressions(Dictionary<string, object> context, ContextChanges changes)
    {
        foreach (var key in context.Keys)
        {
            changes.Available.Add($"$.{key}");
        }
    }

    private string DescribeValue(object value)
    {
        if (value == null) return "null";
        if (value is string str) return $"\"{str}\"";
        
        if (value is IDictionary<string, object> dict)
            return $"{{object with {dict.Count} properties}}";
        
        return value.ToString() ?? "unknown";
    }
}