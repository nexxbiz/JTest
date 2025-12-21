using JTest.Core.Assertions;
using JTest.Core.Execution;
using System.Text.Json.Serialization;

namespace JTest.Core.Steps.Configuration;


public sealed record HttpStepConfiguration(
    [property: JsonRequired] string Method,
    [property: JsonRequired] string Url,
    string? File = null,
    object? Body = null,
    string? ContentType = null,
    IEnumerable<HttpStepRequestHeaderConfiguration>? Headers = null,
    IEnumerable<HttpStepFormFileConfiguration>? FormFiles = null,
    IReadOnlyDictionary<string, string>? Query = null,
    string? Id = null,
    string? Name = null,
    string? Description = null,
    IEnumerable<IAssertionOperation>? Assert = null,
    IReadOnlyDictionary<string, object?>? Save = null
)
    : StepConfigurationBase;

public sealed record HttpStepRequestHeaderConfiguration(
    [property: JsonRequired] string Name,
    [property: JsonRequired] string Value
);

public sealed record HttpStepFormFileConfiguration(
    [property: JsonRequired] string Name,
    [property: JsonRequired] string FileName,
    [property: JsonRequired] string Path,
    [property: JsonRequired] string ContentType
);

