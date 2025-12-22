using JTest.Core.Assertions;

namespace JTest.Core.Steps.Configuration;

public abstract record StepConfigurationBase : IStepConfiguration
{
    private string? updatedDescription;

    public abstract string? Id { get; init; }
    public abstract string? Name { get; init; }
    public abstract string? Description { get; init; }
    public string GetDescription() => !string.IsNullOrWhiteSpace(updatedDescription)
        ? updatedDescription
        : Description ?? string.Empty;
    public void UpdateDescription(string? value) => updatedDescription = value;

    public abstract IEnumerable<IAssertionOperation>? Assert { get; init; }
    public abstract IReadOnlyDictionary<string, object?>? Save { get; init; }
}
