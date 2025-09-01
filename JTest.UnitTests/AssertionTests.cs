using System.Globalization;
using System.Text.Json;
using JTest.Core.Assertions;
using JTest.Core.Execution;

namespace JTest.UnitTests;

public class AssertionTests
{
    private class TestExecutionContext : IExecutionContext
    {
        public Dictionary<string, object> Variables { get; } = new();
        public IList<string> Log { get; } = new List<string>();
    }

    [Fact]
    public void EqualsAssertion_WithIntegerValues_ReturnsTrue()
    {
        // Arrange
        var assertion = new EqualsAssertion();

        // Act
        var result = assertion.Execute(42, 42);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void EqualsAssertion_WithDifferentIntegerValues_ReturnsFalse()
    {
        // Arrange
        var assertion = new EqualsAssertion();

        // Act
        var result = assertion.Execute(42, 43);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected '43' but got '42'", result.Message);
    }

    [Fact]
    public void EqualsAssertion_WithDoubleValues_InDifferentCultures_ReturnsConsistentResult()
    {
        // Save current culture
        var originalCulture = CultureInfo.CurrentCulture;
        
        try
        {
            // Test with English culture (uses dot)
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            var assertion = new EqualsAssertion();
            var result1 = assertion.Execute(30.5, 30.5);
            
            // Test with German culture (uses comma)
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var result2 = assertion.Execute(30.5, 30.5);
            
            // Both should succeed regardless of culture
            Assert.True(result1.Success);
            Assert.True(result2.Success);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void EqualsAssertion_WithStringRepresentationOfNumbers_InDifferentCultures_HandlesCorrectly()
    {
        // Save current culture
        var originalCulture = CultureInfo.CurrentCulture;
        
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var assertion = new EqualsAssertion();
            
            // Both values should be compared using invariant culture formatting
            var result = assertion.Execute(30.5, "30.5");
            
            Assert.True(result.Success);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void GreaterThanAssertion_WithNumericValues_InDifferentCultures_WorksCorrectly()
    {
        // Save current culture
        var originalCulture = CultureInfo.CurrentCulture;
        
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var assertion = new GreaterThanAssertion();
            
            // Act
            var result = assertion.Execute(30.5, 20.3);
            
            // Assert
            Assert.True(result.Success);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void LessThanAssertion_WithNumericValues_InDifferentCultures_WorksCorrectly()
    {
        // Save current culture
        var originalCulture = CultureInfo.CurrentCulture;
        
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var assertion = new LessThanAssertion();
            
            // Act
            var result = assertion.Execute(20.3, 30.5);
            
            // Assert
            Assert.True(result.Success);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void ExistsAssertion_WithNonNullValue_ReturnsTrue()
    {
        // Arrange
        var assertion = new ExistsAssertion();

        // Act
        var result = assertion.Execute("test", null);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void ExistsAssertion_WithNullValue_ReturnsFalse()
    {
        // Arrange
        var assertion = new ExistsAssertion();

        // Act
        var result = assertion.Execute(null, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Value does not exist or is null/empty", result.Message);
    }

    [Fact]
    public void AssertionProcessor_WithEqualsAssertion_ProcessesCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["response"] = new { status = 200 };
        
        var assertionJson = """
        [
            {
                "op": "equals",
                "actualValue": "{{$.response.status}}",
                "expectedValue": 200
            }
        ]
        """;
        
        var assertionsElement = JsonSerializer.Deserialize<JsonElement>(assertionJson);

        // Act
        var results = AssertionProcessor.ProcessAssertions(assertionsElement, context);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].Success);
    }

    [Fact]
    public void AssertionProcessor_WithNumericComparison_InDifferentCulture_WorksCorrectly()
    {
        // Save current culture
        var originalCulture = CultureInfo.CurrentCulture;
        
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            
            // Arrange
            var context = new TestExecutionContext();
            context.Variables["response"] = new { duration = 30.5 };
            
            var assertionJson = """
            [
                {
                    "op": "less-than",
                    "actualValue": "{{$.response.duration}}",
                    "expectedValue": 60.0
                }
            ]
            """;
            
            var assertionsElement = JsonSerializer.Deserialize<JsonElement>(assertionJson);

            // Act
            var results = AssertionProcessor.ProcessAssertions(assertionsElement, context);

            // Assert
            Assert.Single(results);
            Assert.True(results[0].Success);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void AssertionProcessor_WithUnknownOperation_ReturnsFailure()
    {
        // Arrange
        var context = new TestExecutionContext();
        
        var assertionJson = """
        [
            {
                "op": "unknown-operation",
                "actualValue": "test"
            }
        ]
        """;
        
        var assertionsElement = JsonSerializer.Deserialize<JsonElement>(assertionJson);

        // Act
        var results = AssertionProcessor.ProcessAssertions(assertionsElement, context);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].Success);
        Assert.Contains("Unknown assertion operation: unknown-operation", results[0].Message);
    }

    [Fact]
    public void AssertionRegistry_RegistersCustomOperation()
    {
        // Arrange
        var registry = new AssertionRegistry();
        var customOperation = new TestCustomAssertion();

        // Act
        registry.Register(customOperation);
        var retrievedOperation = registry.GetOperation("test-custom");

        // Assert
        Assert.NotNull(retrievedOperation);
        Assert.Equal("test-custom", retrievedOperation.OperationType);
    }

    private class TestCustomAssertion : IAssertionOperation
    {
        public string OperationType => "test-custom";

        public AssertionResult Execute(object? actualValue, object? expectedValue)
        {
            return new AssertionResult(true);
        }
    }
}