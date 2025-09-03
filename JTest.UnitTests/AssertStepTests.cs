using System.Text.Json;
using JTest.Core.Execution;
using JTest.Core.Steps;

namespace JTest.UnitTests;

public class AssertStepTests
{
    [Fact]
    public void Type_ShouldReturnAssert()
    {
        var step = new AssertStep();
        Assert.Equal("assert", step.Type);
    }

    [Fact]
    public void ValidateConfiguration_WithValidAssertProperty_ReturnsTrue()
    {
        var step = new AssertStep();
        var config = JsonSerializer.SerializeToElement(new 
        { 
            assert = new object[]
            {
                new { op = "exists", actualValue = "test" }
            }
        });
        Assert.True(step.ValidateConfiguration(config));
    }

    [Fact]
    public void ValidateConfiguration_WithoutAssertProperty_ReturnsFalse()
    {
        var step = new AssertStep();
        var config = JsonSerializer.SerializeToElement(new { other = "value" });
        Assert.False(step.ValidateConfiguration(config));
    }

    [Fact]
    public void ValidateConfiguration_WithNonArrayAssertProperty_ReturnsFalse()
    {
        var step = new AssertStep();
        var config = JsonSerializer.SerializeToElement(new { assert = "not an array" });
        Assert.False(step.ValidateConfiguration(config));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidAssertions_ProcessesCorrectly()
    {
        // Arrange
        var step = new AssertStep();
        var context = new TestExecutionContext();
        
        // Set up some context variables for assertions to reference
        context.Variables["testValue"] = "hello world";
        context.Variables["numberValue"] = 42;
        
        var config = JsonSerializer.SerializeToElement(new 
        { 
            assert = new object[]
            {
                new { op = "exists", actualValue = "{{$.testValue}}" },
                new { op = "equals", actualValue = "{{$.numberValue}}", expectedValue = 42 },
                new { op = "contains", actualValue = "{{$.testValue}}", expectedValue = "hello" }
            }
        });
        
        step.ValidateConfiguration(config);
        
        // Act
        var result = await step.ExecuteAsync(context);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.AssertionResults.Count);
        
        // Check that all assertions passed
        foreach (var assertionResult in result.AssertionResults)
        {
            Assert.True(assertionResult.Success, $"Assertion {assertionResult.Operation} failed: {assertionResult.ErrorMessage}");
        }
        
        // Check that the step added its own result data to context
        Assert.Contains("this", context.Variables.Keys);
        var thisResult = Assert.IsType<Dictionary<string, object>>(context.Variables["this"]);
        Assert.Equal("assert", thisResult["type"]);
        Assert.Equal(true, thisResult["executed"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingAssertions_ReturnsCorrectResults()
    {
        // Arrange
        var step = new AssertStep();
        var context = new TestExecutionContext();
        
        context.Variables["testValue"] = "hello";
        
        var config = JsonSerializer.SerializeToElement(new 
        { 
            assert = new object[]
            {
                new { op = "exists", actualValue = "{{$.testValue}}" }, // Should pass
                new { op = "equals", actualValue = "{{$.testValue}}", expectedValue = "goodbye" }, // Should fail
                new { op = "equals", actualValue = "{{$.testValue}}", expectedValue = "hello" } // Should pass
            }
        });
        
        step.ValidateConfiguration(config);
        
        // Act
        var result = await step.ExecuteAsync(context);
        
        // Assert
        Assert.False(result.Success); // Step execution should fail when assertions fail
        Assert.Equal(3, result.AssertionResults.Count);
        
        // Check individual assertion results
        Assert.True(result.AssertionResults[0].Success); // exists should pass
        Assert.False(result.AssertionResults[1].Success); // equals should fail
        Assert.True(result.AssertionResults[2].Success); // equals should pass
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyAssertionsArray_ReturnsSuccess()
    {
        // Arrange
        var step = new AssertStep();
        var context = new TestExecutionContext();
        
        var config = JsonSerializer.SerializeToElement(new { assert = new object[0] });
        step.ValidateConfiguration(config);
        
        // Act
        var result = await step.ExecuteAsync(context);
        
        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.AssertionResults);
    }

    [Fact]
    public async Task ExecuteAsync_WithStepId_StoresResultInContextWithId()
    {
        // Arrange
        var step = new AssertStep();
        step.Id = "my-assert-step";
        var context = new TestExecutionContext();
        
        var config = JsonSerializer.SerializeToElement(new 
        { 
            assert = new object[]
            {
                new { op = "exists", actualValue = "test" }
            }
        });
        
        step.ValidateConfiguration(config);
        
        // Act
        var result = await step.ExecuteAsync(context);
        
        // Assert
        Assert.True(result.Success);
        Assert.Contains("this", context.Variables.Keys);
        Assert.Contains("my-assert-step", context.Variables.Keys);
        
        // Both should reference the same data
        Assert.Same(context.Variables["this"], context.Variables["my-assert-step"]);
    }

    [Fact]
    public void GetStepDescription_ReturnsCorrectDescription()
    {
        var step = new AssertStep();
        var config = JsonSerializer.SerializeToElement(new 
        { 
            assert = new object[]
            {
                new { op = "exists", actualValue = "test" }
            }
        });
        
        step.ValidateConfiguration(config);
        
        // Use reflection to access the protected method for testing
        var method = typeof(AssertStep).GetMethod("GetStepDescription", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var description = method?.Invoke(step, null) as string;
        
        Assert.Equal("Execute assertions", description);
    }

    [Fact]
    public void StepFactory_CreateStep_CanCreateAssertStep()
    {
        // Arrange
        var factory = new StepFactory();
        var stepConfig = new 
        { 
            type = "assert",
            assert = new object[]
            {
                new { op = "exists", actualValue = "test" }
            }
        };
        
        // Act
        var step = factory.CreateStep(stepConfig);
        
        // Assert
        Assert.IsType<AssertStep>(step);
        Assert.Equal("assert", step.Type);
    }

    [Fact]
    public void StepFactory_CreateStep_WithAssertStepAndId_SetsIdCorrectly()
    {
        // Arrange
        var factory = new StepFactory();
        var stepConfig = new 
        { 
            type = "assert",
            id = "test-assert",
            assert = new object[]
            {
                new { op = "exists", actualValue = "test" }
            }
        };
        
        // Act
        var step = factory.CreateStep(stepConfig);
        
        // Assert
        Assert.IsType<AssertStep>(step);
        Assert.Equal("test-assert", step.Id);
    }
}