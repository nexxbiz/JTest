using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;
using JTest.Core.TypeDescriptors;
using JTest.Core.Utilities;

namespace JTest.Core.Steps;

[TypeIdentifier("for")]
public sealed class ForLoopStep(IStepProcessor stepProcessor, ForLoopStepConfiguration configuration) : BaseStep<ForLoopStepConfiguration>(configuration)
{
    protected override void Validate(IExecutionContext context, IList<string> validationErrors)
    {
        try
        {
            // An empty item list is valid: the loop runs zero iterations. "Clean up whatever is left
            // over" is a normal shape, and its zero-items case must not be an error.
            _ = Configuration.Items.ConvertToArray(context);
        }
        catch (Exception e)
        {
            validationErrors.Add(e.Message);
        }

        if (!Configuration.Steps.Any())
        {
            validationErrors.Add($"At least 1 step must be specified");
        }
    }


    public override async Task<StepExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        if (!Validate(context, out var errors))
        {
            throw new InvalidOperationException(string.Join("; ", errors));
        }

        var items = Configuration.Items
            .ConvertToArray(context)
            .ToArray();

        var stepsToIterate = Configuration.Steps.ToArray();
        var iterations = new List<StepIteration>();
        var allInner = new List<StepProcessedResult>();
        var allStepsSuccess = true;
        var completedIterationCount = 0;

        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            context.Variables[Configuration.CurrentIndexKey] = index;
            context.Variables[Configuration.CurrentItemKey] = item;

            var iterationSteps = new List<StepProcessedResult>();
            var iterationSuccess = true;

            for (var i = 0; i < stepsToIterate.Length; i++)
            {
                var step = stepsToIterate[i];
                var stepProcessedResult = await ExecuteStep(step, context, cancellationToken);
                iterationSteps.Add(stepProcessedResult);
                allInner.Add(stepProcessedResult);

                if(!stepProcessedResult.Success)
                {
                    iterationSuccess = false;
                    allStepsSuccess = false;
                    break;
                }
            }

            // Every iteration is retained with its own steps — no overwrite (FR-013).
            iterations.Add(new StepIteration(index, iterationSuccess, iterationSteps));

            if (!allStepsSuccess)
            {
                break;
            }

            completedIterationCount++;
        }

        var data = new Dictionary<string, object?>
        {
            ["items"] = items,
            ["completedItems"] = items.Take(completedIterationCount).ToArray(),
            ["allIterationsSucceeded"] = allStepsSuccess,
            ["completedIterationCount"] = completedIterationCount
        };

        return new(data, allInner, iterations);
    }

    async Task<StepProcessedResult> ExecuteStep(IStep step, IExecutionContext context, CancellationToken cancellationToken)
    {
        try
        {
            return await stepProcessor.ProcessStep(step, context, cancellationToken);
        }
        catch (Exception e)
        {
            return new StepProcessedResult(context.StepNumber)
            {
                Step = step,
                Success = false,
                ErrorMessage = e.Message
            };
        }
    }
}
