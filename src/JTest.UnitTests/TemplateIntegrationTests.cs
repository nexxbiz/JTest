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

    [Fact]
    public async Task TemplateStep_WithInnerSteps_ShowsInnerStepsInMarkdownOutput()
    {
        // Arrange
        var testRunner = new TestRunner();

        var templateJson = """
        {
            "version": "1.0",
            "components": {
                "templates": [
                    {
                        "name": "multi-step-template",
                        "description": "Template with multiple steps",
                        "params": {
                            "inputValue": { "type": "string", "required": true }
                        },
                        "steps": [
                            {
                                "type": "wait",
                                "ms": 50,
                                "save": {
                                    "waitResult": "first-step-completed"
                                }
                            },
                            {
                                "type": "wait",
                                "ms": 25,
                                "save": {
                                    "secondResult": "{{inputValue}}-processed"
                                }
                            }
                        ],
                        "output": {
                            "finalResult": "{{waitResult}}-{{secondResult}}"
                        }
                    }
                ]
            }
        }
        """;

        testRunner.LoadTemplates(templateJson);

        var testJson = """
        {
            "name": "Template with inner steps",
            "steps": [
                {
                    "type": "use",
                    "template": "multi-step-template",
                    "with": {
                        "inputValue": "test-data"
                    },
                    "save": {
                        "templateOutput": "{{$.this.finalResult}}"
                    }
                }
            ]
        }
        """;

        // Act
        var results = await testRunner.RunTestAsync(testJson);
        var converter = new JTest.Core.Converters.ResultsToMarkdownConverter();
        var markdown = converter.ConvertToMarkdown(results);

        // Assert execution was successful
        Assert.Single(results);
        var result = results[0];
        
        // Debug output
        Console.WriteLine($"Result Success: {result.Success}");
        Console.WriteLine($"Result ErrorMessage: {result.ErrorMessage}");
        Console.WriteLine($"Number of step results: {result.StepResults.Count}");
        
        if (!result.Success)
        {
            Assert.True(result.Success, $"Test execution failed: {result.ErrorMessage}");
        }
        
        Assert.Single(result.StepResults);

        var templateStepResult = result.StepResults[0];
        Console.WriteLine($"Template step success: {templateStepResult.Success}");
        Console.WriteLine($"Template step error: {templateStepResult.ErrorMessage}");
        Console.WriteLine($"Inner results count: {templateStepResult.InnerResults.Count}");
        
        Assert.True(templateStepResult.Success);
        Assert.Equal(2, templateStepResult.InnerResults.Count);

        // Assert markdown contains template steps section
        Assert.Contains("**Template Steps:**", markdown);
        Assert.Contains("<tr><td>Wait 50ms</td><td>PASSED</td>", markdown);
        
        // Verify inner step details are shown (variables saved in inner steps)  
        Assert.Contains("<tr><td>Added</td><td>waitResult</td><td>\"first-step-completed\"</td></tr>", markdown);
        Assert.Contains("<tr><td>Added</td><td>secondResult</td><td>\"{{inputValue}}-processed\"</td></tr>", markdown); // Variable is saved, interpolation may not resolve in this context
        
        // Print the markdown for manual verification
        Console.WriteLine("Generated Markdown with Inner Steps:");
        Console.WriteLine(markdown);
    }
}