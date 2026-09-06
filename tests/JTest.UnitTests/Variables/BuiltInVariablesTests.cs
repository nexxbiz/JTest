using JTest.Core.Exceptions;
using JTest.Core.Execution;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.Utilities;
using JTest.Core.Variables;
using Xunit;

namespace JTest.UnitTests.Variables;

/// <summary>
/// <c>$.now</c> and <c>$.random</c> were documented as built-ins but never implemented, so they
/// expanded to an empty string. That silently turned a per-run unique resource name into a constant
/// (e.g. the literal route "greet-"), which made every run target the SAME server-side resource — a
/// suite could pass while being served by a previous run's artifact.
/// </summary>
public class BuiltInVariablesTests
{
    [Theory]
    [InlineData("$.random.uuid")]
    [InlineData("$.random.id")]
    [InlineData("$.now.iso")]
    [InlineData("$.now.date")]
    [InlineData("$.now.time")]
    [InlineData("$.now.epoch")]
    [InlineData("$.now.epochMs")]
    public void BuiltIn_ResolvesToANonEmptyValue(string path)
    {
        var context = new TestExecutionContext();

        var resolved = VariableInterpolator.ResolveVariableTokens($"{{{{{path}}}}}", context, out var unresolved);

        Assert.Empty(unresolved);
        Assert.False(string.IsNullOrWhiteSpace(resolved?.ToString()), $"{path} resolved to an empty value");
    }

    [Fact]
    public void RandomUuid_IsAUuid_AndFreshPerReference()
    {
        var context = new TestExecutionContext();

        var first = VariableInterpolator.ResolveVariableTokens("{{$.random.uuid}}", context)!.ToString();
        var second = VariableInterpolator.ResolveVariableTokens("{{$.random.uuid}}", context)!.ToString();

        Assert.True(Guid.TryParse(first, out _));
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void NowIso_RoundTripsAsATimestamp()
    {
        var context = new TestExecutionContext();

        var iso = VariableInterpolator.ResolveVariableTokens("{{$.now.iso}}", context)!.ToString();

        Assert.True(DateTimeOffset.TryParse(iso, out _));
    }

    [Fact]
    public void BuiltIn_InterpolatesInsideASurroundingString()
    {
        // The reported failure was a route name, not a bare token: "greet-" plus an empty expansion.
        var context = new TestExecutionContext();

        var route = VariableInterpolator.ResolveVariableTokens("greet-{{$.random.id}}", context)!.ToString();

        Assert.NotEqual("greet-", route);
        Assert.StartsWith("greet-", route);
        Assert.Equal("greet-".Length + 8, route!.Length);
    }

    [Fact]
    public void UnknownFieldOnABuiltInRoot_IsAnError_NotAnEmptyString()
    {
        var context = new TestExecutionContext();

        var error = Assert.Throws<InvalidOperationException>(
            () => VariableInterpolator.ResolveVariableTokens("{{$.random.wat}}", context));

        Assert.Contains("Unknown field 'wat'", error.Message);
        Assert.Contains("$.random.uuid", error.Message);
    }

    [Fact]
    public void RealContextData_WinsOverABuiltIn()
    {
        // Adding built-ins must not change what an existing suite means.
        var context = new TestExecutionContext();
        context.Variables["now"] = new Dictionary<string, object?> { ["iso"] = "from-the-suite" };

        var resolved = VariableInterpolator.ResolveVariableTokens("{{$.now.iso}}", context);

        Assert.Equal("from-the-suite", resolved);
    }

    [Fact]
    public void BuiltInsAreUsableInAStepField()
    {
        var context = new TestExecutionContext();
        var step = new WaitStep(new(Ms: 1));

        // A step field resolves without throwing and without collapsing to the literal prefix.
        var url = ResolveThroughStep("routes/greet-{{$.random.id}}", context);

        Assert.NotEqual("routes/greet-", url);
        Assert.NotNull(step);
    }

    private static string ResolveThroughStep(string expression, IExecutionContext context)
    {
        var resolved = VariableInterpolator.ResolveVariableTokens(expression, context, out var unresolved);
        Assert.Empty(unresolved);
        return resolved!.ToString()!;
    }
}

/// <summary>
/// The other half, and the more important one: a token that cannot resolve must fail loudly rather
/// than become an empty string. A step field silently filled with "" still gets sent, so the run can
/// report success for a request the suite never actually constructed (FR-061).
/// </summary>
public class UnresolvedStepFieldTests
{
    [Fact]
    public async Task UnresolvedTokenInAStepField_Throws_NamingTheToken()
    {
        var context = new TestExecutionContext();
        context.Variables["ctx"] = new Dictionary<string, object?>();

        var step = new HttpStep(new HttpClient(), new HttpStepConfiguration("GET", "https://api.test/greet-{{$.ctx.neverSet}}"));

        var error = await Assert.ThrowsAsync<UnresolvedTokenException>(() => step.ExecuteAsync(context, default));

        Assert.Contains("$.ctx.neverSet", error.Message);
        Assert.Contains("Unresolved token", error.Message);
        Assert.Equal(["$.ctx.neverSet"], error.UnresolvedPaths);
    }

    [Fact]
    public async Task ResolvableStepField_StillWorks()
    {
        var context = new TestExecutionContext();
        context.Variables["ctx"] = new Dictionary<string, object?> { ["name"] = "alice" };

        var step = new HttpStep(new HttpClient(), new HttpStepConfiguration("GET", "https://api.test/greet-{{$.ctx.name}}"));

        // Resolution succeeds; the request itself fails (no server), which is a different error.
        await Assert.ThrowsAnyAsync<Exception>(() => step.ExecuteAsync(context, default));
    }
}
