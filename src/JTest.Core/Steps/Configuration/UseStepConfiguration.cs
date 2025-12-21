using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Templates;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace JTest.Core.Steps.Configuration;

[method: JsonConstructor]
public sealed class UseStepConfiguration(string? id, string? name, string? description, IEnumerable<IAssertionOperation>? assert, IReadOnlyDictionary<string, object?>? save, string template, IReadOnlyDictionary<string, object?> with)
    : StepConfiguration(id, name, description, assert, save)
{
    public string Template { get; } = template;
    public IReadOnlyDictionary<string, object?> With { get; } = with;

    protected override void Validate(IServiceProvider serviceProvider, IExecutionContext context, IList<string> validationErrors)
    {
        var templateContext = serviceProvider.GetRequiredService<ITemplateContext>();

        Models.Template? template = null;
        try
        {
            template = templateContext.GetTemplate(Template);
        }
        catch (Exception e)
        {
            validationErrors.Add(e.Message);
        }

        if (template is null)
        {
            return;
        }

        if (template.Params is null)
            return;

        foreach (var param in template.Params)
        {
            if (param.Value.Required && !context.Variables.ContainsKey(param.Key))
            {
                validationErrors.Add($"Required template parameter '{param.Key}' not provided");
            }
        }
    }
}
