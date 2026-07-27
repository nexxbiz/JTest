namespace JTest.Engine.Ports;

/// <summary>The real process environment.</summary>
public sealed class SystemProcessEnvironment : IProcessEnvironment
{
    /// <inheritdoc />
    public string? GetValue(string name) => Environment.GetEnvironmentVariable(name);
}
