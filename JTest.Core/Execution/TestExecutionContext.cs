namespace JTest.Core.Execution;

/// <summary>
/// Default implementation of IExecutionContext for test execution.
/// 
/// During dataset iterations, proper cleanup is implemented via TestCaseExecutor:
/// - env variables: Immutable, preserved across iterations
/// - globals: Shared state, modifications persist across iterations
/// - other variables (ctx, this, named): Reset to original values for each iteration
/// </summary>
public class TestExecutionContext : IExecutionContext
{
    /// <summary>
    /// Gets the variables dictionary containing all execution variables
    /// </summary>
    public Dictionary<string, object> Variables { get; } = new();

    /// <summary>
    /// Gets the log list for warnings and errors during execution
    /// </summary>
    public IList<string> Log { get; } = new List<string>();

    /// <summary>
    /// Sets the case context variables for the current dataset iteration
    /// </summary>
    /// <param name="caseData">The case variables to set in the context</param>
    public void SetCase(Dictionary<string, object> caseData)
    {
        Variables["case"] = caseData;
    }

    /// <summary>
    /// Clears the case context (sets to empty dictionary)
    /// </summary>
    public void ClearCase()
    {
        Variables["case"] = new Dictionary<string, object>();
    }
}