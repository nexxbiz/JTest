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
        var ms = ParseMs(context);
        if (ms <= 0)
        {
            validationErrors.Add("Milliseconds must be greater than 0");
        }
    }

    int ParseMs(IExecutionContext context)
    {
        var jsonElement = SerializeToJsonElement(Configuration.Ms);
        if(jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            return (int)jsonElement.GetDouble();
        }
        if(jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            var result = ResolveStringValue(jsonElement.GetString()!, context);
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
            ["expectedMs"] = Configuration.Ms,
            ["actualMs"] = stopWatch.ElapsedMilliseconds
        };

        return new(data);
    }

}