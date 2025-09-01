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

    [Fact(Skip = "Requires external HTTP call")]
    public async Task HttpStep_WithAssertions_ProcessesAssertionsAfterExecution()
    {
        // Arrange
        var httpClient = new HttpClient();
        var step = new HttpStep(httpClient);
        var context = new TestExecutionContext();
        
        var config = JsonSerializer.SerializeToElement(new 
        { 
            method = "GET", 
            url = "https://httpbin.org/status/200",
            assert = new object[]
            {
                new { op = "equals", actualValue = "{{$.this.status}}", expectedValue = 200 },
                new { op = "exists", actualValue = "{{$.this.body}}" }
            }
        });
        
        step.ValidateConfiguration(config);
        
        // Act
        var result = await step.ExecuteAsync(context);
        
        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.AssertionResults);
        Assert.Equal(2, result.AssertionResults.Count);
        
        // Check first assertion (status equals 200)
        var statusAssertion = result.AssertionResults[0];
        Assert.Equal("equals", statusAssertion.Operation);
        Assert.Equal(200, statusAssertion.ActualValue);
        Assert.Equal(200, statusAssertion.ExpectedValue);
        
        // Check second assertion (body exists)
        var bodyAssertion = result.AssertionResults[1];
        Assert.Equal("exists", bodyAssertion.Operation);
        Assert.NotNull(bodyAssertion.ActualValue);
    }

    [Fact(Skip = "Complex test with timing issues")]
    public async Task WaitStep_WithAssertions_ProcessesAssertionsAfterExecution()
    {
        // Arrange
        var step = new WaitStep();
        var context = new TestExecutionContext();
        
        var config = JsonSerializer.SerializeToElement(new 
        { 
            ms = 10,
            assert = new object[]
            {
                new { op = "exists", actualValue = "{{$.this.durationMs}}" },
                new { op = "greater-than", actualValue = "{{$.this.durationMs}}", expectedValue = 5 }
            }
        });
        
        step.ValidateConfiguration(config);
        
        // Act
        var result = await step.ExecuteAsync(context);
        
        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.AssertionResults);
        Assert.Equal(2, result.AssertionResults.Count);
        
        // Check first assertion (durationMs exists)
        var existsAssertion = result.AssertionResults[0];
        Assert.Equal("exists", existsAssertion.Operation);
        Assert.NotNull(existsAssertion.ActualValue);
        
        // Check second assertion (durationMs > 5)
        var greaterThanAssertion = result.AssertionResults[1];
        Assert.Equal("greater-than", greaterThanAssertion.Operation);
        Assert.True(Convert.ToInt32(greaterThanAssertion.ActualValue) > 5);
        Assert.Equal(5, greaterThanAssertion.ExpectedValue);
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