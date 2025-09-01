namespace JTest.Core.Execution;

/// <summary>
/// Represents the execution context for test steps, providing access to variables and logging
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// Gets the variables dictionary containing all execution variables
    /// </summary>
    Dictionary<string, object> Variables { get; }
    
    /// <summary>
    /// Gets the log list for warnings and errors during execution
    /// </summary>
    IList<string> Log { get; }
}