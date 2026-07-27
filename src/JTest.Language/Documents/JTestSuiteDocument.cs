namespace JTest.Language.Documents;

/// <summary>A validated JTest 2.0 test-suite document.</summary>
/// <param name="LanguageVersion">The exact <c>jtest</c> discriminator value.</param>
/// <param name="Info">Optional suite metadata.</param>
/// <param name="Using">Template file references, in declaration order.</param>
/// <param name="Env">Initial immutable environment values.</param>
/// <param name="Globals">Initial suite-scoped mutable values.</param>
/// <param name="Secrets">Context paths whose values are sensitive.</param>
/// <param name="Tests">The test cases, in document order.</param>
public sealed record JTestSuiteDocument(
    string LanguageVersion,
    SuiteInfo? Info,
    IReadOnlyList<string> Using,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Env,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Globals,
    IReadOnlyList<string> Secrets,
    IReadOnlyList<TestCaseDefinition> Tests);
