using JTest.Core.Execution;

namespace JTest.Core.Steps;

public interface IStepProcessor
{
    Task<StepProcessedResult> ProcessStep(IStep step, IExecutionContext executionContext, CancellationToken cancellationToken);
}
