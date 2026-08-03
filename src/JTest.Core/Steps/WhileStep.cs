using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;
using JTest.Core.TypeDescriptors;
using JTest.Core.Utilities;

namespace JTest.Core.Steps;

[TypeIdentifier("while")]
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
        var iterations = new List<StepIteration>();
        var allInner = new List<StepProcessedResult>();

        var totalIterationCount = 0;
        var timeoutTriggered = false;
        var stepError = false;

        bool conditionMet;
        do
        {
            var iterationIndex = totalIterationCount; // 0-based
            totalIterationCount++;
            if(MustTimeOut(start, timeoutMs))
            {
                timeoutTriggered = true;
                break;
            }

            var iterationSteps = new List<StepProcessedResult>();
            var iterationSuccess = true;

            for (var i = 0; i < stepsToIterate.Length;i++)
            {
                var step = stepsToIterate[i];
                var stepProcessedResult = await ExecuteStep(step, context, cancellationToken);
                iterationSteps.Add(stepProcessedResult);
                allInner.Add(stepProcessedResult);

                timeoutTriggered = MustTimeOut(start, timeoutMs);
                stepError = !stepProcessedResult.Success;

                if (stepError || timeoutTriggered)
                {
                    iterationSuccess = !stepError;
                    break;
                }
            }

            // Every iteration is retained with its own steps — no overwrite (FR-013).
            iterations.Add(new StepIteration(iterationIndex, iterationSuccess, iterationSteps));

            conditionMet = Configuration.Condition.Execute(context).Success;

            await Delay(context);
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

        return new(data, allInner, iterations);
    }

    async Task Delay(IExecutionContext context)
    {
        if(Configuration.DelayMs is not null)
        {
            var delayMs = Configuration.DelayMs.ConvertToDouble(context);
            await Task.Delay((int)delayMs);
        }
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
