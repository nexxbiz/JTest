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
        
        try
        {
            var result = await ExecuteTemplateAsync(context, stopwatch);
            stopwatch.Stop();
            
            LogDebugInformation(context, contextBefore, stopwatch, true);
            return StepResult.CreateSuccess(result, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            context.Log.Add($"Template execution failed: {ex.Message}");
            LogDebugInformation(context, contextBefore, stopwatch, false);
            return StepResult.CreateFailure(ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<object> ExecuteTemplateAsync(IExecutionContext context, Stopwatch stopwatch)
    {
        // Get template name
        var templateName = Configuration.GetProperty("template").GetString()
            ?? throw new InvalidOperationException("Template name is required");

        // Get template definition
        var template = _templateProvider.GetTemplate(templateName)
            ?? throw new InvalidOperationException($"Template '{templateName}' not found");

        // Create isolated execution context for template
        var templateContext = CreateIsolatedTemplateContext(context, template);

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
        
        // Store outputs in parent context using both direct and output-prefixed access patterns
        StoreTemplateOutputsInParentContext(context, outputs);
        
        return new
        {
            templateName = templateName,
            outputs = outputs,
            steps = templateResults.Count
        };
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

    private void StoreTemplateOutputsInParentContext(IExecutionContext parentContext, Dictionary<string, object> outputs)
    {
        // Store step result as 'this' with outputs directly accessible ({{$.this.outputKey}})
        var stepResult = new Dictionary<string, object>(outputs);
        stepResult["type"] = "template";
        
        parentContext.Variables["this"] = stepResult;
        
        // Store with step ID if provided
        if (!string.IsNullOrEmpty(Id))
        {
            parentContext.Variables[Id] = stepResult;
        }
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

    private void LogDebugInformation(IExecutionContext context, Dictionary<string, object> contextBefore, Stopwatch stopwatch, bool success)
    {
        if (_debugLogger == null) return;
        
        var stepInfo = CreateStepDebugInfo(stopwatch, success);
        var contextChanges = DetectContextChanges(contextBefore, context.Variables);
        
        _debugLogger.LogStepExecution(stepInfo);
        _debugLogger.LogContextChanges(contextChanges);
        _debugLogger.LogRuntimeContext(context.Variables);
    }

    private StepDebugInfo CreateStepDebugInfo(Stopwatch stopwatch, bool success)
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
            Result = success ? "✅ Success" : "❌ Failed",
            Duration = stopwatch.Elapsed,
            Description = $"Execute template '{templateName}'"
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