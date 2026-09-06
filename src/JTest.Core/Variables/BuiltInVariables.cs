using System.Globalization;

namespace JTest.Core.Variables;

/// <summary>
/// Dynamic values resolved at the moment a token is evaluated: <c>$.now</c> and <c>$.random</c>.
///
/// These are evaluated per reference, so two references yield two values — <c>{{$.random.uuid}}</c>
/// used in a create step and again in a later fetch step will NOT match. For a value that must stay
/// the same across a whole run (the usual need when minting a uniquely-named server-side resource)
/// use <c>$.run</c> instead, which is fixed for the run and recorded in the trace.
/// </summary>
public static class BuiltInVariables
{
    public const string NowRoot = "now";
    public const string RandomRoot = "random";

    private static readonly string[] NowFields = ["iso", "date", "time", "epoch", "epochMs"];
    private static readonly string[] RandomFields = ["uuid", "id"];

    /// <summary>Whether this root name is served by a built-in (used to let real context data win).</summary>
    public static bool IsBuiltInRoot(string? root) =>
        string.Equals(root, NowRoot, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(root, RandomRoot, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves a built-in path such as <c>$.now.iso</c> or <c>$.random.uuid</c>.
    /// Returns true when resolved. Returns false with <paramref name="error"/> set when the path
    /// addresses a built-in root but names a field that does not exist — that is a mistake worth
    /// reporting, not a path that quietly matches nothing. Returns false with no error when the path
    /// is not a built-in at all.
    /// </summary>
    public static bool TryResolve(string path, out object? value, out string? error)
    {
        value = null;
        error = null;

        var segments = Split(path);
        if (segments is null || !IsBuiltInRoot(segments.Value.Root))
        {
            return false;
        }

        var (root, field) = segments.Value;
        var isNow = string.Equals(root, NowRoot, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(field))
        {
            error = $"'$.{root.ToLowerInvariant()}' needs a field: {Describe(isNow)}.";
            return false;
        }

        var resolved = isNow ? ResolveNow(field) : ResolveRandom(field);
        if (resolved is null)
        {
            error = $"Unknown field '{field}' on '$.{root.ToLowerInvariant()}'. Available: {Describe(isNow)}.";
            return false;
        }

        value = resolved;
        return true;
    }

    private static string Describe(bool isNow) =>
        string.Join(", ", (isNow ? NowFields : RandomFields).Select(f => $"$.{(isNow ? NowRoot : RandomRoot)}.{f}"));

    private static object? ResolveNow(string field)
    {
        var now = DateTimeOffset.UtcNow;

        return field.ToLowerInvariant() switch
        {
            "iso" => now.ToString("O", CultureInfo.InvariantCulture),
            "date" => now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "time" => now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            "epoch" => now.ToUnixTimeSeconds(),
            "epochms" => now.ToUnixTimeMilliseconds(),
            _ => null
        };
    }

    private static object? ResolveRandom(string field)
    {
        var uuid = Guid.NewGuid();

        return field.ToLowerInvariant() switch
        {
            "uuid" => uuid.ToString(),
            "id" => uuid.ToString("N")[..8],
            _ => null
        };
    }

    /// <summary>Splits "$.now.iso" into its root and field. Null when the shape is not "$.root[.field]".</summary>
    private static (string Root, string Field)? Split(string path)
    {
        var trimmed = path.Trim();
        if (!trimmed.StartsWith("$.", StringComparison.Ordinal)) return null;

        var remainder = trimmed[2..];
        if (remainder.Length == 0) return null;

        // Only simple dotted access is a built-in; anything with a selector is a real JSONPath.
        if (remainder.IndexOfAny(['[', '*', '?', '\'', '"', ' ']) >= 0) return null;

        var parts = remainder.Split('.');
        return parts.Length switch
        {
            1 => (parts[0], string.Empty),
            2 => (parts[0], parts[1]),
            _ => null
        };
    }
}
