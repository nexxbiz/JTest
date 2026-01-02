using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using NSubstitute;

namespace JTest.UnitTests.Steps;

public sealed class WhileStepTests
{
    private static readonly EqualsAssertion ConditionThatAlwaysSucceeds = new(1, 1);    

    [Fact]
    public void When_Validate_And_StepsEmpty_Then_Fails()
    {
        // Arrange
        var context = new TestExecutionContext();
        var configuration = new WhileStepConfiguration(
            [],
            100,
            ConditionThatAlwaysSucceeds
        );
        var step = GetSut(configuration);

        // Act
        var result = step.Validate(context, out var errors);

        // Assert
        Assert.False(result);
        Assert.Single(errors);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("{{ $.unknownToken }}")]
    [InlineData(-1)]
    [InlineData(0)]
    public void When_Validate_And_TimeoutInvalid_Then_Fails(object timeout)
    {
        // Arrange
        var context = new TestExecutionContext();
        var configuration = new WhileStepConfiguration(
            [Substitute.For<IStep>()],
            timeout,
            ConditionThatAlwaysSucceeds
        );
        var step = GetSut(configuration);

        // Act
        var result = step.Validate(context, out var errors);

        // Assert
        Assert.False(result);
        Assert.Single(errors);
    }

    [Fact]
    public async Task When_Execute_And_TimeoutReached_Then_BreaksLoop()
    {
        // Arrange
        var context = new TestExecutionContext();
        var configuration = new WhileStepConfiguration(
            [
                new WaitStep(new(Ms: 200)),
                new WaitStep(new(Ms: 200))
            ],
            TimeoutMs: 1000,
            Condition: ConditionThatAlwaysSucceeds
        );
        var step = GetSut(configuration);

        // Act
        var result = await step.ExecuteAsync(context, default);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal(true, result.Data["timeoutTriggered"]);
        Assert.True((int)result.Data["iterationCount"]! > 1);

        Assert.Equal(false, result.Data["stepError"]);

        Assert.True(result.InnerProcessedResults.All(x => x.Success));
    }

    [Fact]
    public async Task When_Execute_And_StepFails_Then_BreaksLoop()
    {
        // Arrange
        var context = new TestExecutionContext();
        var configuration = new WhileStepConfiguration(
            [
                new WaitStep(new(Ms: 100)),
                new WaitStep(new(Ms: "invalid value type"))
            ],
            TimeoutMs: 10000,
            Condition: ConditionThatAlwaysSucceeds
        );
        var step = GetSut(configuration);

        // Act
        var result = await step.ExecuteAsync(context, default);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal(true, result.Data["stepError"]);
        Assert.Equal(1, result.Data["iterationCount"]);
        Assert.Equal(false, result.Data["timeoutTriggered"]);

        var firstStep = result.InnerProcessedResults.First();
        Assert.True(firstStep.Success);
        var stepThatFails = result.InnerProcessedResults.Last();
        Assert.False(stepThatFails.Success);
    }

    [Fact]
    public async Task When_Execute_Then_IteratesUntilConditionNoLongerMet()
    {
        // Arrange
        var context = new TestExecutionContext();
        var saveOperations = new Dictionary<string, object?> { ["$.msWaited"] = "{{ $.this.ms }}" };
        var configuration = new WhileStepConfiguration(
            [
                new WaitStep(new(Ms: 500)),
                new WaitStep(new(Ms: 200, Save: saveOperations)),
            ],
            TimeoutMs: 10000,
            Condition: new GreaterThanAssertion("{{ $.msWaited }}", 200)
        );
        var step = GetSut(configuration);

        // Act
        var result = await step.ExecuteAsync(context, default);

        // Assert
        Assert.NotNull(result.Data);        
        Assert.Equal(false, result.Data["stepError"]);
        Assert.Equal(false, result.Data["timeoutTriggered"]);
        Assert.Equal(1, result.Data["iterationCount"]);

        Assert.True(result.InnerProcessedResults.All(x => x.Success));
    }

    private static WhileStep GetSut(WhileStepConfiguration configuration) => new(StepProcessor.Default, configuration);
}
