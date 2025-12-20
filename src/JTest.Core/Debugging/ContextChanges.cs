namespace JTest.Core.Debugging;

/// <summary>
/// Represents context changes after step execution
/// </summary>
public sealed class ContextChanges
{
    /// <summary>
    /// Variable paths that were added
    /// </summary>
    public Dictionary<string, object?> Added { get; } = [];

    /// <summary>
    /// Variable paths that were modified
    /// </summary>
    public Dictionary<string, object?> Modified { get; } = [];

    /// <summary>
    /// JSONPath expressions available for assertions
    /// </summary>
    public Dictionary<string, object?> Available { get; } = [];
}