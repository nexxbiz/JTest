using System.Text.Json;
using JTest.Core;
using JTest.Core.Debugging;
using JTest.Core.Execution;
using Xunit;

namespace JTest.UnitTests;

public class TemplateIntegrationTests
{
    [Fact]
    public async Task TemplateStep_FullIntegration_WorksAsDocumented()
    {
        // Arrange
        var testRunner = new TestRunner();
        
        // Load template as described in documentation
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "authenticate",
                        "description": "Authenticate user and obtain access token",
                        "params": {
                            "username": { "type": "string", "required": true },
                            "password": { "type": "string", "required": true },
                            "tokenUrl": { "type": "string", "required": true }
                        },
                        "steps": [],
                        "output": {
                            "token": "{{$.username}}-{{$.password}}-token",
                            "authHeader": "Bearer {{$.username}}-{{$.password}}-token"
                        }
                    }
                ]
            }
        }
        """;
        
        testRunner.LoadTemplates(templateJson);
        
        // Create test that uses the template
        var testJson = """
        {
            "name": "Test with authentication template",
            "flow": [
                {
                    "type": "use",
                    "template": "authenticate",
                    "with": {
                        "username": "{{$.env.username}}",
                        "password": "{{$.env.password}}",
                        "tokenUrl": "{{$.env.tokenUrl}}"
                    }
                }
            ]
        }
        """;
        
        var environment = new Dictionary<string, object>
        {
            ["username"] = "testuser",
            ["password"] = "testpass",
            ["tokenUrl"] = "https://api.example.com/token"
        };

        // Act
        var results = await testRunner.RunTestAsync(testJson, environment);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.True(result.Success);
        Assert.Single(result.StepResults);
        
        var stepResult = result.StepResults[0];
        Assert.True(stepResult.Success);
        Assert.NotNull(stepResult.Data);
    }

    [Fact]
    public async Task TemplateStep_WithDebugLogger_LogsTemplateStepInformation()
    {
        // Arrange
        var testRunner = new TestRunner();
        var debugLogger = new MarkdownDebugLogger();
        
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "debug-test",
                        "params": {
                            "message": { "type": "string", "required": true }
                        },
                        "steps": [],
                        "output": {
                            "response": "Debug: {{$.message}}"
                        }
                    }
                ]
            }
        }
        """;
        
        testRunner.LoadTemplates(templateJson);
        
        var testJson = """
        {
            "name": "Debug logger test",
            "flow": [
                {
                    "type": "use",
                    "id": "debug-step",
                    "template": "debug-test",
                    "with": {
                        "message": "hello world"
                    }
                }
            ]
        }
        """;

        // Act
        var results = await testRunner.RunTestAsync(testJson, environment: null, globals: null, debugLogger);
        var debugOutput = debugLogger.GetOutput();

        // Assert
        Assert.Single(results);
        Assert.True(results[0].Success);
        
        // Debug output should contain template step information
        Assert.Contains("UseStep", debugOutput);
        Assert.Contains("debug-step", debugOutput);
        Assert.Contains("✅ Success", debugOutput);
        
        // Verify template outputs are properly logged in context
        Assert.Contains("\"response\": \"Debug: hello world\"", debugOutput);
    }

    [Fact]
    public async Task TemplateStep_WithRequiredParameterMissing_FailsExecution()
    {
        // Arrange
        var testRunner = new TestRunner();
        
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "required-param-test",
                        "params": {
                            "requiredParam": { "type": "string", "required": true }
                        },
                        "steps": [],
                        "output": {}
                    }
                ]
            }
        }
        """;
        
        testRunner.LoadTemplates(templateJson);
        
        var testJson = """
        {
            "name": "Required parameter test",
            "flow": [
                {
                    "type": "use",
                    "template": "required-param-test",
                    "with": {}
                }
            ]
        }
        """;

        // Act
        var results = await testRunner.RunTestAsync(testJson);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.False(result.Success);
        Assert.Contains("Required template parameter 'requiredParam' not provided", result.ErrorMessage);
    }
}