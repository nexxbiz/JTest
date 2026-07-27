using Orbyss.ProgramKit.Serialization.Json.Profiles;

namespace JTest.Reporting.Canonical;

/// <summary>The fixed serialization limits JTest applies to result documents.</summary>
public static class ReportingLimits
{
    /// <summary>Result documents up to 64 MiB, 64 levels deep.</summary>
    public static JsonSerializationLimits Default { get; } = new(
        MaxUtf8Bytes: 64L * 1024 * 1024,
        MaxDepth: 64,
        MaxTokens: 50_000_000,
        MaxObjectMembers: 1_000_000,
        MaxBufferedObjectBytes: 64L * 1024 * 1024);
}
