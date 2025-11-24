using JTest.Core.Execution;
using JTest.Core.Steps;
using System.Text.Json;

namespace JTest.UnitTests;

public class StepAssertionIntegrationTests
{

    [Fact]
    public async Task WaitStep_WithSimpleAssertions_ProcessesCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();

        var config = JsonSerializer.SerializeToElement(new
        {
            ms = 50,
            assert = new object[]
            {
                new { op = "exists", actualValue = "{{$.this.ms}}" }
            }
        });

        var step = new WaitStep(config);

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
        var context = new TestExecutionContext();

        var config = JsonSerializer.SerializeToElement(new { ms = 10 });
        var step = new WaitStep(config);

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
        var useStep = new UseStep(templateProvider, stepFactory, config);

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

    [Fact]
    public async Task AssertStep_WithAssertions_ProcessesCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();

        // Set up context with some test data
        context.Variables["user"] = new Dictionary<string, object>
        {
            ["name"] = "John Doe",
            ["age"] = 30,
            ["email"] = "john.doe@example.com"
        };
        context.Variables["config"] = new Dictionary<string, object>
        {
            ["maxUsers"] = 100,
            ["environment"] = "test"
        };

        var config = JsonSerializer.SerializeToElement(new
        {
            assert = new object[]
            {
                new { op = "exists", actualValue = "{{$.user.name}}" },
                new { op = "equals", actualValue = "{{$.user.age}}", expectedValue = 30 },
                new { op = "contains", actualValue = "{{$.user.email}}", expectedValue = "@example.com" },
                new { op = "greaterthan", actualValue = "{{$.config.maxUsers}}", expectedValue = 50 },
                new { op = "equals", actualValue = "{{$.config.environment}}", expectedValue = "test" }
            }
        });
        var step = new AssertStep(config);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5, result.AssertionResults.Count);

        // Verify all assertions passed
        foreach (var assertion in result.AssertionResults)
        {
            Assert.True(assertion.Success, $"Assertion {assertion.Operation} failed: {assertion.ErrorMessage}");
        }

        // Verify step stored its result in context
        Assert.Contains("this", context.Variables.Keys);
        var thisResult = Assert.IsType<Dictionary<string, object>>(context.Variables["this"]);
        Assert.Equal("assert", thisResult["type"]);
        Assert.Equal(true, thisResult["executed"]);
    }

    [Fact]
    public async Task AssertStep_WithMixedPassFailAssertions_ReportsCorrectResults()
    {
        // Arrange
        var context = new TestExecutionContext();

        context.Variables["testData"] = "hello world";

        var config = JsonSerializer.SerializeToElement(new
        {
            assert = new object[]
            {
                new { op = "exists", actualValue = "{{$.testData}}" },           // Should pass
                new { op = "equals", actualValue = "{{$.testData}}", expectedValue = "hello world" }, // Should pass
                new { op = "equals", actualValue = "{{$.testData}}", expectedValue = "goodbye" },     // Should fail
                new { op = "contains", actualValue = "{{$.testData}}", expectedValue = "world" },     // Should pass
                new { op = "contains", actualValue = "{{$.testData}}", expectedValue = "xyz" }        // Should fail
            }
        });

        var step = new AssertStep(config);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success); // Step execution should fail when assertions fail
        Assert.Equal(5, result.AssertionResults.Count);

        // Check specific assertion results
        Assert.True(result.AssertionResults[0].Success);  // exists
        Assert.True(result.AssertionResults[1].Success);  // equals (pass)
        Assert.False(result.AssertionResults[2].Success); // equals (fail)
        Assert.True(result.AssertionResults[3].Success);  // contains (pass)
        Assert.False(result.AssertionResults[4].Success); // contains (fail)
    }
}