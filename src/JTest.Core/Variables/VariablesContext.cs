namespace JTest.Core.Variables;

public sealed class VariablesContext : IVariablesContext
{
    private static readonly IReadOnlyDictionary<string, object?> empty = new Dictionary<string, object?>().AsReadOnly();

    private IReadOnlyDictionary<string, object?>? globalVariables;
    private IReadOnlyDictionary<string, object?>? environmentVariables;

    public IReadOnlyDictionary<string, object?> GlobalVariables => globalVariables ?? empty;

    public IReadOnlyDictionary<string, object?> EnvironmentVariables => environmentVariables ?? empty;

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
