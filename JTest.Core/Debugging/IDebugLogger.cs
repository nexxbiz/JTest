namespace JTest.Core.Debugging;

using JTest.Core.Assertions;
using JTest.Core.Models;

/// <summary>
/// Interface for debug logging during test step execution
/// </summary>
public interface IDebugLogger
{
    /// <summary>
    /// Logs step execution information
    /// </summary>
    void LogStepExecution(StepDebugInfo stepInfo);
    
    /// <summary>
    /// Logs context changes after step execution
    /// </summary>
    void LogContextChanges(ContextChanges changes);
    
    /// <summary>
    /// Logs current runtime context
    /// </summary>
    void LogRuntimeContext(Dictionary<string, object> context);
    
    /// <summary>
    /// Logs assertion execution results
    /// </summary>
    void LogAssertionResults(List<AssertionResult> assertionResults);
    
    /// <summary>
    /// Logs test execution summary with overall results
    /// </summary>
    void LogTestSummary(List<JTestCaseResult> testResults);
}