namespace JTest.Core.Security;

/// <summary>
/// Value-based secret redaction (FR-025/026). Fixes the 1.0 defect where masking keyed only on
/// header-style names and never fired for request bodies. Secrets are identified as (a) values
/// explicitly declared as secret and (b) values found under secret-like keys; identified values
/// are then replaced wherever they appear — headers, request/response bodies, query strings.
/// Matching is by value; property casing is irrelevant to what gets masked.
/// </summary>
public sealed class ValueRedactor
{
    public const string Mask = "***REDACTED***";

    private static readonly string[] SecretKeyMarkers =
    {
        "password", "passwd", "secret", "token", "credential", "authorization",
        "auth", "bearer", "cookie", "set-cookie", "api-key", "apikey", "x-api-key", "key"
    };

    private readonly HashSet<string> _secretValues = new(StringComparer.Ordinal);

    /// <summary>Register an explicit secret value to mask wherever it appears.</summary>
    public void RegisterSecret(string? value)
    {
        if (!string.IsNullOrEmpty(value)) _secretValues.Add(value!);
    }

    /// <summary>True when the key name marks its value as secret.</summary>
    public static bool IsSecretKey(string key) =>
        !string.IsNullOrEmpty(key) &&
        SecretKeyMarkers.Any(m => key.Contains(m, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// If <paramref name="key"/> is secret-like, register its value so it is masked everywhere.
    /// Returns whether the key was treated as secret.
    /// </summary>
    public bool ConsiderKeyValue(string key, string? value)
    {
        if (value is not null && IsSecretKey(key))
        {
            RegisterSecret(value);
            return true;
        }
        return false;
    }

    /// <summary>Replace every registered secret value in <paramref name="text"/> (longest match first).</summary>
    public string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text) || _secretValues.Count == 0) return text ?? string.Empty;

        var result = text!;
        foreach (var secret in _secretValues.OrderByDescending(s => s.Length))
            result = result.Replace(secret, Mask, StringComparison.Ordinal);
        return result;
    }

    /// <summary>
    /// Redact a keyed value: a secret-like key masks its whole value; otherwise any registered
    /// secret substrings inside a string value are masked.
    /// </summary>
    public object? RedactValue(string key, object? value)
    {
        if (value is null) return null;
        if (IsSecretKey(key)) return Mask;
        return value is string s ? Redact(s) : value;
    }
}
