namespace JTest.Core.Variables;

public interface IVariablesContext
{
    IReadOnlyDictionary<string, object?> GlobalVariables { get; }

    IReadOnlyDictionary<string, object?> EnvironmentVariables { get; }

    void Initialize(IReadOnlyDictionary<string, object?>? env, IReadOnlyDictionary<string, object?>? globals);
}
