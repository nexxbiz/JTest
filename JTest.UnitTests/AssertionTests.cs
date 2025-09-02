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
        Assert.Contains("Expected '43' but got '42'", result.ErrorMessage);
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
        Assert.Equal("Value does not exist or is null/empty", result.ErrorMessage);
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
                    "op": "lessthan",
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
        Assert.Contains("Unknown assertion operation: 'unknown-operation'", results[0].ErrorMessage);
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

    // New tests for all assertion operations
    
    [Fact]
    public void NotEqualsAssertion_WithDifferentValues_ReturnsTrue()
    {
        // Arrange
        var assertion = new NotEqualsAssertion();

        // Act
        var result = assertion.Execute(42, 43);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void NotEqualsAssertion_WithSameValues_ReturnsFalse()
    {
        // Arrange
        var assertion = new NotEqualsAssertion();

        // Act
        var result = assertion.Execute(42, 42);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected '42' to not equal '42'", result.ErrorMessage);
    }

    [Fact]
    public void NotExistsAssertion_WithNullValue_ReturnsTrue()
    {
        // Arrange
        var assertion = new NotExistsAssertion();

        // Act
        var result = assertion.Execute(null, null);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void NotExistsAssertion_WithNonNullValue_ReturnsFalse()
    {
        // Arrange
        var assertion = new NotExistsAssertion();

        // Act
        var result = assertion.Execute("test", null);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected value to not exist, but it does", result.ErrorMessage);
    }

    [Fact]
    public void ContainsAssertion_WithMatchingSubstring_ReturnsTrue()
    {
        // Arrange
        var assertion = new ContainsAssertion();

        // Act
        var result = assertion.Execute("Hello World", "World");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void ContainsAssertion_WithNonMatchingSubstring_ReturnsFalse()
    {
        // Arrange
        var assertion = new ContainsAssertion();

        // Act
        var result = assertion.Execute("Hello World", "xyz");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected 'Hello World' to contain 'xyz'", result.ErrorMessage);
    }

    [Fact]
    public void NotContainsAssertion_WithNonMatchingSubstring_ReturnsTrue()
    {
        // Arrange
        var assertion = new NotContainsAssertion();

        // Act
        var result = assertion.Execute("Hello World", "xyz");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void NotContainsAssertion_WithMatchingSubstring_ReturnsFalse()
    {
        // Arrange
        var assertion = new NotContainsAssertion();

        // Act
        var result = assertion.Execute("Hello World", "World");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected 'Hello World' to not contain 'World'", result.ErrorMessage);
    }

    [Fact]
    public void StartsWithAssertion_WithMatchingPrefix_ReturnsTrue()
    {
        // Arrange
        var assertion = new StartsWithAssertion();

        // Act
        var result = assertion.Execute("Hello World", "Hello");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void StartsWithAssertion_WithNonMatchingPrefix_ReturnsFalse()
    {
        // Arrange
        var assertion = new StartsWithAssertion();

        // Act
        var result = assertion.Execute("Hello World", "World");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected 'Hello World' to start with 'World'", result.ErrorMessage);
    }

    [Fact]
    public void EndsWithAssertion_WithMatchingSuffix_ReturnsTrue()
    {
        // Arrange
        var assertion = new EndsWithAssertion();

        // Act
        var result = assertion.Execute("Hello World", "World");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void EndsWithAssertion_WithNonMatchingSuffix_ReturnsFalse()
    {
        // Arrange
        var assertion = new EndsWithAssertion();

        // Act
        var result = assertion.Execute("Hello World", "Hello");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected 'Hello World' to end with 'Hello'", result.ErrorMessage);
    }

    [Fact]
    public void MatchesAssertion_WithValidRegex_ReturnsTrue()
    {
        // Arrange
        var assertion = new MatchesAssertion();

        // Act
        var result = assertion.Execute("test123", @"\d+");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void MatchesAssertion_WithNonMatchingRegex_ReturnsFalse()
    {
        // Arrange
        var assertion = new MatchesAssertion();

        // Act
        var result = assertion.Execute("test", @"\d+");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected 'test' to match pattern", result.ErrorMessage);
    }

    [Fact]
    public void MatchesAssertion_WithInvalidRegex_ReturnsFalse()
    {
        // Arrange
        var assertion = new MatchesAssertion();

        // Act
        var result = assertion.Execute("test", "[invalid");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid regex pattern", result.ErrorMessage);
    }

    [Fact]
    public void GreaterOrEqualAssertion_WithGreaterValue_ReturnsTrue()
    {
        // Arrange
        var assertion = new GreaterOrEqualAssertion();

        // Act
        var result = assertion.Execute(10, 5);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void GreaterOrEqualAssertion_WithEqualValue_ReturnsTrue()
    {
        // Arrange
        var assertion = new GreaterOrEqualAssertion();

        // Act
        var result = assertion.Execute(5, 5);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void GreaterOrEqualAssertion_WithLessValue_ReturnsFalse()
    {
        // Arrange
        var assertion = new GreaterOrEqualAssertion();

        // Act
        var result = assertion.Execute(5, 10);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected 5 to be greater than or equal to 10", result.ErrorMessage);
    }

    [Fact]
    public void LessOrEqualAssertion_WithLessValue_ReturnsTrue()
    {
        // Arrange
        var assertion = new LessOrEqualAssertion();

        // Act
        var result = assertion.Execute(5, 10);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessOrEqualAssertion_WithEqualValue_ReturnsTrue()
    {
        // Arrange
        var assertion = new LessOrEqualAssertion();

        // Act
        var result = assertion.Execute(5, 5);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessOrEqualAssertion_WithGreaterValue_ReturnsFalse()
    {
        // Arrange
        var assertion = new LessOrEqualAssertion();

        // Act
        var result = assertion.Execute(10, 5);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected 10 to be less than or equal to 5", result.ErrorMessage);
    }

    [Fact]
    public void BetweenAssertion_WithValueInRange_ReturnsTrue()
    {
        // Arrange
        var assertion = new BetweenAssertion();
        var range = JsonSerializer.Deserialize<JsonElement>("[5, 15]");

        // Act
        var result = assertion.Execute(10, range);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void BetweenAssertion_WithValueOutOfRange_ReturnsFalse()
    {
        // Arrange
        var assertion = new BetweenAssertion();
        var range = JsonSerializer.Deserialize<JsonElement>("[5, 15]");

        // Act
        var result = assertion.Execute(20, range);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected 20 to be between 5 and 15", result.ErrorMessage);
    }

    [Fact]
    public void BetweenAssertion_WithInvalidRange_ReturnsFalse()
    {
        // Arrange
        var assertion = new BetweenAssertion();

        // Act
        var result = assertion.Execute(10, "not-an-array");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Between assertion requires an array", result.ErrorMessage);
    }

    [Fact]
    public void LengthAssertion_WithStringOfCorrectLength_ReturnsTrue()
    {
        // Arrange
        var assertion = new LengthAssertion();

        // Act
        var result = assertion.Execute("Hello", 5);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LengthAssertion_WithStringOfIncorrectLength_ReturnsFalse()
    {
        // Arrange
        var assertion = new LengthAssertion();

        // Act
        var result = assertion.Execute("Hello", 3);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected length 3 but got 5", result.ErrorMessage);
    }

    [Fact]
    public void LengthAssertion_WithArray_ReturnsCorrectLength()
    {
        // Arrange
        var assertion = new LengthAssertion();
        var array = new[] { "a", "b", "c" };

        // Act
        var result = assertion.Execute(array, 3);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void EmptyAssertion_WithEmptyString_ReturnsTrue()
    {
        // Arrange
        var assertion = new EmptyAssertion();

        // Act
        var result = assertion.Execute("", null);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void EmptyAssertion_WithNonEmptyString_ReturnsFalse()
    {
        // Arrange
        var assertion = new EmptyAssertion();

        // Act
        var result = assertion.Execute("Hello", null);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected value to be empty but it has 5", result.ErrorMessage);
    }

    [Fact]
    public void NotEmptyAssertion_WithNonEmptyString_ReturnsTrue()
    {
        // Arrange
        var assertion = new NotEmptyAssertion();

        // Act
        var result = assertion.Execute("Hello", null);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void NotEmptyAssertion_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var assertion = new NotEmptyAssertion();

        // Act
        var result = assertion.Execute("", null);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected value to not be empty but it is", result.ErrorMessage);
    }

    [Fact]
    public void InAssertion_WithValueInArray_ReturnsTrue()
    {
        // Arrange
        var assertion = new InAssertion();
        var array = new[] { "apple", "banana", "orange" };

        // Act
        var result = assertion.Execute("banana", array);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void InAssertion_WithValueNotInArray_ReturnsFalse()
    {
        // Arrange
        var assertion = new InAssertion();
        var array = new[] { "apple", "banana", "orange" };

        // Act
        var result = assertion.Execute("grape", array);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected 'grape' to be in [apple, banana, orange]", result.ErrorMessage);
    }

    [Fact]
    public void InAssertion_WithJsonArray_ReturnsTrue()
    {
        // Arrange
        var assertion = new InAssertion();
        var jsonArray = JsonSerializer.Deserialize<JsonElement>(@"[""apple"", ""banana"", ""orange""]");

        // Act
        var result = assertion.Execute("banana", jsonArray);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void TypeAssertion_WithCorrectType_ReturnsTrue()
    {
        // Arrange
        var assertion = new TypeAssertion();

        // Act
        var result = assertion.Execute("Hello", "string");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void TypeAssertion_WithIncorrectType_ReturnsFalse()
    {
        // Arrange
        var assertion = new TypeAssertion();

        // Act
        var result = assertion.Execute(42, "string");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected type 'string' but got 'integer'", result.ErrorMessage);
    }

    [Fact]
    public void TypeAssertion_WithJsonElement_ReturnsCorrectType()
    {
        // Arrange
        var assertion = new TypeAssertion();
        var jsonNumber = JsonSerializer.Deserialize<JsonElement>("42");

        // Act
        var result = assertion.Execute(jsonNumber, "integer");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void AssertionProcessor_WithSimilarOperationName_ProvidesSuggestion()
    {
        // Arrange
        var context = new TestExecutionContext();
        
        var assertionJson = """
        [
            {
                "op": "equal",
                "actualValue": "test",
                "expectedValue": "test"
            }
        ]
        """;
        
        var assertionsElement = JsonSerializer.Deserialize<JsonElement>(assertionJson);

        // Act
        var results = AssertionProcessor.ProcessAssertions(assertionsElement, context);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].Success);
        Assert.Contains("Did you mean:", results[0].ErrorMessage);
        Assert.Contains("'equals'", results[0].ErrorMessage);
    }
}