namespace JTest.Core.Variables;

public sealed class VariablesContext : IVariablesContext
{
    private static readonly IReadOnlyDictionary<string, object?> empty = new Dictionary<string, object?>().AsReadOnly();

    private IReadOnlyDictionary<string, object?>? globalVariables;
    private IReadOnlyDictionary<string, object?>? environmentVariables;

    /// <summary>
    /// Created once, on first use, and then fixed for the rest of the run — every step that reads
    /// <c>$.run</c> sees the same values.
    /// </summary>
    private readonly Lazy<IReadOnlyDictionary<string, object?>> runVariables = new(CreateRunVariables);

    public IReadOnlyDictionary<string, object?> GlobalVariables => globalVariables ?? empty;

    public IReadOnlyDictionary<string, object?> EnvironmentVariables => environmentVariables ?? empty;

    public IReadOnlyDictionary<string, object?> RunVariables => runVariables.Value;

    private static IReadOnlyDictionary<string, object?> CreateRunVariables()
    {
        var uuid = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow;

        return new Dictionary<string, object?>
        {
            // A full v4 GUID, for anything that wants maximum collision resistance.
            ["uuid"] = uuid.ToString(),

            // A short token safe to embed in URLs, route names and other identifiers, where a full
            // GUID is unwieldy — the common case for "make this resource unique per run".
            ["id"] = uuid.ToString("N")[..8],

            // ISO-8601 UTC, sortable and unambiguous.
            ["timestamp"] = startedAt.ToString("O"),

            ["epoch"] = startedAt.ToUnixTimeSeconds(),
            ["epochMs"] = startedAt.ToUnixTimeMilliseconds()
        }.AsReadOnly();
    }

    public void Initialize(IReadOnlyDictionary<string, object?>? env, IReadOnlyDictionary<string, object?>? globals)
    {
        if (globalVariables is not null || environmentVariables is not null)
        {
            throw new InvalidProgramException("Variables context is already initialized");
        }

        environmentVariables = env ?? empty;
        globalVariables = globals ?? empty;
    }
}
