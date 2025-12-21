using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Steps;
using Xunit;

namespace JTest.UnitTests.Steps;

public sealed class AssertStepTests
{
    [Fact]
    public void TypeName_ShouldReturnAssert()
    {
        var step = new AssertStep(new(null, null, null, null, null));
        Assert.Equal("assert", step.TypeName);
    }

    [Fact]
    public async Task Can_Execute()
    {
        // Arrange
        var context = new TestExecutionContext();
        
        context.Variables["testValue"] = "hello world";
        context.Variables["numberValue"] = 42;

        var assertions = new IAssertionOperation[]
        {
            new ExistsAssertion("{{$.testValue}}"),
            new EqualsAssertion("{{$.numberValue}}", 42),
            new ContainsAssertion("{{$.testValue}}", "hello")
        };

        var step = new AssertStep(new(assertions));

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        var resultData = result.Data as Dictionary<string, object?>;
        Assert.NotNull(resultData);
        Assert.True(resultData.ContainsKey("assertions"));
        Assert.NotNull(resultData["assertions"]);
        Assert.True(resultData.ContainsKey("type"));
        Assert.Equal("assert", resultData["type"]);
    }
}