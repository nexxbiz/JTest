using JTest.Core.Execution;
using JTest.Core.Steps;
using System.Text.Json;

namespace JTest.UnitTests.Steps;

[Collection("WaitStepsExecutionContext")]
public class WaitStepTests
{
    private const double expectedResultIncludingErrorMargin = 0.95;

    [Fact]
    public void Type_ShouldReturnWait()
    {
        var step = new WaitStep(new(1));
        Assert.Equal("wait", step.TypeName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void ValidateConfiguration_WithInvalidMsProperty_ReturnsFalse(int invalidMs)
    {
        // Arrange
        var step = new WaitStep(new(invalidMs));

        // Act
        var result = step.Validate(new TestExecutionContext(), out var errors);

        // Assert
        Assert.False(result);
        Assert.Single(errors);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidMs_CompletesSuccessfully()
    {
        const long durationInput = 20;
        var expectedDuration = durationInput * expectedResultIncludingErrorMargin;
        var context = new TestExecutionContext();
        var step = new WaitStep(new(durationInput));

        var result = await step.ExecuteAsync(context);

        Assert.NotNull(result.Data);
        Assert.True((long?)result.Data["duration"] >= expectedDuration, $"Expected duration to be at least {expectedDuration}ms, but got {result.Data["duration"]}ms.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task ExecuteAsync_WithNegativeMs_ThrowsException(int ms)
    {
        var context = new TestExecutionContext();
        var step = new WaitStep(new(ms));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
          async () => _ = await step.ExecuteAsync(context)
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithStringMs_ParsesCorrectly()
    {
        // Arrange
        const long durationInput = 50;
        var expectedDuration = durationInput * expectedResultIncludingErrorMargin;
        var step = new WaitStep(new($"{durationInput}"));
        var context = new TestExecutionContext();

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.True((long?)result.Data["duration"] >= expectedDuration, $"Expected duration to be at least {expectedDuration}ms, but got {result.Data["duration"]}ms.");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidStringMs_FailsValidation()
    {
        // Arrange
        var context = new TestExecutionContext();
        var config = JsonSerializer.SerializeToElement(new { ms = "invalid" });
        var step = new WaitStep(new("invalid"));

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(async () =>
        {
            _ = await step.ExecuteAsync(context);
        });
    }

    [Fact]
    public async Task ExecuteAsync_WithTokenExpression_ResolvesVariable()
    {
        // Arrange
        const long durationInput = 25;
        var expectedDuration = durationInput * expectedResultIncludingErrorMargin;
        var context = new TestExecutionContext();
        context.Variables["env"] = new { requestDelay = durationInput };
        var step = new WaitStep(new("{{$.env.requestDelay}}"));

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.True((long?)result.Data["duration"] >= expectedDuration, $"Expected duration to be at least {expectedDuration}ms, but got {result.Data["duration"]}ms.");
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingToken_Fails()
    {
        // Arrange
        var context = new TestExecutionContext();
        var step = new WaitStep(new("{{$.missing.value}}"));

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(async () =>
        {
            _ = await step.ExecuteAsync(context);
        });
    }


    [Fact]
    public async Task ExecuteAsync_WithNumericTokenResult_ParsesCorrectly()
    {
        // Arrange
        const long durationInput = 30;
        var expectedDuration = durationInput * expectedResultIncludingErrorMargin;
        var context = new TestExecutionContext();
        context.Variables["config"] = new { timeout = durationInput };
        var step = new WaitStep(new("{{$.config.timeout}}"));

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.True((long?)result.Data["duration"] >= expectedDuration, $"Expected duration to be at least {expectedDuration}ms, but got {result.Data["duration"]}ms.");
    }

    [Fact]
    public async Task ExecuteAsync_SupportsCancellation()
    {
        // Arrange
        var context = new TestExecutionContext();
        var step = new WaitStep(new(5000));

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        // Act: WaitStep uses Task.Delay which respects cancellation
        var task = step.ExecuteAsync(context, cts.Token);

        // The step should complete quickly due to our small test delay
        var result = await task;
        Assert.NotNull(result.Data);
        Assert.True((long?)result.Data["duration"] <= 5000, $"Expected duration '{result.Data["duration"]}ms' to be definitely under initially configured 5000ms due to cancellation.");
    }


    [Fact]
    public async Task Integration_WorksWithComplexVariableExpression()
    {
        const long mediumDurationInput = 50;
        var expectedDuration = mediumDurationInput * expectedResultIncludingErrorMargin;
        var context = new TestExecutionContext();
        context.Variables["env"] = new { delays = new { short_ = 10, medium = mediumDurationInput, long_ = 100 } };
        var step = new WaitStep(new("{{$.env.delays.medium}}"));

        var result = await step.ExecuteAsync(context);

        Assert.NotNull(result.Data);
        Assert.True((long?)result.Data["duration"] >= expectedDuration, $"Expected duration to be at least {expectedDuration}ms, but got {result.Data["duration"]}ms.");
    }
}