using System.Text.Json;
using JTest.Language.Diagnostics;

namespace JTest.Language.Reading;

/// <summary>Static binding of the <c>jtest</c> language discriminator.</summary>
internal static class LanguageVersionBinder
{
    internal static string? Bind(JsonElement root, string source, ICollection<LanguageDiagnostic> sink)
    {
        if (!root.TryGetProperty("jtest", out var version) ||
            version.ValueKind != JsonValueKind.String ||
            version.GetString() != LanguageContract.LanguageVersion)
        {
            Diag.Error(
                sink,
                DiagnosticCodes.UnsupportedLanguageVersion,
                $"The document must declare \"jtest\": \"{LanguageContract.LanguageVersion}\".",
                source,
                "/jtest");
            return null;
        }

        return version.GetString();
    }
}
