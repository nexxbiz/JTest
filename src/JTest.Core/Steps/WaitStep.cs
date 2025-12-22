using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;
using JTest.Core.Utilities;
using System.Diagnostics;

namespace JTest.Core.Steps;

/// <summary>
/// Wait step implementation for delaying execution
/// </summary>
public sealed class WaitStep(WaitStepConfiguration configuration) : BaseStep<WaitStepConfiguration>(configuration)
{
    protected override void Validate(IExecutionContext context, IList<string> validationErrors)
    {
        try
        {
            var ms = Configuration.Ms.ConvertToDouble(context);
            if (ms <= 0)
            {
                validationErrors.Add("Milliseconds must be greater than 0");
            }
        }
        catch (Exception e)
        {
            validationErrors.Add($"Could not parse/resolve milliseconds '{Configuration.Ms}'. Error: {e.Message}");
        }
    }    

    public override async Task<StepExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Description))
        {
            Description = $"Wait {Configuration.Ms}ms";
        }

        if (!Validate(context, out var errors))
        {
            throw new InvalidOperationException(string.Join("; ", errors));
        }

        var stopWatch = Stopwatch.StartNew();
        try
        {
            var ms = (int)Configuration.Ms.ConvertToDouble(context);
            await Task.Delay(ms, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Gracefully handle cancellation
        }
        finally
        {
            stopWatch.Stop();
        }

        var data = new Dictionary<string, object?>
        {
            ["ms"] = Configuration.Ms,
            ["duration"] = stopWatch.ElapsedMilliseconds
        };

        return new(data);
    }

}