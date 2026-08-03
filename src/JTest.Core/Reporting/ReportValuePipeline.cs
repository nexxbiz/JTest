using System.Net;
using JTest.Core.Security;

namespace JTest.Core.Reporting;

/// <summary>
/// The single path every dynamic value takes into any report projection (FR-024). Redaction runs
/// first, then format-appropriate encoding, so no attacker-influenced content can produce active
/// markup (no XSS) and no declared secret can leak. All projectors MUST route values through this
/// rather than escaping ad hoc.
/// </summary>
public sealed class ReportValuePipeline
{
    private readonly ValueRedactor _redactor;

    public ReportValuePipeline(ValueRedactor redactor) => _redactor = redactor;

    public ValueRedactor Redactor => _redactor;

    /// <summary>Redact, then HTML-encode for safe embedding in the HTML report.</summary>
    public string Html(string? raw) => WebUtility.HtmlEncode(_redactor.Redact(raw));

    /// <summary>Redact, then escape for safe embedding in a Markdown document that may contain HTML.</summary>
    public string Markdown(string? raw)
    {
        var redacted = _redactor.Redact(raw);
        // The Markdown projection embeds values in tables/HTML; encode markup and escape pipes.
        return WebUtility.HtmlEncode(redacted).Replace("|", "\\|");
    }

    /// <summary>Redact a keyed value (used for header maps, parameters, context changes).</summary>
    public object? RedactValue(string key, object? value) => _redactor.RedactValue(key, value);
}
