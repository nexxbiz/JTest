using JTest.Core;

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
            "steps": [
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
            "steps": [
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