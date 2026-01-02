using JTest.Core.Assertions;

namespace JTest.Core.Steps.Configuration;

public interface IStepConfiguration
{
    string? Id { get; }

    string? Name { get; }

    string? Description { get; }

    IEnumerable<IAssertionOperation>? Assert { get; }

    IReadOnlyDictionary<string, object?>? Save { get; }
}