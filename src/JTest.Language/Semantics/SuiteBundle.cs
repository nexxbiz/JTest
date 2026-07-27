using JTest.Language.Documents;

namespace JTest.Language.Semantics;

/// <summary>A suite together with the template files it loads.</summary>
/// <param name="SuiteSource">Suite document name for diagnostics.</param>
/// <param name="Suite">The bound suite document.</param>
/// <param name="TemplateFiles">The loaded template files with their source names.</param>
public sealed record SuiteBundle(
    string SuiteSource,
    JTestSuiteDocument Suite,
    IReadOnlyList<(string Source, TemplateFileDocument Document)> TemplateFiles);
