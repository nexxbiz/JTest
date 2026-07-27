namespace JTest.Language.Documents;

/// <summary>One assertion of a step.</summary>
/// <param name="Operator">The assertion operator, e.g. <c>equals</c>.</param>
/// <param name="Actual">The actual-value operand; usually an expression.</param>
/// <param name="Expected">The expected-value operand where the operator takes one.</param>
/// <param name="Description">Optional human-readable intent.</param>
public sealed record AssertionDefinition(
    string Operator,
    System.Text.Json.JsonElement? Actual,
    System.Text.Json.JsonElement? Expected,
    string? Description);
