using JTest.Core.Execution;
using JTest.Core.Steps.Configuration;
using System.Diagnostics;

namespace JTest.Core.Steps;

/// <summary>
/// Wait step implementation for delaying execution
/// </summary>
public sealed class WaitStep(WaitStepConfiguration configuration) : BaseStep<WaitStepConfiguration>(configuration)
{
    public override async Task<object?> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Description))
        {
            Description = $"Wait {Configuration.Ms}ms";
        }

        var stopWatch = Stopwatch.StartNew();
        try
        {
            await Task.Delay(Configuration.Ms, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Gracefully handle cancellation
        }
        finally
        {
            stopWatch.Stop();
        }

        return new Dictionary<string, object>
        {
            ["expectedMs"] = Configuration.Ms,
            ["actualMs"] = stopWatch.ElapsedMilliseconds
        };
    }

}