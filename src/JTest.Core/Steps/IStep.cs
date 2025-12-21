using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;

namespace JTest.Core.Steps;

/// <summary>
/// Interface for step implementations in the JTest execution engine
/// </summary>
public interface IStep
{  
    string TypeName { get; }

    IStepConfiguration Configuration { get; }

    /// <summary>
    /// Executes the step with the provided context
    /// </summary>
    Task<StepExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the configuration of this step
    /// </summary>
    /// <param name="context"></param>
    /// <param name="validationErrors"></param>
    /// <returns></returns>
    bool Validate(IExecutionContext context, out IEnumerable<string> validationErrors);
}