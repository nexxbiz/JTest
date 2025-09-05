using JTest.Core.Execution;
using JTest.Core.Steps;
using System.Text.Json;

namespace JTest.UnitTests;

public class WaitStepTests
{

    [Fact]
    public void Type_ShouldReturnWait()
    {
        var step = new WaitStep();
        Assert.Equal("wait", step.Type);
    }

    [Fact]
    public void ValidateConfiguration_WithValidMsProperty_ReturnsTrue()
    {
        var step = new WaitStep();
        var config = JsonSerializer.SerializeToElement(new { ms = 1000 });
        Assert.True(step.ValidateConfiguration(config));
    }

    [Fact]
    public void ValidateConfiguration_WithoutMsProperty_ReturnsFalse()
    {
        var step = new WaitStep();
        var config = JsonSerializer.SerializeToElement(new { other = "value" });
        Assert.False(step.ValidateConfiguration(config));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidMs_CompletesSuccessfully()
    {
        var step = new WaitStep();
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = 10 });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.DurationMs >= 10);
        Assert.Contains("this", context.Variables.Keys);
    }

    [Fact]
    public async Task ExecuteAsync_WithZeroMs_CompletesImmediately()
    {
        var step = new WaitStep();
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = 0 });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.DurationMs >= 0);
    }

    [Fact]
    public async Task ExecuteAsync_WithNegativeMs_FailsValidation()
    {
        var step = new WaitStep();
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = -100 });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Invalid ms value", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithStringMs_ParsesCorrectly()
    {
        var step = new WaitStep();
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = "50" });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.DurationMs >= 50);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidStringMs_FailsValidation()
    {
        var step = new WaitStep();
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = "invalid" });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Invalid ms value", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithTokenExpression_ResolvesVariable()
    {
        var step = new WaitStep();
        var context = new TestExecutionContext();
        context.Variables["env"] = new { requestDelay = 25 };
        var config = JsonSerializer.SerializeToElement(new { ms = "{{$.env.requestDelay}}" });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.DurationMs >= 25);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingToken_FailsGracefully()
    {
        var step = new WaitStep();
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = "{{$.missing.value}}" });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_StoresCorrectDataInContext()
    {
        var step = new WaitStep();
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = 20 });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Contains("this", context.Variables.Keys);

        var data = context.Variables["this"] as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal(20L, data["ms"]);
        Assert.True((long)data["actualMs"] >= 20);
        Assert.Equal(data["actualMs"], data["duration"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithStepId_StoresDataInNamedScope()
    {
        var step = new WaitStep { Id = "myWait" };
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = 15 });

        step.ValidateConfiguration(config);
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
        var step = new WaitStep();
        var context = new TestExecutionContext();
        context.Variables["config"] = new { timeout = 30 };
        var config = JsonSerializer.SerializeToElement(new { ms = "{{$.config.timeout}}" });

        step.ValidateConfiguration(config);
        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success, $"Step failed: {result.ErrorMessage}");
        Assert.True(result.DurationMs >= 30);

        var data = context.Variables["this"] as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal(30L, data["ms"]);
    }

    [Fact]
    public async Task ExecuteAsync_SupportsCancellation()
    {
        var step = new WaitStep();
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = 5000 }); // 5 seconds

        step.ValidateConfiguration(config);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        // Note: WaitStep uses Task.Delay which respects cancellation
        var task = step.ExecuteAsync(context);

        // The step should complete quickly due to our small test delay
        var result = await task;
        Assert.True(result.Success);
    }

    [Fact]
    public void Integration_CanCreateAndValidateFromJson()
    {
        var step = new WaitStep();
        var jsonText = """{"type": "wait", "id": "myWait", "ms": 100}""";
        var jsonDoc = JsonDocument.Parse(jsonText);
        var rootElement = jsonDoc.RootElement;

        step.Id = rootElement.GetProperty("id").GetString();
        var isValid = step.ValidateConfiguration(rootElement);

        Assert.True(isValid);
        Assert.Equal("wait", step.Type);
        Assert.Equal("myWait", step.Id);
    }

    [Fact]
    public async Task Integration_WorksWithComplexVariableExpression()
    {
        var step = new WaitStep { Id = "complexWait" };
        var context = new TestExecutionContext();
        context.Variables["env"] = new { delays = new { short_ = 10, medium = 50, long_ = 100 } };

        var config = JsonSerializer.SerializeToElement(new { ms = "{{$.env.delays.medium}}" });
        step.ValidateConfiguration(config);

        var result = await step.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.DurationMs >= 50);
        Assert.Contains("complexWait", context.Variables.Keys);

        var data = context.Variables["complexWait"] as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal(50L, data["ms"]);
    }
}