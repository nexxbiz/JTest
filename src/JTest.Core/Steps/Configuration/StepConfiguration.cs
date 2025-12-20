using JTest.Core.Assertions;
using JTest.Core.Execution;

namespace JTest.Core.Steps.Configuration;

public class StepConfiguration(string? id, string? name, string? description, IEnumerable<IAssertionOperation>? assert, IReadOnlyDictionary<string, object?>? save)
{
    public string? Id { get; } = id;

    public string? Name { get; } = name;

    public string? Description { get; } = description;

    public IEnumerable<IAssertionOperation> Assert { get; } = assert ?? [];

    public virtual void ValidateConfiguration(IServiceProvider serviceProvider, IExecutionContext context, List<string> validationErrors) { }

    public IReadOnlyDictionary<string, object?> Save { get; } = save ?? new Dictionary<string, object?>();
}
