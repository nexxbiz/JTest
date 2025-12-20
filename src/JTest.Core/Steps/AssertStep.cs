using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;

namespace JTest.Core.Steps;

/// <summary>
/// Assert step implementation that does not perform any extra action than what is done in the <see cref="BaseStep{TConfiguration}"/>
/// </summary>
public sealed class AssertStep(StepConfiguration configuration) : BaseStep<StepConfiguration>(configuration)
{
    public override string Type => "assert";

    public override async Task<object?> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        return new Dictionary<string, object>
        {
            ["type"] = "assert",
            ["executed"] = true
        };
    }
}