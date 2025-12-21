using JTest.Core.Assertions;
using JTest.Core.Execution;

namespace JTest.Core.Steps.Configuration;

public class StepConfiguration(string? id, string? name, string? description, IEnumerable<IAssertionOperation>? assert, IReadOnlyDictionary<string, object?>? save)
{
    public string? Id { get; } = id;

    public string? Name { get; } = name;

    public string? Description { get; } = description;

    public IEnumerable<IAssertionOperation> Assert { get; } = assert ?? [];

    protected virtual void Validate(IServiceProvider serviceProvider, IExecutionContext context, IList<string> validationErrors) { }

    public bool Validate(IServiceProvider serviceProvider, IExecutionContext context, out IEnumerable<string> validationErrors)
    {
        var validationErrorsList = new List<string>();
        Validate(serviceProvider, context, validationErrorsList);
        validationErrors = validationErrorsList;

        return validationErrors.Any();
    }

    public IReadOnlyDictionary<string, object?> Save { get; } = save ?? new Dictionary<string, object?>();
}
