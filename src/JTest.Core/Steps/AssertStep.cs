using JTest.Core.Execution;
using System.Diagnostics;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// Assert step implementation that only processes assertions without performing any other action
/// </summary>
public class AssertStep : BaseStep
{
    public AssertStep()
    {

    }

    public override string Type => "assert";

    public override bool ValidateConfiguration(JsonElement configuration)
    {
        SetConfiguration(configuration);

        // Assert step requires an 'assert' property with assertion definitions
        if (!configuration.TryGetProperty("assert", out var assertElement))
        {
            return false;
        }

        // The assert property should be an array
        return assertElement.ValueKind == JsonValueKind.Array;
    }

    public override async Task<StepResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await ExecuteAssertLogic(context, stopwatch);
        }
        catch (Exception ex)
        {
            return HandleExecutionError(ex, stopwatch);
        }
    }

    private async Task<StepResult> ExecuteAssertLogic(IExecutionContext context, Stopwatch stopwatch)
    {
        var contextBefore = CloneContext(context);

        // Assert step doesn't modify context or perform any action - it only processes assertions
        var resultData = new Dictionary<string, object>
        {
            ["type"] = "assert",
            ["executed"] = true
        };

        // Store minimal result data in context
        StoreResultInContext(context, resultData);

        // Process assertions - this is the main purpose of this step
        var assertionResults = await ProcessAssertionsAsync(context);

        stopwatch.Stop();

        // Determine if step should be marked as failed based on assertion results
        var hasFailedAssertions = HasFailedAssertions(assertionResults);

        // Create result - fail if any assertions failed
        var result = hasFailedAssertions
            ? StepResult.CreateFailure(this,"One or more assertions failed", stopwatch.ElapsedMilliseconds)
            : StepResult.CreateSuccess(this,resultData, stopwatch.ElapsedMilliseconds);

        result.Data = resultData;
        result.AssertionResults = assertionResults;

        return result;
    }

    private StepResult HandleExecutionError(Exception ex, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        return StepResult.CreateFailure(this,$"Assert step failed: {ex.Message}", stopwatch.ElapsedMilliseconds);
    }

    public override string GetStepDescription()
    {
        return $"Execute assertions";
    }
}