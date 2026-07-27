namespace JTest.Engine.Ports;

/// <summary>Process environment lookup used for load-time <c>${NAME}</c> substitution.</summary>
public interface IProcessEnvironment
{
    /// <summary>Returns the variable value, or null when undefined.</summary>
    /// <param name="name">The variable name.</param>
    string? GetValue(string name);
}
