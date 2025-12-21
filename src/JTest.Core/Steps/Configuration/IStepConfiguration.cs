using JTest.Core.Assertions;
using JTest.Core.Execution;

namespace JTest.Core.Steps.Configuration;

public interface IStepConfiguration
{
    string? Id { get; }

    string? Name { get; }

    string? Description { get; }

    IEnumerable<IAssertionOperation>? Assert { get; }

    IReadOnlyDictionary<string, object?>? Save { get; }    
}