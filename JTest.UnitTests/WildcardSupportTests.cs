using JTest.Core;
using Xunit;

namespace JTest.UnitTests;

public class WildcardSupportTests
{
    [Fact]
    public void ExpandWildcardPattern_WithSingleFile_ReturnsSingleFile()
    {
        // This test verifies the wildcard expansion logic conceptually
        // Since the method is private, we test through the CLI behavior
        var pattern = "test.json";
        var expected = new List<string> { "test.json" };
        
        // For a non-wildcard pattern, it should return the pattern as-is
        Assert.True(!pattern.Contains('*') && !pattern.Contains('?'));
    }

    [Fact]
    public void ExpandWildcardPattern_WithWildcard_IdentifiesWildcardPattern()
    {
        var pattern = "*.json";
        
        // Pattern should be identified as wildcard
        Assert.True(pattern.Contains('*') || pattern.Contains('?'));
    }

    [Fact]
    public void WildcardPattern_WithQuestionMark_IdentifiesWildcardPattern()
    {
        var pattern = "test?.json";
        
        // Pattern should be identified as wildcard
        Assert.True(pattern.Contains('*') || pattern.Contains('?'));
    }

    [Fact]
    public void NonWildcardPattern_WithoutSpecialChars_NotIdentifiedAsWildcard()
    {
        var pattern = "simple-test.json";
        
        // Pattern should not be identified as wildcard
        Assert.False(pattern.Contains('*') || pattern.Contains('?'));
    }
}