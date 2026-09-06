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

    // Operator resolution (FR-055). The JSON Schema cannot express this: operators are matched
    // case-insensitively at run time, so a strict enum would reject spellings that execute fine.

    [Fact]
    public void UnknownOperator_IsRejected_WithLocationAndSupportedList()
    {
        var json = """
        { "version": "1.0", "tests": [ { "name": "t", "steps": [
          { "type": "assert", "assert": [ { "op": "isEqual", "actualValue": 1, "expectedValue": 1 } ] } ] } ] }
        """;

        var result = Validator.Validate(json);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("/tests/0/steps/0/assert/0/op", diagnostic.Location);
        Assert.Contains("Unknown assertion operator 'isEqual'", diagnostic.Message);
        Assert.Contains("equals", diagnostic.Message);
    }

    [Fact]
    public void UnknownOperator_InWhileCondition_IsRejected()
    {
        var json = """
        { "version": "1.0", "tests": [ { "name": "t", "steps": [
          { "type": "while", "timeoutMs": 100, "condition": { "op": "isReady", "actualValue": 1, "expectedValue": 1 },
            "steps": [ { "type": "wait", "ms": 1 } ] } ] } ] }
        """;

        var result = Validator.Validate(json);

        Assert.False(result.IsValid);
        Assert.Equal("/tests/0/steps/0/condition/op", result.Diagnostics[0].Location);
    }

    [Fact]
    public void UnknownOperator_OnStepLevelAssert_IsRejected()
    {
        var json = """
        { "version": "1.0", "tests": [ { "name": "t", "steps": [
          { "type": "http", "method": "GET", "url": "https://api.test/x",
            "assert": [ { "op": "equals", "actualValue": 1, "expectedValue": 1 },
                        { "op": "nope", "actualValue": 1, "expectedValue": 1 } ] } ] } ] }
        """;

        var result = Validator.Validate(json);

        Assert.False(result.IsValid);
        Assert.Equal("/tests/0/steps/0/assert/1/op", Assert.Single(result.Diagnostics).Location);
    }

    [Theory]
    [InlineData("equals")]
    [InlineData("notEquals")]   // operator matching is case-insensitive at run time...
    [InlineData("NOTEXISTS")]   // ...so validation must not be stricter than execution
    [InlineData("in")]
    [InlineData("length")]
    public void KnownOperators_AreAccepted_RegardlessOfCasing(string op)
    {
        var json = $$"""
        { "version": "1.0", "tests": [ { "name": "t", "steps": [
          { "type": "assert", "assert": [ { "op": "{{op}}", "actualValue": 1, "expectedValue": 1 } ] } ] } ] }
        """;

        Assert.True(Validator.Validate(json).IsValid);
    }
}
