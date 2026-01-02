using JTest.Core.Assertions;
using System.Text.Json.Serialization;

namespace JTest.Core.Steps.Configuration;

public sealed record AssertStepConfiguration(
    [property: JsonRequired] IEnumerable<IAssertionOperation>? Assert,
    string? Id = null,
    string? Name = null,
    string? Description = null,
    IReadOnlyDictionary<string, object?>? Save = null
)

: StepConfigurationBase;