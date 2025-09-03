using System.Diagnostics;
using System.Text.Json;
using JTest.Core.Assertions;
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

    public UseStep(ITemplateProvider templateProvider, StepFactory stepFactory, IDebugLogger? debugLogger = null)
    {
        _templateProvider = templateProvider;
        _stepFactory = stepFactory;
        // Set debug logger using base class method
        SetDebugLogger(debugLogger);
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
            
            // Use common step completion logic from BaseStep
            return await ProcessStepCompletionAsync(context, contextBefore, stopwatch, result, templateInfo);
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

        // Create a template-specific debug logger that captures step details without outputting separate headers
        var templateStepDebugLogger = new TemplateStepDebugLogger();
        
        // Create a template step factory that uses the existing factory but with the template debug logger
        // This ensures that the steps created (like HttpStep) are properly mocked when testing
        var templateStepFactory = CreateTemplateStepFactory(templateStepDebugLogger);

        // Execute template steps
        var templateResults = new List<object>();
        
        foreach (var stepConfig in template.Steps)
        {
            var step = templateStepFactory.CreateStep(stepConfig);
            var stepResult = await step.ExecuteAsync(templateContext);
            
            if (!stepResult.Success)
            {
                throw new InvalidOperationException($"Template step failed: {stepResult.ErrorMessage} - {stepResult.DetailedAssertionFailures}");
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
            SavedVariables = new Dictionary<string, object>(), // Will be populated after save operations
            StepExecutionDetails = templateStepDebugLogger.GetCapturedSteps()
        };
        
        return (resultData, templateInfo);
    }

    /// <summary>
    /// Creates a step factory for template execution with the specified debug logger
    /// This method can be overridden by subclasses to customize step creation (e.g., for testing)
    /// </summary>
    protected virtual StepFactory CreateTemplateStepFactory(IDebugLogger templateDebugLogger)
    {
        var templateStepFactory = new StepFactory(_templateProvider);
        templateStepFactory.SetDebugLogger(templateDebugLogger);
        return templateStepFactory;
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
        if (DebugLogger == null) return;
        
        var stepInfo = CreateStepDebugInfo(context, stopwatch, success, templateInfo);
        var contextChanges = DetectContextChanges(contextBefore, context.Variables);
        
        DebugLogger.LogStepExecution(stepInfo);
        DebugLogger.LogContextChanges(contextChanges);
        DebugLogger.LogRuntimeContext(context.Variables);
    }

    private StepDebugInfo CreateStepDebugInfo(IExecutionContext context, Stopwatch stopwatch, bool success, TemplateExecutionInfo? templateInfo = null)
    {
        var templateName = Configuration.TryGetProperty("template", out var templateElement) 
            ? templateElement.GetString() ?? "unknown" 
            : "unknown";

        return new StepDebugInfo
        {
            TestNumber = context.TestNumber,
            StepNumber = context.StepNumber,
            StepType = Type,
            StepId = Id ?? "",
            Enabled = true,
            Result = success ? "Success" : "Failed",
            Duration = stopwatch.Elapsed,
            Description = $"Execute template '{templateName}'",
            TemplateExecution = templateInfo
        };
    }

    protected override string GetStepDescription()
    {
        var templateName = Configuration.TryGetProperty("template", out var templateElement) 
            ? templateElement.GetString() ?? "unknown" 
            : "unknown";
        return $"Execute template '{templateName}'";
    }

    /// <summary>
    /// Override to handle TemplateExecutionInfo in debug logging
    /// </summary>
    protected override void LogDebugInformationWithAdditionalInfo(
        IExecutionContext context, 
        Dictionary<string, object> contextBefore, 
        Stopwatch stopwatch, 
        bool success, 
        List<AssertionResult> assertionResults, 
        object additionalDebugInfo)
    {
        if (additionalDebugInfo is TemplateExecutionInfo templateInfo)
        {
            LogDebugInformation(context, contextBefore, stopwatch, success, templateInfo);
        }
        else
        {
            base.LogDebugInformationWithAdditionalInfo(context, contextBefore, stopwatch, success, assertionResults, additionalDebugInfo);
        }
    }
}