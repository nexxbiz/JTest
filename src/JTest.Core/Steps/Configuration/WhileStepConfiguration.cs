using JTest.Core.Assertions;
using System.Text.Json.Serialization;

namespace JTest.Core.Steps.Configuration;

public sealed record WhileStepConfiguration(
    [property: JsonRequired] IEnumerable<IStep> Steps,
    [property: JsonRequired] object TimeoutMs,
    [property: JsonRequired] IAssertionOperation Condition,
    string? Id = null,
    string? Name = null,
    string? Description = null,
    IEnumerable<IAssertionOperation>? Assert = null,
    IReadOnlyDictionary<string, object?>? Save = null
) 
: StepConfigurationBase;
