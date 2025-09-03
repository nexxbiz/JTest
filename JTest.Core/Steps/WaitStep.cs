using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using JTest.Core.Execution;
using JTest.Core.Utilities;

namespace JTest.Core.Steps;

/// <summary>
/// Wait step implementation for delaying execution
/// </summary>
public class WaitStep : BaseStep
{
    public override string Type => "wait";

    public override bool ValidateConfiguration(JsonElement configuration)
    {
        SetConfiguration(configuration);
        return ValidateRequiredProperties();
    }

    public override async Task<StepResult> ExecuteAsync(IExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try { return await ExecuteWaitLogic(context, stopwatch); }
        catch (Exception ex) { return HandleExecutionError(ex, stopwatch); }
    }

    private async Task<StepResult> ExecuteWaitLogic(IExecutionContext context, Stopwatch stopwatch)
    {
        var contextBefore = CloneContext(context);
        var delayMs = ParseDelayMilliseconds(context);
        if (delayMs < 0) return CreateValidationFailure();
        await Task.Delay(delayMs);
        stopwatch.Stop();
        return await CreateSuccessResult(delayMs, stopwatch, context, contextBefore);
    }

    private StepResult HandleExecutionError(Exception ex, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        LogDebugInformation(new TestExecutionContext(), new Dictionary<string, object>(), stopwatch, false);
        return StepResult.CreateFailure($"Wait step failed: {ex.Message}", stopwatch.ElapsedMilliseconds);
    }

    private async Task<StepResult> CreateSuccessResult(int delayMs, Stopwatch stopwatch, IExecutionContext context, Dictionary<string, object> contextBefore)
    {
        var resultData = CreateResultData(delayMs, stopwatch.ElapsedMilliseconds);
        StoreResultInContext(context, resultData);
        
        // Process assertions after storing result data
        var assertionResults = await ProcessAssertionsAsync(context);
        
        // Determine if step should be marked as failed based on assertion results
        var hasFailedAssertions = HasFailedAssertions(assertionResults);
        
        // Log debug information
        LogDebugInformation(context, contextBefore, stopwatch, !hasFailedAssertions, assertionResults);
        
        // Create result - fail if any assertions failed
        var result = hasFailedAssertions 
            ? StepResult.CreateFailure("One or more assertions failed", stopwatch.ElapsedMilliseconds)
            : StepResult.CreateSuccess(resultData, stopwatch.ElapsedMilliseconds);
        
        result.Data = resultData;
        result.AssertionResults = assertionResults;
        return result;
    }

    private bool ValidateRequiredProperties()
    {
        return Configuration.TryGetProperty("ms", out _);
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

    private StepResult CreateValidationFailure()
    {
        return StepResult.CreateFailure("Invalid ms value: must be a positive integer");
    }

    protected override string GetStepDescription()
    {
        if (Configuration.TryGetProperty("ms", out var msProperty))
        {
            var msValue = msProperty.ValueKind == JsonValueKind.Number ? msProperty.GetInt32().ToString() : msProperty.GetString() ?? "unknown";
            return $"Wait {msValue}ms";
        }
        return "Wait for specified time";
    }
}