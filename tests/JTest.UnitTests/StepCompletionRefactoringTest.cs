using JTest.Core.Execution;
using JTest.Core.Steps;
using System.Text.Json;

namespace JTest.UnitTests;

/// <summary>
/// Test to verify that the refactored step completion pattern works consistently across all step types.
/// This test validates that the common ProcessStepCompletionAsync method in BaseStep correctly handles
/// assertion processing, result creation, and error handling for both HttpStep and WaitStep.
/// </summary>
public class StepCompletionRefactoringTest
{
    [Fact]
    public async Task AllStepTypes_WithFailingAssertions_HandleAssertionsConsistently()
    {
        // Arrange - Create test context with assertion data that will fail
        var context = new TestExecutionContext();
        context.Variables["expectedValue"] = "success";
        context.Variables["actualValue"] = "failure"; // This will cause assertion to fail

        // Create assertion configuration that should fail
        var assertionConfig = JsonDocument.Parse("""
        [
            {
                "actual": "{{ $.actualValue }}",
                "equals": "{{ $.expectedValue }}"
            }
        ]
        """).RootElement;

        // Test that all step types handle failed assertions consistently using the refactored pattern
        await TestStepWithFailingAssertions_HttpStep(context, assertionConfig);
        await TestStepWithFailingAssertions_WaitStep(context, assertionConfig);
    }

    [Fact]
    public async Task StepTypes_UseCommonCompletionPattern_ConsistentBehavior()
    {
        // This test verifies that the refactored ProcessStepCompletionAsync method produces
        // consistent behavior across different step types by testing the basic execution flow.

        var context = new TestExecutionContext();

        // Test HttpStep basic execution (no assertions)
        var httpClient = new HttpClient(new TestHttpMessageHandler());
        var httpConfig = JsonDocument.Parse("""
        {
            "method": "GET",
            "url": "https://test.com"
        }
        """).RootElement;
        var httpStep = new HttpStep(httpClient, httpConfig);
        var httpResult = await httpStep.ExecuteAsync(context);

        // Verify consistent step result structure
        Assert.True(httpResult.Success);
        Assert.NotNull(httpResult.Data);
        Assert.NotNull(httpResult.AssertionResults);
        Assert.Empty(httpResult.AssertionResults); // No assertions configured

        // Test WaitStep basic execution (no assertions) 
        var waitConfig = JsonDocument.Parse("""
        {
            "ms": 1
        }
        """).RootElement;
        var waitStep = new WaitStep(waitConfig);
        var waitResult = await waitStep.ExecuteAsync(context);

        // Verify consistent step result structure
        Assert.True(waitResult.Success);
        Assert.NotNull(waitResult.Data);
        Assert.NotNull(waitResult.AssertionResults);
        Assert.Empty(waitResult.AssertionResults); // No assertions configured
    }

    private async Task TestStepWithFailingAssertions_HttpStep(TestExecutionContext context, JsonElement assertionConfig)
    {
        var httpClient = new HttpClient(new TestHttpMessageHandler());

        var config = JsonDocument.Parse($$"""
        {
            "method": "GET",
            "url": "https://test.com",
            "assert": {{assertionConfig.GetRawText()}}
        }
        """).RootElement;

        var httpStep = new HttpStep(httpClient, config);

        var result = await httpStep.ExecuteAsync(context);

        // Verify common step completion behavior for failed assertions
        Assert.False(result.Success); // Step should fail due to assertion failure
        Assert.Equal("One or more assertions failed", result.ErrorMessage);
        Assert.NotNull(result.AssertionResults);
        Assert.Single(result.AssertionResults);
        Assert.False(result.AssertionResults[0].Success); // Assertion should fail
        Assert.NotNull(result.Data);
    }

    private async Task TestStepWithFailingAssertions_WaitStep(TestExecutionContext context, JsonElement assertionConfig)
    {

        var config = JsonDocument.Parse($$"""
        {
            "ms": 1,
            "assert": {{assertionConfig.GetRawText()}}
        }
        """).RootElement;
        var waitStep = new WaitStep(config);

        var result = await waitStep.ExecuteAsync(context);

        // Verify common step completion behavior for failed assertions
        Assert.False(result.Success); // Step should fail due to assertion failure
        Assert.Equal("One or more assertions failed", result.ErrorMessage);
        Assert.NotNull(result.AssertionResults);
        Assert.Single(result.AssertionResults);
        Assert.False(result.AssertionResults[0].Success); // Assertion should fail
        Assert.NotNull(result.Data);
    }
}

// Test helper class
public class TestHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("""{"status": "success"}""", System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}