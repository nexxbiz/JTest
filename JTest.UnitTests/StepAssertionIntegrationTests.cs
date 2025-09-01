using System.Text.Json;
using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Steps;

namespace JTest.UnitTests;

public class StepAssertionIntegrationTests
{
    private class TestExecutionContext : IExecutionContext
    {
        public Dictionary<string, object> Variables { get; } = new();
        public IList<string> Log { get; } = new List<string>();
    }

    [Fact]
    public async Task WaitStep_WithSimpleAssertions_ProcessesCorrectly()
    {
        // Arrange
        var step = new WaitStep();
        var context = new TestExecutionContext();
        
        var config = JsonSerializer.SerializeToElement(new 
        { 
            ms = 50,
            assert = new object[]
            {
                new { op = "exists", actualValue = "{{$.this.ms}}" }
            }
        });
        
        step.ValidateConfiguration(config);
        
        // Act
        var result = await step.ExecuteAsync(context);
        
        // Assert
        Assert.True(result.Success);
        Assert.Single(result.AssertionResults);
        
        var assertion = result.AssertionResults[0];
        Assert.Equal("exists", assertion.Operation);
        Assert.Equal(50, assertion.ActualValue);
        Assert.True(assertion.Success);
    }

    [Fact]
    public async Task Step_WithoutAssertions_ReturnsEmptyAssertionResults()
    {
        // Arrange
        var step = new WaitStep();
        var context = new TestExecutionContext();
        
        var config = JsonSerializer.SerializeToElement(new { ms = 10 });
        step.ValidateConfiguration(config);
        
        // Act
        var result = await step.ExecuteAsync(context);
        
        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.AssertionResults);
    }
}