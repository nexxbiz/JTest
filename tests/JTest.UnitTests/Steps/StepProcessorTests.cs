using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Steps;

namespace JTest.UnitTests.Steps;

public sealed class StepProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WithStepId_StoresResultInContextWithId()
    {
        // Arrange
        var context = new TestExecutionContext();

        var assertions = new IAssertionOperation[]
        {
            new ExistsAssertion("test")
        };
        const string id = "my-assert-step";
        var step = new AssertStep(new(assertions, Id: id));

        var sut = StepProcessor.Default;

        // Act
        var result = await sut.ProcessStep(step, context, default);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("this", context.Variables.Keys);
        Assert.Contains("my-assert-step", context.Variables.Keys);

        // Both should reference the same data
        Assert.Same(context.Variables["this"], context.Variables["my-assert-step"]);
    }
}
