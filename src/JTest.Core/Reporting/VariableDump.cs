using JTest.Core.Security;

namespace JTest.Core.Reporting;

/// <summary>
/// Builds the opt-in environment/global variable dump for the trace (FR-027/028). Excluded by
/// default; when included, values under secret-like keys are masked.
/// </summary>
public static class VariableDump
{
    public static IReadOnlyDictionary<string, object?>? Build(
        IReadOnlyDictionary<string, object?>? environment,
        IReadOnlyDictionary<string, object?>? globals)
    {
        var map = new Dictionary<string, object?>();
        Add(map, "env", environment);
        Add(map, "globals", globals);
        return map.Count > 0 ? map : null;
    }

    private static void Add(Dictionary<string, object?> map, string prefix, IReadOnlyDictionary<string, object?>? source)
    {
        if (source is null) return;
        foreach (var (key, value) in source)
            map[$"{prefix}.{key}"] = ValueRedactor.IsSecretKey(key) ? ValueRedactor.Mask : value?.ToString();
    }
}
