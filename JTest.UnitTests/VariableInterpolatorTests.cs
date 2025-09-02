using System.Globalization;
using System.Text.Json.Nodes;
using JTest.Core.Execution;
using JTest.Core.Utilities;

namespace JTest.UnitTests;

public class VariableInterpolatorTests
{

    [Fact]
    public void ResolveVariableTokens_WithNullInput_ReturnsEmptyString()
    {
        // Arrange
        var context = new TestExecutionContext();

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(null!, context);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ResolveVariableTokens_WithNoTokens_ReturnsOriginalString()
    {
        // Arrange
        var context = new TestExecutionContext();
        var input = "Hello World";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void ResolveVariableTokens_WithSingleToken_ReturnsRawValue()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["user"] = new { name = "John", age = 30 };
        var input = "{{$.user.name}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal("John", result);
    }

    [Fact]
    public void ResolveVariableTokens_WithSingleTokenComplexObject_ReturnsRawObject()
    {
        // Arrange
        var context = new TestExecutionContext();
        var userObject = new { name = "John", age = 30 };
        context.Variables["user"] = userObject;
        var input = "{{$.user}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.NotNull(result);
        // The result should be a JsonObject when complex objects are returned
        Assert.True(result is JsonObject || result.GetType().Name.Contains("Json"));
    }

    [Fact]
    public void ResolveVariableTokens_WithMultipleTokens_ReturnsInterpolatedString()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["user"] = new { name = "John", age = 30 };
        context.Variables["env"] = new { baseUrl = "https://api.example.com" };
        var input = "Hello {{$.user.name}}, visit {{$.env.baseUrl}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal("Hello John, visit https://api.example.com", result);
    }

    [Fact]
    public void ResolveVariableTokens_WithMissingPath_ReturnsEmptyStringAndLogsWarning()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["user"] = new { name = "John" };
        var input = "Hello {{$.user.nonexistent}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal("Hello ", result);
        Assert.Single(context.Log);
        Assert.Contains("Warning: JSONPath '$.user.nonexistent' not found in variables", context.Log[0]);
    }

    [Fact]
    public void ResolveVariableTokens_WithInvalidJsonPath_ReturnsEmptyStringAndLogsWarning()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["user"] = new { name = "John" };
        var input = "{{$.user.}}"; // Invalid JSONPath - ends with dot

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal(string.Empty, result);
        Assert.Single(context.Log);
        Assert.Contains("Warning: JSONPath '$.user.' not found in variables", context.Log[0]);
    }

    [Fact]
    public void ResolveVariableTokens_WithMixedTokensAndMissingPath_ContinuesProcessing()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["user"] = new { name = "John" };
        var input = "Hello {{$.user.name}} from {{$.missing.path}}!";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal("Hello John from !", result);
        Assert.Single(context.Log);
        Assert.Contains("Warning: JSONPath '$.missing.path' not found in variables", context.Log[0]);
    }

    [Fact]
    public void ResolveVariableTokens_WithTokensWithWhitespace_HandlesCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["user"] = new { name = "John" };
        var input = "{{ $.user.name }}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal("John", result);
    }

    [Fact]
    public void ResolveVariableTokens_WithNestedProperties_ReturnsCorrectValue()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["response"] = new 
        { 
            body = new { 
                data = new { 
                    id = 123,
                    attributes = new { name = "Test Item" }
                }
            }
        };
        var input = "{{$.response.body.data.attributes.name}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal("Test Item", result);
    }

    [Fact]
    public void ResolveVariableTokens_WithArrayAccess_ReturnsCorrectValue()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["items"] = new[] { "first", "second", "third" };
        var input = "{{$.items[1]}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal("second", result);
    }

    [Fact]
    public void ResolveVariableTokens_WithNumericValue_ReturnsStringRepresentation()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["config"] = new { port = 8080, timeout = 30.5 };
        var input = "Port: {{$.config.port}}, Timeout: {{$.config.timeout}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal("Port: 8080, Timeout: 30.5", result);
    }

    [Fact]
    public void ResolveVariableTokens_WithNumericValue_InDifferentCulture_ReturnsConsistentDecimalFormat()
    {
        // Save current culture
        var originalCulture = CultureInfo.CurrentCulture;
        
        try
        {
            // Set to a culture that uses comma as decimal separator
            CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // German culture uses comma
            
            // Arrange
            var context = new TestExecutionContext();
            context.Variables["config"] = new { port = 8080, timeout = 30.5 };
            var input = "Port: {{$.config.port}}, Timeout: {{$.config.timeout}}";

            // Act
            var result = VariableInterpolator.ResolveVariableTokens(input, context);

            // Assert - should always use dot as decimal separator regardless of culture
            Assert.Equal("Port: 8080, Timeout: 30.5", result);
        }
        finally
        {
            // Restore original culture
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}