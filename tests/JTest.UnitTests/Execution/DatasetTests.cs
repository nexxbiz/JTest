using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.UnitTests.TestHelpers;
using System.Text.Json;

namespace JTest.UnitTests.Execution;

public class DatasetTests
{
    [Fact]
    public void JTestDataset_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var dataset = new JTestDataset
        {
            Name = "basic",
            Case = new Dictionary<string, object>
            {
                ["accountId"] = "acct-1001",
                ["expectedTotal"] = 20.0
            }
        };

        // Act
        var json = JsonSerializer.Serialize(dataset);
        var deserialized = JsonSerializer.Deserialize<JTestDataset>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("basic", deserialized.Name);
        Assert.Equal(2, deserialized.Case.Count);
        Assert.Contains("accountId", deserialized.Case.Keys);
        Assert.Contains("expectedTotal", deserialized.Case.Keys);
    }

    [Fact]
    public void JTestCase_ShouldSupportOptionalDatasets()
    {
        // Arrange & Act - Test case without datasets
        var testCaseWithoutDatasets = new JTestCase
        {
            Name = "Simple test",
            Steps = [new WaitStep(new(1))]
        };

        // Test case with datasets
        var testCaseWithDatasets = new JTestCase
        {
            Name = "Data-driven test",
            Steps = [new WaitStep(new(1))],
            Datasets =
            [
                new() { Name = "dataset1", Case = new Dictionary<string, object> { ["value"] = 1 } },
                new() { Name = "dataset2", Case = new Dictionary<string, object> { ["value"] = 2 } }
            ]
        };

        // Assert
        Assert.Null(testCaseWithoutDatasets.Datasets);
        Assert.NotNull(testCaseWithDatasets.Datasets);
        Assert.Equal(2, testCaseWithDatasets.Datasets.Count);
    }

    [Fact]
    public void JTestCaseResult_ShouldTrackDatasetExecution()
    {
        // Arrange
        var dataset = new JTestDataset
        {
            Name = "test-dataset",
            Case = new Dictionary<string, object> { ["userId"] = "123" }
        };

        // Act
        var result = new JTestCaseResult
        {
            TestCaseName = "Order processing",
            Dataset = dataset,
            DurationMs = 500
        };

        // Assert
        Assert.Equal("Order processing", result.TestCaseName);
        Assert.NotNull(result.Dataset);
        Assert.Equal("test-dataset", result.Dataset.Name);
        Assert.True(result.Success);
        Assert.Equal(500, result.DurationMs);
    }

    [Fact]
    public void JTestCase_ShouldSerializeFromExampleJson()
    {
        // Arrange - JSON from the problem statement
        var json = """
        {
          "name": "Order processing",
          "steps": [
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
            }
          ],
          "datasets": [
            {
              "name": "basic",
              "case": {
                "accountId": "acct-1001",
                "orderPayload": { "sku": "SKU-1", "qty": 2 },
                "expectedTotal": 20.0
              }
            }
          ]
        }
        """;

        // Act
        var testCase = JsonSerializer.Deserialize<JTestCase>(json, JsonSerializerHelper.Options);

        // Assert
        Assert.NotNull(testCase);
        Assert.Equal("Order processing", testCase.Name);
        Assert.Single(testCase.Steps);
        Assert.NotNull(testCase.Datasets);
        Assert.Single(testCase.Datasets);
        Assert.Equal("basic", testCase.Datasets[0].Name);
        Assert.Equal(3, testCase.Datasets[0].Case.Count);
    }
}