namespace JTest.Core.Variables
{
    public interface IVariablesContext
    {
        IReadOnlyDictionary<string, object?> GlobalVariables { get; }

        IReadOnlyDictionary<string, object?> EnvironmentVariables { get; }
    }
}
