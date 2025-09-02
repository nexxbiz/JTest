using System.Text.Json;
using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Steps;

namespace JTest.UnitTests;

public class StepAssertionIntegrationTests
{

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

    [Fact]
    public async Task UseStep_WithAssertions_ProcessesCorrectly()
    {
        // Arrange
        var templateProvider = new JTest.Core.Templates.TemplateProvider();
        var stepFactory = new StepFactory(templateProvider);
        
        // Load test template
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "test-template",
                        "params": {
                            "testParam": { "type": "string", "required": true }
                        },
                        "steps": [],
                        "output": {
                            "result": "Template result: {{$.testParam}}"
                        }
                    }
                ]
            }
        }
        """;
        templateProvider.LoadTemplatesFromJson(templateJson);
        
        var useStep = new UseStep(templateProvider, stepFactory);
        var context = new TestExecutionContext();
        
        var config = JsonSerializer.SerializeToElement(new 
        {
            template = "test-template",
            with = new { testParam = "test value" },
            assert = new object[]
            {
                new { op = "exists", actualValue = "{{$.this.result}}" },
                new { op = "equals", actualValue = "{{$.this.result}}", expectedValue = "Template result: test value" }
            }
        });
        
        useStep.ValidateConfiguration(config);
        
        // Act
        var result = await useStep.ExecuteAsync(context);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.AssertionResults.Count);
        
        var existsAssertion = result.AssertionResults[0];
        Assert.Equal("exists", existsAssertion.Operation);
        Assert.Equal("Template result: test value", existsAssertion.ActualValue);
        Assert.True(existsAssertion.Success);
        
        var equalsAssertion = result.AssertionResults[1];
        Assert.Equal("equals", equalsAssertion.Operation);
        Assert.Equal("Template result: test value", equalsAssertion.ActualValue);
        Assert.Equal("Template result: test value", equalsAssertion.ExpectedValue);
        Assert.True(equalsAssertion.Success);
    }
}