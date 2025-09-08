using JTest.Core.Execution;
using JTest.Core.Utilities;
using System.Globalization;
using System.Text.Json.Nodes;

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
        // The result should be a Dictionary when complex objects are returned (converted for token resolution)
        Assert.True(result is Dictionary<string, object> || result is JsonObject || result.GetType().Name.Contains("Json"));

        // Verify the content is accessible
        if (result is Dictionary<string, object> dict)
        {
            Assert.Equal("John", dict["name"]);
            Assert.Equal(30, dict["age"]);
        }
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
            body = new
            {
                data = new
                {
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

    [Fact]
    public void ResolveVariableTokens_WithCaseData_ShouldReplaceAllTokens()
    {
        // Arrange - reproduce the issue from the problem statement
        var context = new TestExecutionContext();
        var caseData = new Dictionary<string, object>
        {
            ["isTrue"] = false,
            ["expectedCondition"] = false,
            ["testScenario"] = "false condition path"
        };
        context.SetCase(caseData);

        // This is the problematic input from the problem statement
        var input = "{{$.case.isTrue}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert - should resolve to the actual value, not remain as token
        Assert.Equal(false, result);
        Assert.NotEqual("{{$.case.isTrue}}", result);
    }

    [Fact]
    public void ResolveVariableTokens_InComplexObject_ShouldReplaceAllTokens()
    {
        // Arrange - test with more complex scenario
        var context = new TestExecutionContext();
        var caseData = new Dictionary<string, object>
        {
            ["isTrue"] = false,
            ["expectedCondition"] = false,
            ["testScenario"] = "false condition path"
        };
        context.SetCase(caseData);

        // Test a template string that might appear in JSON output
        var input = "Request: { \"isTrue\": \"{{$.case.isTrue}}\" }";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal("Request: { \"isTrue\": \"False\" }", result);
        Assert.DoesNotContain("{{$.case.isTrue}}", result.ToString());
    }

    [Fact]
    public void ResolveVariableTokens_WithNestedComplexPaths_ShouldReplaceCorrectly()
    {
        // Arrange - test case that might reveal the issue
        var context = new TestExecutionContext();
        context.Variables["workflowResponse"] = new Dictionary<string, object>
        {
            ["request"] = new Dictionary<string, object>
            {
                ["isTrue"] = "{{$.case.isTrue}}"
            }
        };
        var caseData = new Dictionary<string, object>
        {
            ["isTrue"] = false
        };
        context.SetCase(caseData);

        // This simulates a complex nested scenario like in the problem statement
        var input = "{{$.workflowResponse.request.isTrue}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert - should resolve completely, not remain as nested token
        Assert.Equal(false, result); // With the fix, this should resolve to the actual value
        Assert.NotEqual("{{$.case.isTrue}}", result); // Should not be the intermediate unresolved token
    }

    [Fact]
    public void ResolveVariableTokens_WithDoubleNesting_ShouldResolveCompletely()
    {
        // Arrange - test to verify the exact scenario from problem statement
        var context = new TestExecutionContext();
        var caseData = new Dictionary<string, object>
        {
            ["isTrue"] = false
        };
        context.SetCase(caseData);

        // Simulate the exact scenario - where a token contains another token that needs resolution
        // This is the bug: when a resolved value contains another token, it should be resolved further
        context.Variables["nested"] = new Dictionary<string, object>
        {
            ["template"] = "{{$.case.isTrue}}"
        };

        var complexInput = "{{$.nested.template}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(complexInput, context);

        // Assert
        // This should resolve completely to "False", not remain as "{{$.case.isTrue}}"
        Assert.Equal(false, result); // This test should fail with current implementation
        Assert.NotEqual("{{$.case.isTrue}}", result); // This is the bug - it currently returns this
    }

    [Fact]
    public void ResolveVariableTokens_WithCircularReference_ShouldPreventInfiniteLoop()
    {
        // Arrange - test infinite recursion protection
        var context = new TestExecutionContext();
        context.Variables["a"] = "{{$.b}}";
        context.Variables["b"] = "{{$.a}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens("{{$.a}}", context);

        // Assert - should not crash and should log warning
        Assert.NotNull(result);
        Assert.True(context.Log.Any(log => log.Contains("Maximum token resolution depth")));
    }

    [Fact]
    public void SetCase_WithTokensInCaseData_ShouldResolveTokensAutomatically()
    {
        // Arrange - This test demonstrates the issue FransVanEk pointed out
        var context = new TestExecutionContext();
        context.Variables["env"] = new { baseUrl = "https://api.test.com" };

        // Case data contains tokens that reference other variables
        var caseData = new Dictionary<string, object>
        {
            ["endpoint"] = "{{$.env.baseUrl}}/users", // This token should be resolved when case is set
            ["userId"] = "123",
            ["nested"] = new Dictionary<string, object>
            {
                ["apiUrl"] = "{{$.env.baseUrl}}/api"
            }
        };

        // Act - Currently this just sets the case data without resolving tokens
        context.SetCase(caseData);

        // Try to access the endpoint
        var resolvedEndpoint = VariableInterpolator.ResolveVariableTokens("{{$.case.endpoint}}", context);
        var resolvedNestedUrl = VariableInterpolator.ResolveVariableTokens("{{$.case.nested.apiUrl}}", context);

        // Assert - This currently fails because tokens in case data are not resolved
        // The resolved endpoint should be the fully resolved URL, not the token
        Assert.Equal("https://api.test.com/users", resolvedEndpoint);
        Assert.Equal("https://api.test.com/api", resolvedNestedUrl);

        // Verify the case data itself was modified to contain resolved values
        var caseContext = context.Variables["case"] as Dictionary<string, object>;
        Assert.Equal("https://api.test.com/users", caseContext!["endpoint"]);

        var nestedContext = caseContext["nested"] as Dictionary<string, object>;
        Assert.Equal("https://api.test.com/api", nestedContext!["apiUrl"]);
    }

    [Fact]
    public void ResolveVariableTokens_WithComplexObjectContainingTokens_ShouldResolveNestedTokens()
    {
        // Arrange - This test validates the exact issue reported in the problem statement
        var context = new TestExecutionContext();

        // Set up case data
        var caseData = new Dictionary<string, object>
        {
            ["isTrue"] = false
        };
        context.SetCase(caseData);

        // Set up workflowbody that contains tokens (this is the issue scenario)
        context.Variables["workflowbody"] = new Dictionary<string, object>
        {
            ["isTrue"] = "{{$.case.isTrue}}"
        };

        // Act - Resolve the workflowbody token
        var result = VariableInterpolator.ResolveVariableTokens("{{$.workflowbody}}", context);

        // Assert - The returned object should have tokens resolved
        Assert.NotNull(result);
        Assert.True(result is Dictionary<string, object>);

        var resultDict = result as Dictionary<string, object>;
        Assert.Equal(false, resultDict!["isTrue"]); // Should be resolved to false, not "{{$.case.isTrue}}"

        // Verify that the JSON representation doesn't contain unresolved tokens
        var resultJson = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.DoesNotContain("{{$.case.isTrue}}", resultJson);
        Assert.Contains("false", resultJson);
    }

    [Fact]
    public void ResolveVariableTokens_WithSingleJsonPathMatch_ReturnsScalarValue()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["items"] = new[]
        {
            new { id = "1", name = "first", type = "A" },
            new { id = "2", name = "second", type = "B" },
            new { id = "3", name = "third", type = "A" }
        };
        var input = "{{$.items[0].id}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.Equal("1", result);
    }

    [Fact]
    public void ResolveVariableTokens_WithMultipleJsonPathMatches_ReturnsArray()
    {
        // Arrange
        var context = new TestExecutionContext();
        context.Variables["items"] = new[]
        {
            new { id = "1", name = "first", type = "A" },
            new { id = "2", name = "second", type = "B" },
            new { id = "3", name = "third", type = "A" }
        };
        var input = "{{$.items[?(@.type=='A')].id}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.IsType<object[]>(result);
        var resultArray = (object[])result;
        Assert.Equal(2, resultArray.Length);
        Assert.Equal("1", resultArray[0]);
        Assert.Equal("3", resultArray[1]);
    }

    [Fact]
    public void ResolveVariableTokens_WithComplexFilterExpression_ReturnsMatchingItems()
    {
        // Arrange - This test simulates the exact scenario from the problem statement
        var context = new TestExecutionContext();
        context.Variables["testResult"] = new
        {
            journal = new
            {
                items = new[]
                {
                    new { activityId = "88e113194fd0c4d5", eventName = "Started", id = "event1" },
                    new { activityId = "88e113194fd0c4d5", eventName = "Completed", id = "event2" },
                    new { activityId = "88e113194fd0c4d5", eventName = "Completed", id = "event3" },
                    new { activityId = "other", eventName = "Completed", id = "event4" }
                }
            }
        };
        var input = "{{$.testResult.journal.items[?(@.activityId=='88e113194fd0c4d5' && @.eventName == 'Completed')].id}}";

        // Act
        var result = VariableInterpolator.ResolveVariableTokens(input, context);

        // Assert
        Assert.IsType<object[]>(result);
        var resultArray = (object[])result;
        Assert.Equal(2, resultArray.Length);
        Assert.Equal("event2", resultArray[0]);
        Assert.Equal("event3", resultArray[1]);
    }

    [Fact]
    public void ResolveVariableTokens_MultipleMatchesWithLengthAssertion_ShouldReturnCorrectCount()
    {
        // Arrange - This test verifies that the length assertion works with multiple matches
        var context = new TestExecutionContext();
        context.Variables["testResult"] = new
        {
            journal = new
            {
                items = new[]
                {
                    new { activityId = "88e113194fd0c4d5", eventName = "Completed", id = "event1" },
                    new { activityId = "88e113194fd0c4d5", eventName = "Completed", id = "event2" },
                    new { activityId = "other", eventName = "Completed", id = "event3" }
                }
            }
        };
        context.Variables["case"] = new { expectedIterations = 2 };

        // Act - Get the actual value that would be used in a length assertion
        var actualValue = VariableInterpolator.ResolveVariableTokens("{{$.testResult.journal.items[?(@.activityId=='88e113194fd0c4d5' && @.eventName == 'Completed')].id}}", context);
        var expectedValue = VariableInterpolator.ResolveVariableTokens("{{$.case.expectedIterations}}", context);

        // Assert - Verify we can get the length of the result
        Assert.IsType<object[]>(actualValue);
        var resultArray = (object[])actualValue;
        Assert.Equal(2, resultArray.Length);
        Assert.Equal(2, expectedValue);

        // Verify that a length assertion would work correctly
        var lengthAssertion = new JTest.Core.Assertions.LengthAssertion();
        var assertionResult = lengthAssertion.Execute(actualValue, expectedValue);
        Assert.True(assertionResult.Success);
    }
}