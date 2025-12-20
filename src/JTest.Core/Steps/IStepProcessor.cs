using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;

namespace JTest.Core.Steps;

public interface IStepProcessor
{
    Task<StepResult> ProcessStep(IStep step, StepConfiguration? stepConfiguration, IExecutionContext executionContext, CancellationToken cancellationToken);
}
