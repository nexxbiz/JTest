using JTest.Core.Execution;
using JTest.Core.Utilities;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace JTest.Core.Steps;

/// <summary>
/// Wait step implementation for delaying execution
/// </summary>
public sealed class WaitStep(JsonElement configuration) : BaseStep(configuration)
{
    public override string Type => "wait";

    public override void ValidateConfiguration(List<string> validationErrors)
    {
        if(!Configuration.TryGetProperty("ms", out _))
        {
            validationErrors.Add("Wait step configuration must have an 'ms' property specifying the delay in milliseconds");
        }
    }

    public override async Task<StepResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var contextBefore = CloneContext(context);
        try { return await ExecuteWaitLogic(context, stopwatch, cancellationToken); }
        catch (Exception ex) { return HandleExecutionError(ex, stopwatch, context, contextBefore); }
    }

    private async Task<StepResult> ExecuteWaitLogic(IExecutionContext context, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        var contextBefore = CloneContext(context);
        var delayMs = ParseDelayMilliseconds(context);

        if(string.IsNullOrWhiteSpace(Description))
        {
            Description = $"Wait {delayMs}ms";
        }

        if (delayMs < 0) 
            return await CreateValidationFailure(context, contextBefore, stopwatch);

        try
        {
            await Task.Delay(delayMs, cancellationToken);
            stopwatch.Stop();
        }
        catch(TaskCanceledException)
        {
            stopwatch.Stop();
        }

        return await CreateSuccessResult(delayMs, stopwatch, context, contextBefore);
    }

    private StepResult HandleExecutionError(Exception ex, Stopwatch stopwatch, IExecutionContext context, Dictionary<string, object> contextBefore)
    {
        stopwatch.Stop();

        // Still process assertions even when wait step fails
        var assertionResults = ProcessAssertionsAsync(context).Result;

        var result = StepResult.CreateFailure(context.StepNumber, this, $"Wait step failed: {ex.Message}", stopwatch.ElapsedMilliseconds);
        result.AssertionResults = assertionResults;
        return result;
    }

    private async Task<StepResult> CreateSuccessResult(int delayMs, Stopwatch stopwatch, IExecutionContext context, Dictionary<string, object> contextBefore)
    {
        var resultData = CreateResultData(delayMs, stopwatch.ElapsedMilliseconds);

        // Use common step completion logic from BaseStep
        return await ProcessStepCompletionAsync(context, contextBefore, stopwatch, resultData);
    }

    private int ParseDelayMilliseconds(IExecutionContext context)
    {
        if (!Configuration.TryGetProperty("ms", out var msProperty)) return -1;
        var resolvedValue = ResolveMillisecondsValue(msProperty, context);
        return ParseToInteger(resolvedValue);
    }

    private object ResolveMillisecondsValue(JsonElement msProperty, IExecutionContext context)
    {
        var rawValue = GetRawValue(msProperty);
        if (rawValue is string stringValue)
            return VariableInterpolator.ResolveVariableTokens(stringValue, context);
        return rawValue;
    }

    private object GetRawValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetInt32(),
            JsonValueKind.String => element.GetString() ?? "",
            _ => ""
        };
    }

    private int ParseToInteger(object value)
    {
        return value switch
        {
            int intValue => intValue >= 0 ? intValue : -1,
            string stringValue => TryParseStringToInt(stringValue),
            _ => TryConvertToInt(value)
        };
    }

    private int TryParseStringToInt(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            return result >= 0 ? result : -1;
        return -1;
    }

    private int TryConvertToInt(object value)
    {
        try
        {
            var converted = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            return converted >= 0 ? converted : -1;
        }
        catch { return -1; }
    }

    private Dictionary<string, object> CreateResultData(long requestedMs, long actualMs)
    {
        return new Dictionary<string, object>
        {
            ["ms"] = requestedMs,
            ["actualMs"] = actualMs,
            ["duration"] = actualMs
        };
    }

    private async Task<StepResult> CreateValidationFailure(IExecutionContext context, Dictionary<string, object> contextBefore, Stopwatch stopwatch)
    {
        stopwatch.Stop();

        // Still process assertions even when validation fails
        var assertionResults = await ProcessAssertionsAsync(context);

        var result = StepResult.CreateFailure(context.StepNumber,this,"Invalid ms value: must be a positive integer");
        result.AssertionResults = assertionResults;
        return result;
    }

}