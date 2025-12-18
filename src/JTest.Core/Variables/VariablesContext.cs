namespace JTest.Core.Variables
{
    public sealed class VariablesContext : IVariablesContext
    {
        public VariablesContext()
        {
            GlobalVariables = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            EnvironmentVariables = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, object?> GlobalVariables { get; }

        public IReadOnlyDictionary<string, object?> EnvironmentVariables { get; }
    }
}
