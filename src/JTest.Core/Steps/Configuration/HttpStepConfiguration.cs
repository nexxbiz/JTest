using JTest.Core.Assertions;
using JTest.Core.JsonConverters;
using System.Text.Json.Serialization;

namespace JTest.Core.Steps.Configuration;


public sealed record HttpStepConfiguration(
    [property: JsonRequired] string Method,
    [property: JsonRequired] string Url,
    string? File = null,
    object? Body = null,
    string? ContentType = null,
    [property: JsonConverter(typeof(HttpHeaderCollectionJsonConverter))] IEnumerable<HttpStepRequestHeaderConfiguration>? Headers = null,
    IEnumerable<HttpStepFormFileConfiguration>? FormFiles = null,
    [property: JsonConverter(typeof(ScalarStringMapJsonConverter))] IReadOnlyDictionary<string, string>? Query = null,
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

