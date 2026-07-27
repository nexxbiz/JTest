using System.Security.Cryptography;
using System.Text;

namespace JTest.Engine.Redaction;

/// <summary>
/// The secret string values of one run. Values are collected at run start
/// (declared secret paths and process-environment substitutions) and every
/// piece of captured evidence is filtered through them.
/// </summary>
public sealed class SecretSet
{
    private readonly List<(string Value, string Marker)> secrets = [];

    /// <summary>Header names whose values are always redacted.</summary>
    public static readonly IReadOnlySet<string> CredentialHeaders =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Proxy-Authorization",
            "Cookie",
            "Set-Cookie",
            "X-Api-Key",
        };

    /// <summary>The registered secrets ordered longest-first for safe substring replacement.</summary>
    public IReadOnlyList<(string Value, string Marker)> Ordered
    {
        get
        {
            var ordered = new List<(string, string)>(secrets);
            ordered.Sort(static (a, b) => b.Item1.Length.CompareTo(a.Item1.Length));
            return ordered;
        }
    }

    /// <summary>Registers one secret value; short or empty values are ignored.</summary>
    /// <param name="value">The sensitive value.</param>
    public void Register(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 2)
        {
            return;
        }

        if (secrets.Any(s => s.Value == value))
        {
            return;
        }

        secrets.Add((value, MarkerFor(value)));
    }

    /// <summary>Builds the stable redaction marker for a value.</summary>
    /// <param name="value">The sensitive value.</param>
    public static string MarkerFor(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return $"«redacted:{Convert.ToHexStringLower(hash)[..8]}»";
    }
}
