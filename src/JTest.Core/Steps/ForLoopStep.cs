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
        IEnumerable<object?>? items = null;
        try
        {
            items = TypeConversionHelper.ConvertToArray(Configuration.Items, context);
        }
        catch (Exception e)
        {
            validationErrors.Add(e.Message);
        }

        if (items?.Any() == false)
        {
            validationErrors.Add("At least 1 item must be specified");
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

        var items = TypeConversionHelper
            .ConvertToArray(Configuration.Items, context)
            .ToArray();

        var stepsToIterate = Configuration.Steps.ToArray();
        var innerStepResults = new StepProcessedResult[stepsToIterate.Length];
        var allStepsSuccess = true;
        var completedIterationCount = 0;

        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            context.Variables[Configuration.CurrentIndexKey] = index;
            context.Variables[Configuration.CurrentItemKey] = item;

            for (var i = 0; i < stepsToIterate.Length; i++)
            {
                var step = stepsToIterate[i];
                var stepProcessedResult = await ExecuteStep(step, context, cancellationToken);
                innerStepResults[i] = stepProcessedResult;

                if(!stepProcessedResult.Success)
                {
                    allStepsSuccess = false;
                    break;
                }
            }

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

        return new(data, innerStepResults);
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
