using JTest.Core;
using System.Text.Json;

namespace JTest.UnitTests;

public class TemplateFromSampleFileTests
{
    [Fact]
    public async Task TemplateStep_WithSampleElsaTemplateStructure_ExecutesSuccessfully()
    {
        // Arrange
        var testRunner = new TestRunner();

        // Load a template that mimics the structure of the sample file but without HTTP steps
        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "authenticate",
                        "description": "Authenticate user and obtain access token",
                        "params": {
                            "username": {
                                "type": "string",
                                "required": true,
                                "description": "Username for authentication"
                            },
                            "password": {
                                "type": "string",
                                "required": true,
                                "description": "Password for authentication"
                            },
                            "tokenUrl": {
                                "type": "string",
                                "required": true,
                                "description": "Url to get token"
                            }
                        },
                        "steps": [],
                        "output": {
                            "token": "{{$.username}}-token",
                            "authHeader": "Bearer {{$.username}}-token"
                        }
                    }
                ]
            }
        }
        """;

        testRunner.LoadTemplates(templateJson);

        // Create a test that uses the authenticate template from the sample
        var testJson = """
        {
            "name": "Test authenticate template from sample",
            "steps": [
                {
                    "type": "use",
                    "template": "authenticate",
                    "with": {
                        "username": "testuser",
                        "password": "testpass",
                        "tokenUrl": "https://api.example.com/token"
                    }
                }
            ]
        }
        """;

        // Act
        var results = await testRunner.RunTestAsync(JsonDocument.Parse(testJson).RootElement);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.True(result.Success);
        Assert.Single(result.StepResults);

        var stepResult = result.StepResults[0];
        Assert.True(stepResult.Success);
    }
}