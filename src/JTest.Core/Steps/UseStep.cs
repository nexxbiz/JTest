using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps.Configuration;
using JTest.Core.Templates;
using JTest.Core.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Diagnostics;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// Step that executes a template with provided parameters in an isolated context
/// </summary>
public sealed class UseStep(IAnsiConsole ansiConsole, ITemplateContext templateContext, IStepProcessor stepProcessor, IServiceProvider serviceProvider, UseStepConfiguration configuration)
    : BaseStep<UseStepConfiguration>(configuration)
{
    protected override void Validate(IExecutionContext context, IList<string> validationErrors)
    {
        Template? template = null;
        try
        {
            var templateContext = serviceProvider.GetRequiredService<ITemplateContext>();
            template = templateContext.GetTemplate(Configuration.Template);
        }
        catch (Exception e)
        {
            validationErrors.Add(e.Message);
        }

        if (template?.Params is null)
            return;

        var contextIncludingParameters = CreateIsolatedTemplateContext(context, template);
        template.Params
            .Where(x => IsRequiredParameterMissing(x, contextIncludingParameters))
            .Select(x => $"Required template parameter '{x.Key}' not provided")
            .ToList()
            .ForEach(validationErrors.Add);

    }

    public override async Task<StepExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var stopWatch = Stopwatch.StartNew();

        // Get template name
        var templateName = Configuration.Template;

        // Get template definition
        var template = templateContext.GetTemplate(templateName);

        // Create isolated execution context for template
        var isolatedContext = CreateIsolatedTemplateContext(context, template);

        if (IsAnyRequiredParameterMissing(template, isolatedContext))
        {
            throw new InvalidOperationException("Not all required parameters are given a value");
        }

        // Execute template steps
        var templateResults = new List<object>();
        var innerStepResults = new List<StepProcessedResult>();

        foreach (var step in template.Steps)
        {
            var stepResult = await stepProcessor.ProcessStep(
                step,
                isolatedContext,
                cancellationToken
            );

            innerStepResults.Add(stepResult);

            if (!stepResult.Success)
            {
                ansiConsole.WriteLine(
                    $"Template step failed: {stepResult.ErrorMessage} - {stepResult.DetailedAssertionFailures}",
                    new Style(foreground: Color.Yellow)
                );
            }

            templateResults.Add(stepResult.Data ?? new object());
        }

        stopWatch.Stop();

        // Map template outputs to parent context
        var outputs = MapTemplateOutputs(template, isolatedContext);

        // Return step result data that will be stored by StoreResultInContext()
        var isSuccess = innerStepResults.All(x => x.Success);

        var resultData = new Dictionary<string, object?>(outputs)
        {
            ["type"] = "template",
            ["templateName"] = templateName,
            ["stepsCount"] = templateResults.Count,
            ["success"] = isSuccess,
            ["innerSteps"] = innerStepResults
        };

        var errorMessage = !isSuccess
            ? "Template execution failed"
            : string.Empty;

        if (!isSuccess)
        {
            resultData["errorMessage"] = errorMessage;
        }

        return new(resultData, innerStepResults);
    }


    private TestExecutionContext CreateIsolatedTemplateContext(IExecutionContext parentContext, Template template)
    {
        var templateContext = new TestExecutionContext();

        // Copy case data variables from parent context if they exist
        // This ensures templates can access case variables for data-driven testing
        if (parentContext.Variables.TryGetValue("case", out object? value))
        {
            templateContext.Variables["case"] = value;
        }

        // Add template parameters from 'with' configuration        
        if (Configuration.With?.Any() == true)
        {
            foreach (var param in Configuration.With)
            {
                var resolvedValue = ResolveParameterValue(param.Value, parentContext);
                templateContext.Variables[param.Key] = resolvedValue;
            }
        }

        // Add default values for missing optional parameters
        AddDefaultParameterValues(template, templateContext);

        // Initialize template's internal context
        templateContext.Variables["ctx"] = new Dictionary<string, object?>();

        return templateContext;
    }

    private object? ResolveParameterValue(object? param, IExecutionContext parentContext)
    {
        var paramValue = SerializeToJsonElement(param);

        if (paramValue.ValueKind == JsonValueKind.String)
        {
            var stringValue = paramValue.GetString() ?? "";
            return VariableInterpolator.ResolveVariableTokens(stringValue, parentContext);
        }

        return GetJsonElementValue(paramValue);
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

    private Dictionary<string, object?> MapTemplateOutputs(Template template, TestExecutionContext templateContext)
    {
        var outputs = new Dictionary<string, object?>();

        if (template.Output == null) return outputs;

        foreach (var outputMapping in template.Output)
        {
            var resolvedValue = ResolveOutputValue(outputMapping.Value, templateContext);
            outputs[outputMapping.Key] = resolvedValue;
        }

        return outputs;
    }

    private object? ResolveOutputValue(object? outputValue, TestExecutionContext templateContext)
    {
        return outputValue switch
        {
            string stringValue => VariableInterpolator.ResolveVariableTokens(stringValue, templateContext),
            JsonElement jsonElement => ResolveJsonElementValue(jsonElement, templateContext),
            _ => outputValue
        };
    }

    private object? ResolveJsonElementValue(JsonElement element, TestExecutionContext templateContext)
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

    private static bool IsAnyRequiredParameterMissing(Template template, IExecutionContext context)
    {
        foreach (var param in template.Params ?? [])
        {
            if (IsRequiredParameterMissing(param, context))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRequiredParameterMissing(KeyValuePair<string, TemplateParameter> param, IExecutionContext context)
    {
        return param.Value.Required && !context.Variables.ContainsKey(param.Key);
    }
}