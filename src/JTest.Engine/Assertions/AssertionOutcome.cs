using System.Text.Json.Nodes;

namespace JTest.Engine.Assertions;

/// <summary>Outcome of evaluating one assertion.</summary>
/// <param name="Passed">Whether the assertion held.</param>
/// <param name="Operator">The operator name.</param>
/// <param name="Actual">The resolved actual value (unredacted; redact before capture).</param>
/// <param name="Expected">The resolved expected value (unredacted; redact before capture).</param>
/// <param name="Description">The authored description, if any.</param>
/// <param name="Message">Human-readable explanation when the assertion did not hold.</param>
public sealed record AssertionOutcome(
    bool Passed,
    string Operator,
    JsonNode? Actual,
    JsonNode? Expected,
    string? Description,
    string Message);
