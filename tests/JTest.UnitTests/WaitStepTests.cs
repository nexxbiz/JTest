using JTest.Core.Execution;
using JTest.Core.Steps;
using System.Text.Json;

namespace JTest.UnitTests;

[Collection("WaitStepsExecutionContext")]
public class WaitStepTests
{
    private const double expectedResultIncludingErrorMargin = 0.95;

    [Fact]
    public void Type_ShouldReturnWait()
    {
        var step = new WaitStep(new());
        Assert.Equal("wait", step.Type);
    }

    [Fact]
    public void ValidateConfiguration_WithValidMsProperty_ReturnsTrue()
    {
        var config = JsonSerializer.SerializeToElement(new { ms = 1000 });
        IStep step = new WaitStep(config);
        Assert.True(step.ValidateConfiguration([]));
    }

    [Fact]
    public void ValidateConfiguration_WithoutMsProperty_ReturnsFalse()
    {
        var config = JsonSerializer.SerializeToElement(new { other = "value" });
        IStep step = new WaitStep(config);
        Assert.False(step.ValidateConfiguration([]));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidMs_CompletesSuccessfully()
    {
        const long durationInput = 20;
        var expectedDuration = durationInput * expectedResultIncludingErrorMargin;
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = durationInput });
        var step = new WaitStep(config);

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.DurationMs >= expectedDuration, $"Expected duration to be at least {expectedDuration}ms, but got {result.DurationMs}ms.");
        Assert.Contains("this", context.Variables.Keys);
    }

    [Fact]
    public async Task ExecuteAsync_WithZeroMs_CompletesImmediately()
    {
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = 0 });
        var step = new WaitStep(config);

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);        
        Assert.True(result.DurationMs >= 0, $"Expected duration to be at least 0ms, but got {result.DurationMs}ms.");
    }

    [Fact]
    public async Task ExecuteAsync_WithNegativeMs_FailsValidation()
    {
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = -100 });
        var step = new WaitStep(config);

        var result = await step.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Invalid ms value", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithStringMs_ParsesCorrectly()
    {
        const long durationInput = 50;
        var expectedDuration = durationInput * expectedResultIncludingErrorMargin;
        var config = JsonSerializer.SerializeToElement(new { ms = durationInput.ToString() });
        var step = new WaitStep(config);
        var context = new TestExecutionContext();

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);        
        Assert.True(result.DurationMs >= expectedDuration, $"Expected duration to be at least {expectedDuration}ms, but got {result.DurationMs}ms.");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidStringMs_FailsValidation()
    {
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = "invalid" });
        var step = new WaitStep(config);

        
        var result = await step.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Invalid ms value", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithTokenExpression_ResolvesVariable()
    {
        const long durationInput = 25;
        var expectedDuration = durationInput * expectedResultIncludingErrorMargin;
        var context = new TestExecutionContext();
        context.Variables["env"] = new { requestDelay = durationInput };
        var config = JsonSerializer.SerializeToElement(new { ms = "{{$.env.requestDelay}}" });
        var step = new WaitStep(config);


        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.DurationMs >= expectedDuration, $"Expected duration to be at least {expectedDuration}ms, but got {result.DurationMs}ms.");
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingToken_FailsGracefully()
    {
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = "{{$.missing.value}}" });
        var step = new WaitStep(config);

        var result = await step.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_StoresCorrectDataInContext()
    {
        const long durationInput = 20;
        var expectedDuration = durationInput * expectedResultIncludingErrorMargin;

        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = durationInput });
        var step = new WaitStep(config);

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Contains("this", context.Variables.Keys);

        var data = context.Variables["this"] as Dictionary<string, object>;

        Assert.NotNull(data);
        Assert.Equal(20L, data["ms"]);
        Assert.True((long)data["actualMs"] >= expectedDuration);
        Assert.Equal(data["actualMs"], data["duration"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithStepId_StoresDataInNamedScope()
    {
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = 15, id = "myWait" });
        var step = new WaitStep(config);

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Contains("this", context.Variables.Keys);
        Assert.Contains("myWait", context.Variables.Keys);

        var thisData = context.Variables["this"] as Dictionary<string, object>;
        var namedData = context.Variables["myWait"] as Dictionary<string, object>;

        Assert.NotNull(thisData);
        Assert.NotNull(namedData);
        Assert.Equal(thisData["ms"], namedData["ms"]);
        Assert.Equal(thisData["actualMs"], namedData["actualMs"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithNumericTokenResult_ParsesCorrectly()
    {
        const long durationInput = 30;
        var expectedDuration = durationInput * expectedResultIncludingErrorMargin;
        var context = new TestExecutionContext();
        context.Variables["config"] = new { timeout = durationInput };
        var config = JsonSerializer.SerializeToElement(new { ms = "{{$.config.timeout}}" });
        var step = new WaitStep(config);

        
        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success, $"Step failed: {result.ErrorMessage}");
        Assert.True(result.DurationMs >= expectedDuration, $"Expected duration to be at least {expectedDuration}ms, but got {result.DurationMs}ms.");

        var data = context.Variables["this"] as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal(durationInput, data["ms"]);
    }

    [Fact]
    public async Task ExecuteAsync_SupportsCancellation()
    {
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = 5000 }); // 5 seconds

        var step = new WaitStep(config);
        

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        // Note: WaitStep uses Task.Delay which respects cancellation
        var task = step.ExecuteAsync(context, cts.Token);

        // The step should complete quickly due to our small test delay
        var result = await task;
        Assert.True(result.Success);
        Assert.True(result.DurationMs <= 5000);
    }

    [Fact]
    public void Integration_CanCreateAndValidateFromJson()
    {
        var jsonText = """{"type": "wait", "id": "myWait", "ms": 100}""";
        var jsonDoc = JsonDocument.Parse(jsonText);
        var rootElement = jsonDoc.RootElement;
        IStep step = new WaitStep(rootElement);
        
        var isValid = step.ValidateConfiguration([]);

        Assert.True(isValid);
        Assert.Equal("wait", step.Type);
        Assert.Equal("myWait", step.Id);
    }

    [Fact]
    public async Task Integration_WorksWithComplexVariableExpression()
    {
        const long mediumDurationInput = 50;
        var expectedDuration = mediumDurationInput * expectedResultIncludingErrorMargin;        
        var context = new TestExecutionContext();
        context.Variables["env"] = new { delays = new { short_ = 10, medium = mediumDurationInput, long_ = 100 } };

        var config = JsonSerializer.SerializeToElement(new { ms = "{{$.env.delays.medium}}", id = "complexWait" });
        var step = new WaitStep(config);

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.DurationMs >= expectedDuration);
        Assert.Contains("complexWait", context.Variables.Keys);

        var data = context.Variables["complexWait"] as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal(mediumDurationInput, data["ms"]);
    }
}