using JTest.Core.Assertions;
using System.Text.Json.Serialization;

namespace JTest.Core.Steps.Configuration;


[method: JsonConstructor]
public sealed class HttpStepConfiguration(
    string? id,
    string? name,
    string? description,
    IEnumerable<IAssertionOperation>? assert,
    IReadOnlyDictionary<string, object?>? save,
    string method,
    string url,
    IReadOnlyDictionary<string, string>? query,
    string? file,
    object? body,
    string? contentType,
    IEnumerable<HttpStepRequestHeaderConfiguration>? headers,
    IEnumerable<HttpStepFormFileConfiguration>? formFiles
)
    : StepConfiguration(id, name, description, assert, save)
{
    public string Method { get; } = method;
    public string Url { get; } = url;
    public IReadOnlyDictionary<string, string>? Query { get; } = query;
    public string? File { get; } = file;
    public object? Body { get; } = body;
    public string? ContentType { get; } = contentType;
    public IEnumerable<HttpStepRequestHeaderConfiguration>? Headers { get; } = headers;
    public IEnumerable<HttpStepFormFileConfiguration>? FormFiles { get; } = formFiles;
}

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

