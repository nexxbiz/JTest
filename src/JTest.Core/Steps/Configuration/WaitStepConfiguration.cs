using JTest.Core.Assertions;
using System.Text.Json.Serialization;

namespace JTest.Core.Steps.Configuration;

public sealed record WaitStepConfiguration(
    [property: JsonRequired] object Ms,
    string? Id = null,
    string? Name = null,
    string? Description = null,
    IEnumerable<IAssertionOperation>? Assert = null,
    IReadOnlyDictionary<string, object?>? Save = null
    )
    : StepConfigurationBase;
