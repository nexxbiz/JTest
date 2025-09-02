using System.Text.Json;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Utilities;

namespace JTest.UnitTests;

/// <summary>
/// Example demonstrating how to use case context and datasets functionality
/// This shows the complete workflow from the problem statement
/// </summary>
public class ExampleUsageTests
{
    [Fact]
    public async Task CompleteExample_OrderProcessingWithDatasets()
    {
        // This example demonstrates the exact JSON structure from the problem statement
        
        // 1. Define the test case with datasets (as it would come from JSON)
        var testCaseJson = """
        {
          "name": "Order processing",
          "flow": [
            {
              "type": "http",
              "id": "createOrder", 
              "method": "POST",
              "url": "{{$.env.baseUrl}}/orders",
              "body": "{{$.case.orderPayload}}",
              "assert": [
                { "op": "equals", "actualValue": "$.this.status", "expectedValue": 201 }
              ],
              "save": { "orderId": "$.this.body.id" }
            },
            {
              "type": "http",
              "id": "getOrder",
              "method": "GET", 
              "url": "{{$.env.baseUrl}}/accounts/{{$.case.accountId}}/orders/{{$.orderId}}",
              "assert": [
                { "op": "equals", "actualValue": "$.this.body.total", "expectedValue": "$.case.expectedTotal" }
              ]
            }
          ],
          "datasets": [
            {
              "name": "basic",
              "case": {
                "accountId": "acct-1001",
                "orderPayload": { "sku": "SKU-1", "qty": 2 },
                "expectedTotal": 20
              }
            },
            {
              "name": "discounted",
              "case": {
                "accountId": "acct-1002",
                "orderPayload": { "sku": "SKU-2", "qty": 3, "discountPct": 10 },
                "expectedTotal": 27
              }
            }
          ]
        }
        """;

        // 2. Parse the test case
        var testCase = JsonSerializer.Deserialize<JTestCase>(testCaseJson);
        Assert.NotNull(testCase);

        // 3. Set up the execution context with environment variables
        var baseContext = new TestExecutionContext();
        baseContext.Variables["env"] = new { baseUrl = "https://api.example.com" };

        // 4. Execute the test case with datasets
        var executor = new TestCaseExecutor();
        var results = await executor.ExecuteAsync(testCase, baseContext);

        // 5. Verify execution results
        Assert.Equal(2, results.Count); // One result per dataset

        // Verify basic dataset execution
        var basicResult = results[0];
        Assert.Equal("Order processing", basicResult.TestCaseName);
        Assert.Equal("basic", basicResult.Dataset!.Name);
        Assert.True(basicResult.Success);

        // Verify discounted dataset execution
        var discountedResult = results[1];
        Assert.Equal("Order processing", discountedResult.TestCaseName);
        Assert.Equal("discounted", discountedResult.Dataset!.Name);
        Assert.True(discountedResult.Success);

        // 6. Demonstrate case context variable resolution for each dataset
        await DemonstrateVariableResolution(basicResult.Dataset, baseContext);
        await DemonstrateVariableResolution(discountedResult.Dataset, baseContext);
    }

    private async Task DemonstrateVariableResolution(JTestDataset dataset, TestExecutionContext baseContext)
    {
        // Create execution context for this dataset
        var context = new TestExecutionContext();
        foreach (var kvp in baseContext.Variables)
        {
            context.Variables[kvp.Key] = kvp.Value;
        }
        context.SetCase(dataset.Case);

        // Simulate variable resolution as would happen in HTTP steps
        var createOrderUrl = VariableInterpolator.ResolveVariableTokens(
            "{{$.env.baseUrl}}/orders", context);
        
        var orderPayload = VariableInterpolator.ResolveVariableTokens(
            "{{$.case.orderPayload}}", context);
        
        var getOrderUrl = VariableInterpolator.ResolveVariableTokens(
            "{{$.env.baseUrl}}/accounts/{{$.case.accountId}}/orders/ORDER123", context);
        
        var expectedTotal = VariableInterpolator.ResolveVariableTokens(
            "{{$.case.expectedTotal}}", context);

        // Verify the resolutions work correctly
        Assert.Equal("https://api.example.com/orders", createOrderUrl);
        Assert.NotNull(orderPayload);
        Assert.Contains(dataset.Case["accountId"].ToString()!, getOrderUrl.ToString());
        
        // Compare the actual values, not the types
        var expectedTotalValue = dataset.Case["expectedTotal"];
        var resolvedTotalValue = expectedTotal;
        Assert.Equal(expectedTotalValue.ToString(), resolvedTotalValue.ToString());

        Console.WriteLine($"Dataset: {dataset.Name}");
        Console.WriteLine($"  Create Order URL: {createOrderUrl}");
        Console.WriteLine($"  Order Payload: {JsonSerializer.Serialize(orderPayload)}");
        Console.WriteLine($"  Get Order URL: {getOrderUrl}");
        Console.WriteLine($"  Expected Total: {expectedTotal}");
        Console.WriteLine();
    }

    [Fact]
    public void DemonstrateAllVariableScopes()
    {
        // This test shows how all variable scopes work together
        var context = new TestExecutionContext();

        // Environment variables (read-only configuration)
        context.Variables["env"] = new 
        { 
            baseUrl = "https://api.example.com",
            apiKey = "secret123"
        };

        // Global variables (shared across all tests)
        context.Variables["globals"] = new 
        { 
            authToken = "bearer-token-xyz",
            sessionId = "session-abc"
        };

        // Case variables (dataset-specific)
        context.SetCase(new Dictionary<string, object>
        {
            ["userId"] = "user-456",
            ["testData"] = new { name = "Test User", role = "admin" }
        });

        // Step context variables (step-specific)
        context.Variables["ctx"] = new { stepCount = 1, retryCount = 0 };

        // Current step response (this)
        context.Variables["this"] = new { status = 200, body = new { id = "created-123" } };

        // Demonstrate accessing all scopes
        var envUrl = VariableInterpolator.ResolveVariableTokens("{{$.env.baseUrl}}", context);
        var globalToken = VariableInterpolator.ResolveVariableTokens("{{$.globals.authToken}}", context);
        var caseUserId = VariableInterpolator.ResolveVariableTokens("{{$.case.userId}}", context);
        var ctxStep = VariableInterpolator.ResolveVariableTokens("{{$.ctx.stepCount}}", context);
        var thisStatus = VariableInterpolator.ResolveVariableTokens("{{$.this.status}}", context);

        // Build a complex URL using multiple scopes
        var complexUrl = VariableInterpolator.ResolveVariableTokens(
            "{{$.env.baseUrl}}/users/{{$.case.userId}}/session/{{$.globals.sessionId}}", context);

        Assert.Equal("https://api.example.com", envUrl);
        Assert.Equal("bearer-token-xyz", globalToken);
        Assert.Equal("user-456", caseUserId);
        Assert.Equal(1, ctxStep);
        Assert.Equal(200, thisStatus);
        Assert.Equal("https://api.example.com/users/user-456/session/session-abc", complexUrl);
    }
}