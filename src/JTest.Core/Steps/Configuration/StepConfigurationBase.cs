using JTest.Core.Assertions;

namespace JTest.Core.Steps.Configuration;

public abstract record StepConfigurationBase : IStepConfiguration
{
    public abstract string? Id { get; init; }
    public abstract string? Name { get; init; }
    public abstract string? Description { get; init; }
    public abstract IEnumerable<IAssertionOperation>? Assert { get; init; }
    public abstract IReadOnlyDictionary<string, object?>? Save { get; init; }
}
