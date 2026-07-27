namespace JTest.Engine.Diagnostics;

/// <summary>
/// Stable execution-time diagnostic codes (JT06xx expressions/values,
/// JT9xxx engine). Append-only, like the language registry they extend
/// (docs/language/diagnostics.md).
/// </summary>
public static class RuntimeDiagnosticCodes
{
    /// <summary>An expression path resolved to nothing.</summary>
    public const string UnresolvableExpression = "JT0601";

    /// <summary>A <c>${NAME}</c> token names an undefined process environment variable.</summary>
    public const string UndefinedEnvironmentVariable = "JT0602";

    /// <summary>A resolved value has the wrong type for its use.</summary>
    public const string ValueTypeMismatch = "JT0603";

    /// <summary>The engine failed unexpectedly; the affected node is an error, never a pass.</summary>
    public const string EngineFailure = "JT9101";
}
