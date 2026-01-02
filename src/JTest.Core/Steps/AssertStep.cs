using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;

namespace JTest.Core.Steps;

/// <summary>
/// Assert step implementation that does not perform any extra action than what is done in the <see cref="BaseStep{TConfiguration}"/>
/// </summary>
public sealed class AssertStep(AssertStepConfiguration configuration) : BaseStep<AssertStepConfiguration>(configuration)
{

    protected override void Validate(IExecutionContext context, IList<string> validationErrors)
    {
        if (Configuration.Assert?.Any() != true)
        {
            validationErrors.Add("Assert step must have at least 1 assertion");
        }
    }

    public override Task<StepExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, object?>
        {
            ["type"] = "assert",
            ["assertions"] = Configuration.Assert ?? []
        };

        return Task.FromResult(new StepExecutionResult(result));
    }
}