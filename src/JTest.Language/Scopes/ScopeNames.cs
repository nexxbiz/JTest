namespace JTest.Language.Scopes;

/// <summary>The reserved scope names of the execution context.</summary>
public static class ScopeNames
{
    /// <summary>Immutable run-level values.</summary>
    public const string Env = "env";

    /// <summary>Suite-scoped mutable values.</summary>
    public const string Globals = "globals";

    /// <summary>Frame-local scratch values.</summary>
    public const string Ctx = "ctx";

    /// <summary>The current dataset row.</summary>
    public const string Case = "case";

    /// <summary>The previous step result in the current frame.</summary>
    public const string This = "this";

    /// <summary>All reserved names, in ordinal order.</summary>
    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.Ordinal) { Env, Globals, Ctx, Case, This };
}
