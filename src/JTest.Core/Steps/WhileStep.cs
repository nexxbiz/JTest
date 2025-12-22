using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;
using JTest.Core.Utilities;

namespace JTest.Core.Steps;

public sealed class WhileStep(IStepProcessor stepProcessor, WhileStepConfiguration configuration) : BaseStep<WhileStepConfiguration>(configuration)
{
    protected override void Validate(IExecutionContext context, IList<string> validationErrors)
    {
        var timeoutMs = Configuration.TimeoutMs.ConvertToDouble(context);
        if(timeoutMs <= 0)
        {
            validationErrors.Add($"TimeoutMs must be greater than 0");
        }

        if(!Configuration.Steps.Any())
        {
            validationErrors.Add($"At least 1 step must be specified");
        }
    }

    public override async Task<StepExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        if(!Validate(context, out var errors))
        {
            throw new InvalidOperationException(string.Join("; ", errors));
        }

        var start = DateTime.UtcNow;
        var timeoutMs = (int)Configuration.TimeoutMs.ConvertToDouble(context);

        var stepsToIterate = Configuration.Steps.ToArray();
        var innerStepResults = new StepProcessedResult[stepsToIterate.Length];

        var totalIterationCount = 0;
        var timeoutTriggered = false;
        var stepError = false;

        bool conditionMet;
        do
        {
            totalIterationCount++;
            if(MustTimeOut(start, timeoutMs))
            {
                timeoutTriggered = true;
                break;
            }

            for (var i = 0; i < stepsToIterate.Length;i++)
            {
                var step = stepsToIterate[i];
                var stepProcessedResult = await ExecuteStep(step, context, cancellationToken);
                innerStepResults[i] = stepProcessedResult;

                timeoutTriggered = MustTimeOut(start, timeoutMs);
                stepError = !stepProcessedResult.Success;
                
                if (stepError || timeoutTriggered)
                {                    
                    break;
                }
            }

            conditionMet = Configuration.Condition.Execute(context).Success;
        }
        while (!stepError && !timeoutTriggered && conditionMet);

        var data = new Dictionary<string, object?>
        {
            ["stepError"] = stepError,
            ["timeoutMs"] = timeoutMs,
            ["timeoutTriggered"] = timeoutTriggered,
            ["iterationCount"] = totalIterationCount,
            ["durationMs"] = (DateTime.UtcNow - start).TotalMilliseconds
        };

        return new(data, innerStepResults);
    }

    async Task<StepProcessedResult> ExecuteStep(IStep step, IExecutionContext context, CancellationToken cancellationToken)
    {
        try
        {
            return await stepProcessor.ProcessStep(step, context, cancellationToken);            
        }
        catch(Exception e)
        {
            return new StepProcessedResult(context.StepNumber)
            {
                Step = step,
                Success = false,
                ErrorMessage = e.Message
            };
        }
    }

    static bool MustTimeOut(DateTime start, int timeoutMs)
    {
        var timestamp = DateTime.UtcNow;
        var currentTicksRunning = timestamp - start;
        return currentTicksRunning.TotalMilliseconds >= timeoutMs;
    }
}
