using JTest.Core.Debugging;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Utilities;
using System.Diagnostics;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// Step that executes a template with provided parameters in an isolated context
/// </summary>
public class UseStep : BaseStep
{
    private readonly ITemplateProvider _templateProvider;
    private readonly StepFactory _stepFactory;

    public UseStep(ITemplateProvider templateProvider, StepFactory stepFactory)
    {
        _templateProvider = templateProvider;
        _stepFactory = stepFactory;
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

            // Store result in context and process save operations (consistent with other steps)
            StoreResultInContext(context, result);

            //// Capture saved variables after save operations are processed
            //CaptureSavedVariables(context, contextBefore, templateInfo);

            // Use common step completion logic from BaseStep
            return await ProcessStepCompletionAsync(context, contextBefore, stopwatch, result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            context.Log.Add($"Template execution failed: {ex.Message}");

            // Still process assertions even when template execution fails  
            var assertionResults = await ProcessAssertionsAsync(context);

            var result = StepResult.CreateFailure(ex.Message, stopwatch.ElapsedMilliseconds);
            result.AssertionResults = assertionResults;
            return result;
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

        return resultData;
    }
    private TestExecutionContext CreateIsolatedTemplateContext(IExecutionContext parentContext, Template template)
    {
        var templateContext = new TestExecutionContext();

        // Copy case data variables from parent context if they exist
        // This ensures templates can access case variables for data-driven testing
        if (parentContext.Variables.ContainsKey("case"))
        {
            templateContext.Variables["case"] = parentContext.Variables["case"];
        }

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
                // Handle JsonElement default values properly
                var defaultValue = param.Value.Default;
                if (defaultValue is JsonElement element)
                {
                    defaultValue = GetJsonElementValue(element);
                }

                templateContext.Variables[param.Key] = defaultValue;
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

    private StepDebugInfo CreateStepDebugInfo(IExecutionContext context, Stopwatch stopwatch, bool success)
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

            Result = success ? "Success" : "Failed",
            Duration = stopwatch.Elapsed,
            Description = $"Execute template '{templateName}'"
        };
    }

    protected override string GetStepDescription()
    {
        var templateName = Configuration.TryGetProperty("template", out var templateElement)
            ? templateElement.GetString() ?? "unknown"
            : "unknown";
        return $"Execute template '{templateName}'";
    }
}