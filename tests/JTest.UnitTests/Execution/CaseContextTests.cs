using JTest.Core.Execution;
using JTest.Core.Utilities;

namespace JTest.UnitTests.Execution;

public sealed class CaseContextTests
{
    [Fact]
    public void SetCase_ShouldAddCaseVariablesToContext()
    {
        // Arrange
        var context = new TestExecutionContext();
        var caseData = new Dictionary<string, object>
        {
            ["userId"] = "user123",
            ["accountId"] = "acct-1001",
            ["expectedTotal"] = 20.0
        };

        // Act
        context.SetCase(caseData);

        // Assert
        Assert.Contains("case", context.Variables.Keys);
        var caseContext = context.Variables["case"] as Dictionary<string, object>;
        Assert.NotNull(caseContext);
        Assert.Equal("user123", caseContext["userId"]);
        Assert.Equal("acct-1001", caseContext["accountId"]);
        Assert.Equal(20.0, caseContext["expectedTotal"]);
    }

    [Fact]
    public void ClearCase_ShouldSetCaseToEmptyDictionary()
    {
        // Arrange
        var context = new TestExecutionContext();
        var caseData = new Dictionary<string, object> { ["userId"] = "user123" };
        context.SetCase(caseData);

        // Act
        context.ClearCase();

        // Assert
        Assert.Contains("case", context.Variables.Keys);
        var caseContext = context.Variables["case"] as Dictionary<string, object>;
        Assert.NotNull(caseContext);
        Assert.Empty(caseContext);
    }

    [Fact]
    public void VariableInterpolator_ShouldResolveCaseVariables()
    {
        // Arrange
        var context = new TestExecutionContext();
        var caseData = new Dictionary<string, object>
        {
            ["userId"] = "user456",
            ["orderPayload"] = new { sku = "SKU-1", qty = 2 }
        };
        context.SetCase(caseData);

        // Act
        var resolvedUserId = VariableInterpolator.ResolveVariableTokens("{{$.case.userId}}", context);
        var resolvedTemplate = VariableInterpolator.ResolveVariableTokens("/users/{{$.case.userId}}/orders", context);

        // Assert
        Assert.Equal("user456", resolvedUserId);
        Assert.Equal("/users/user456/orders", resolvedTemplate);
    }

    [Fact]
    public void VariableInterpolator_ShouldResolveComplexCaseObjects()
    {
        // Arrange
        var context = new TestExecutionContext();
        var caseData = new Dictionary<string, object>
        {
            ["orderPayload"] = new { sku = "SKU-1", qty = 2, discountPct = 10 }
        };
        context.SetCase(caseData);

        // Act - This should return the complex object
        var resolvedPayload = VariableInterpolator.ResolveVariableTokens("{{$.case.orderPayload}}", context);

        // Assert
        Assert.NotNull(resolvedPayload);
        // Should be a complex object, not a string
        Assert.IsNotType<string>(resolvedPayload);
    }
}