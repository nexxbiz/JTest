using JTest.Language.Diagnostics;
using JTest.Language.Reading;

namespace JTest.Language.Scopes;

/// <summary>
/// Static rules for <c>save</c> target paths. Targets are explicit: only
/// <c>$.ctx.*</c> is always writable and <c>$.globals.*</c> is writable
/// outside templates. Everything else fails closed.
/// </summary>
public static class SaveTargetRules
{
    /// <summary>Validates one save target path.</summary>
    /// <param name="target">The declared target, e.g. <c>$.ctx.token</c>.</param>
    /// <param name="insideTemplate">Whether the owning step is part of a template.</param>
    /// <param name="source">Document name for diagnostics.</param>
    /// <param name="jsonPointer">JSON pointer of the target for diagnostics.</param>
    /// <param name="sink">Diagnostic sink.</param>
    public static void Validate(
        string target,
        bool insideTemplate,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        var writesCtx = HasScopePrefix(target, ScopeNames.Ctx);
        var writesGlobals = HasScopePrefix(target, ScopeNames.Globals);

        if (!writesCtx && !writesGlobals)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.InvalidSaveTarget,
                $"Save target '{target}' must address '$.ctx.<name>' or '$.globals.<name>'.",
                source,
                jsonPointer);
            return;
        }

        if (writesGlobals && insideTemplate)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.TemplateWritesGlobals,
                $"Save target '{target}' writes to globals, which template steps may not do; export values through the template's output map instead.",
                source,
                jsonPointer);
        }
    }

    private static bool HasScopePrefix(string target, string scope)
    {
        var prefix = $"$.{scope}.";
        return target.StartsWith(prefix, StringComparison.Ordinal) && target.Length > prefix.Length;
    }
}
