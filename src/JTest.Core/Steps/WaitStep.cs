using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;
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
            var ms = ParseMs(context);
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

    private int ParseMs(IExecutionContext context)
    {
        var jsonElement = SerializeToJsonElement(Configuration.Ms);
        if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            return (int)jsonElement.GetDouble();
        }
        if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            var result = ResolveStringVariable(jsonElement.GetString()!, context);
            return int.Parse(result);
        }

        return Convert.ToInt32(Configuration.Ms);
    }

    public override async Task<StepExecutionResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Description))
        {
            Description = $"Wait {Configuration.Ms}ms";
        }

        var stopWatch = Stopwatch.StartNew();
        try
        {
            var ms = ParseMs(context);
            if (ms <= 0)
            {
                throw new InvalidOperationException("Milliseconds to wait is less than or equal to zero");
            }

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