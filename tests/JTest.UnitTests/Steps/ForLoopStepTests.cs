using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using NSubstitute;

namespace JTest.UnitTests.Steps;

public sealed class ForLoopStepTests
{
    [Fact]
    public void When_Validate_And_StepsEmpty_Then_Fails()
    {
        // Arrange
        var context = new TestExecutionContext();
        var configuration = new ForLoopStepConfiguration(
            new object[] { new() },
            []
        );
        var step = GetSut(configuration);

        // Act
        var result = step.Validate(context, out var errors);

        // Assert
        Assert.False(result);
        Assert.Single(errors);
    }

    [Theory]
    [MemberData(nameof(InvalidItemsTestMemberData))]
    public void When_Validate_And_ArrayInvalid_Then_Fails(object items)
    {
        // Arrange
        var context = new TestExecutionContext();
        var configuration = new ForLoopStepConfiguration(
            items,
            [Substitute.For<IStep>()]
        );
        var step = GetSut(configuration);

        // Act
        var result = step.Validate(context, out var errors);

        // Assert
        Assert.False(result);
        Assert.Single(errors);
    }


    [Fact]
    public async Task When_Execute_And_StepFails_Then_BreaksLoop()
    {
        // Arrange
        var context = new TestExecutionContext();
        var configuration = new ForLoopStepConfiguration(
            new object[] { "item1" },
            [
                new WaitStep(new(Ms: 100)),
                new WaitStep(new(Ms: "invalid value type"))
            ]
        );
        var step = GetSut(configuration);

        // Act
        var result = await step.ExecuteAsync(context, default);

        // Assert
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data["items"]);

        Assert.Equal(false, result.Data["allIterationsSucceeded"]);
        Assert.Equal(0, result.Data["completedIterationCount"]);

        var firstStep = result.InnerProcessedResults.First();
        Assert.True(firstStep.Success);
        var stepThatFails = result.InnerProcessedResults.Last();
        Assert.False(stepThatFails.Success);

        var completedItems = (object[])result.Data["completedItems"]!;
        Assert.NotNull(completedItems);
        Assert.Empty(completedItems);
    }

    [Fact]
    public async Task When_Execute_Then_IteratesUntilEnd()
    {
        // Arrange
        var context = new TestExecutionContext();        
        var configuration = new ForLoopStepConfiguration(
               new object[] { "item1", "item2" },
               [
                    new WaitStep(new(Ms: 100)),
                    new WaitStep(new(Ms: 200))
               ]
           );
        var step = GetSut(configuration);

        // Act
        var result = await step.ExecuteAsync(context, default);

        // Assert
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data["items"]);

        Assert.Equal(true, result.Data["allIterationsSucceeded"]);
        Assert.Equal(2, result.Data["completedIterationCount"]);

        var completedItems = (object[])result.Data["completedItems"]!;
        Assert.NotNull(completedItems);
        Assert.Equal(2, completedItems.Length);
        Assert.Equal("item1", completedItems[0]);
        Assert.Equal("item2", completedItems[1]);

        Assert.True(result.InnerProcessedResults.All(x => x.Success));
    }

    [Fact]
    public async Task When_Execute_Then_Sets_VariablesInContext()
    {
        // Arrange
        var context = new TestExecutionContext();
        var saveOperationsStep1 = new Dictionary<string, object?> { ["$.msWaited"] = "{{ $.this.ms }}" };
        var saveOperationsStep2 = new Dictionary<string, object?> { ["$.indexFromInnerStep"] = "{{ $.currentIndex }}" };
        var configuration = new ForLoopStepConfiguration(
               new object[] { 100 },
               [
                    new WaitStep(new(Ms: "{{ $.currentItem }}", Save: saveOperationsStep1)),
                    new WaitStep(new(Ms: 66, Save: saveOperationsStep2))
               ],
               CurrentItemKey: "currentItem",
               CurrentIndexKey: "currentIndex"               
           );
        var step = GetSut(configuration);

        // Act
        _ = await step.ExecuteAsync(context, default);

        // Assert        
        Assert.NotNull(context.Variables);
        Assert.Equal(100, context.Variables["msWaited"]);
        Assert.Equal(0, context.Variables["indexFromInnerStep"]);
    }

    private static ForLoopStep GetSut(ForLoopStepConfiguration configuration) => new(StepProcessor.Default, configuration);

    public static readonly IEnumerable<object[]> InvalidItemsTestMemberData =
    [
        [Array.Empty<object>()],
        ["{{ $.unknownToken }}"],
        ["invalid type"],
        [123],
        [false]
    ];
}
