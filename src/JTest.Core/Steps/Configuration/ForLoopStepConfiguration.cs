using JTest.Core.Assertions;
using System.Text.Json.Serialization;

namespace JTest.Core.Steps.Configuration;

public sealed record ForLoopStepConfiguration(
    [property: JsonRequired] object Items,
    [property: JsonRequired] IEnumerable<IStep> Steps,
    string CurrentItemKey = "item",
    string CurrentIndexKey = "index",
    string? Id = null,
    string? Name = null,
    string? Description = null,
    IEnumerable<IAssertionOperation>? Assert = null,
    IReadOnlyDictionary<string, object?>? Save = null
) 
: StepConfigurationBase;
