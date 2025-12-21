using JTest.Core.Assertions;
using JTest.Core.Execution;
using System.Text.Json.Serialization;

namespace JTest.Core.Steps.Configuration;

[method: JsonConstructor]
public sealed class WaitStepConfiguration(string? id, string? name, string? description, IEnumerable<IAssertionOperation>? assert, IReadOnlyDictionary<string, object?>? save, int ms)
    : StepConfiguration(id, name, description, assert, save)
{
    public int Ms { get; } = ms;

    protected override void Validate(IServiceProvider serviceProvider, IExecutionContext context, IList<string> validationErrors)
    {
        if (Ms <= 0)
        {
            validationErrors.Add("Milliseconds must be greater than 0");
        }
    }
}
