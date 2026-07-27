using JTest.Language.Diagnostics;

namespace JTest.Language.Semantics;

/// <summary>Validates cross-file semantics of a suite and its templates.</summary>
public interface ISuiteBundleValidator
{
    /// <summary>Validates template references, parameters, and invocation cycles.</summary>
    /// <param name="bundle">The suite with its loaded template files.</param>
    IReadOnlyList<LanguageDiagnostic> Validate(SuiteBundle bundle);
}
