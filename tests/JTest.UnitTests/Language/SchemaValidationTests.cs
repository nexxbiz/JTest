using JTest.Core.Language.Validation;
using Xunit;

namespace JTest.UnitTests.Language;

public class SchemaValidationTests
{
    private static readonly SchemaValidator Validator = new();

    [Fact]
    public void ValidDefinition_Passes()
    {
        var json = """
        {
          "version": "1.0",
          "tests": [
            {
              "name": "reads value",
              "steps": [
                { "type": "http", "method": "GET", "url": "https://api.test/x" },
                { "type": "for", "items": ["a", "b"], "steps": [ { "type": "wait", "ms": 1 } ] },
                { "type": "assert", "assert": [ { "op": "equals", "actualValue": 1, "expectedValue": 1 } ] }
              ]
            }
          ]
        }
        """;

        var result = Validator.Validate(json);

        Assert.True(result.IsValid,
            string.Join("; ", result.Diagnostics.Select(d => $"{d.Location}:{d.Message}")));
    }

    [Fact]
    public void UnknownStepType_IsRejected_WithLocatedDiagnostic()
    {
        var json = """{ "version": "1.0", "tests": [ { "name": "t", "steps": [ { "type": "teleport" } ] } ] }""";

        var result = Validator.Validate(json);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, d => d.Location.Contains("/tests/0/steps/0"));
    }

    [Fact]
    public void HttpStepMissingUrl_IsRejected()
    {
        var json = """{ "version": "1.0", "tests": [ { "name": "t", "steps": [ { "type": "http", "method": "GET" } ] } ] }""";

        Assert.False(Validator.Validate(json).IsValid);
    }

    [Fact]
    public void MissingTopLevelTests_IsRejected()
    {
        Assert.False(Validator.Validate("""{ "version": "1.0" }""").IsValid);
    }

    [Fact]
    public void CaseMissingName_IsRejected()
    {
        Assert.False(Validator.Validate("""{ "version": "1.0", "tests": [ { "steps": [] } ] }""").IsValid);
    }

    [Fact]
    public void InvalidJson_IsReported_NotThrown()
    {
        var result = Validator.Validate("{ not json ");

        Assert.False(result.IsValid);
        Assert.Contains("Invalid JSON", result.Diagnostics[0].Message);
    }
}
