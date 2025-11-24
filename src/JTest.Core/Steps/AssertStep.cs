using JTest.Core.Execution;
using System.Diagnostics;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// Assert step implementation that only processes assertions without performing any other action
/// </summary>
public sealed class AssertStep(JsonElement configuration) : BaseStep(configuration)
{
    public override string Type => "assert";

    public override void ValidateConfiguration(List<string> validationErrors)
    {        
        // Assert step requires an 'assert' property with assertion definitions
        if (!Configuration.TryGetProperty("assert", out var assertElement))
        {
            validationErrors.Add("Assert step configuration must have an 'assert' property");
        }

        // The assert property should be an array
        if(assertElement.ValueKind != JsonValueKind.Array)
        {
            validationErrors.Add("'Assert' property must be an array");
        }        
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
            return HandleExecutionError(context, ex, stopwatch);
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
            ? StepResult.CreateFailure(context.StepNumber, this,"One or more assertions failed", stopwatch.ElapsedMilliseconds)
            : StepResult.CreateSuccess(context.StepNumber, this,resultData, stopwatch.ElapsedMilliseconds);

        result.Data = resultData;
        result.AssertionResults = assertionResults;

        return result;
    }

    private StepResult HandleExecutionError(IExecutionContext context, Exception ex, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        return StepResult.CreateFailure(context.StepNumber, this,$"Assert step failed: {ex.Message}", stopwatch.ElapsedMilliseconds);
    }
}