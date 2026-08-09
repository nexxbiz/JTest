using JTest.Core.Language.Validation;
using JTest.Core.Steps.Configuration;
using JTest.UnitTests.TestHelpers;
using System.Text.Json;
using Xunit;

namespace JTest.UnitTests.Steps;

/// <summary>
/// `jtest validate` must never green-light a file the runner cannot load. A numeric query value —
/// { "query": { "take": 1 } }, which is what an author naturally writes — validated clean and then
/// failed deserialization at run time. Query strings and headers carry text, so a scalar is
/// unambiguous and is accepted; only shapes with no textual form are rejected, and then by
/// validation rather than at run time.
/// </summary>
public class HttpQueryAndHeaderShapesTests
{
    private static readonly JsonSerializerOptions Options = JsonSerializerHelper.Options;

    [Fact]
    public void NumericAndBooleanQueryValues_AreAcceptedAsText()
    {
        const string json = """
        { "method": "GET", "url": "https://api.test/x", "query": { "take": 1, "ratio": 1.5, "flag": true, "name": "x" } }
        """;

        var configuration = JsonSerializer.Deserialize<HttpStepConfiguration>(json, Options)!;

        Assert.Equal("1", configuration.Query!["take"]);
        Assert.Equal("1.5", configuration.Query["ratio"]);
        Assert.Equal("true", configuration.Query["flag"]);
        Assert.Equal("x", configuration.Query["name"]);
    }

    [Fact]
    public void ObjectValuedQuery_IsRejected_NamingTheKey()
    {
        const string json = """
        { "method": "GET", "url": "https://api.test/x", "query": { "filter": { "nested": true } } }
        """;

        var error = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<HttpStepConfiguration>(json, Options));

        Assert.Contains("filter", error.Message);
    }

    [Fact]
    public void Headers_AcceptTheObjectMapShape()
    {
        const string json = """
        { "method": "GET", "url": "https://api.test/x", "headers": { "X-Token": "abc", "X-Count": 5 } }
        """;

        var configuration = JsonSerializer.Deserialize<HttpStepConfiguration>(json, Options)!;
        var headers = configuration.Headers!.ToDictionary(h => h.Name, h => h.Value);

        Assert.Equal("abc", headers["X-Token"]);
        Assert.Equal("5", headers["X-Count"]);
    }

    [Fact]
    public void Headers_StillAcceptTheArrayShape()
    {
        const string json = """
        { "method": "GET", "url": "https://api.test/x", "headers": [ { "name": "X-Token", "value": "abc" } ] }
        """;

        var configuration = JsonSerializer.Deserialize<HttpStepConfiguration>(json, Options)!;
        var header = Assert.Single(configuration.Headers!);

        Assert.Equal("X-Token", header.Name);
        Assert.Equal("abc", header.Value);
    }

    // The schema must agree with the runner, so validation catches these at authoring time.

    [Fact]
    public void Schema_AcceptsScalarQueryValues()
    {
        var json = Suite("""{ "type": "http", "method": "GET", "url": "https://api.test/x", "query": { "take": 1 } }""");

        Assert.True(new SchemaValidator().Validate(json).IsValid);
    }

    [Fact]
    public void Schema_AcceptsBothHeaderShapes()
    {
        var map = Suite("""{ "type": "http", "method": "GET", "url": "https://api.test/x", "headers": { "X-Token": "abc" } }""");
        var array = Suite("""{ "type": "http", "method": "GET", "url": "https://api.test/x", "headers": [ { "name": "X-Token", "value": "abc" } ] }""");

        Assert.True(new SchemaValidator().Validate(map).IsValid);
        Assert.True(new SchemaValidator().Validate(array).IsValid);
    }

    [Fact]
    public void Schema_RejectsAQueryValueWithNoTextualForm()
    {
        var json = Suite("""{ "type": "http", "method": "GET", "url": "https://api.test/x", "query": { "filter": { "nested": true } } }""");

        Assert.False(new SchemaValidator().Validate(json).IsValid);
    }

    private static string Suite(string step) =>
        $$"""{ "version": "1.0", "tests": [ { "name": "t", "steps": [ {{step}} ] } ] }""";
}
