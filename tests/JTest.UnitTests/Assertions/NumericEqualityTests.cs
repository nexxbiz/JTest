using System.Text.Json;
using JTest.Core.Assertions;
using JTest.Core.Execution;

namespace JTest.UnitTests.Assertions;

/// <summary>
/// Numeric equality where the values come from JSON — which is the normal case: the actual is read
/// out of an HTTP response body and the expected is written in the suite file, so both arrive as
/// <see cref="JsonElement"/>. <c>equals</c> advertises numeric handling, but a JSON number was not
/// recognised as numeric and the comparison silently fell through to comparing the two values as
/// TEXT. "25" and "25.0" are the same number and different strings, so a correct assertion failed.
/// </summary>
public class NumericEqualityTests
{
    /// <summary>A value as it reaches an assertion from a response body or a suite definition.</summary>
    private static object? Json(string literal) => JsonDocument.Parse(literal).RootElement;

    /// <summary>Runs the assertion the way a suite does, through the execution context.</summary>
    private static bool IsEqual(object? actual, object? expected) =>
        new EqualsAssertion(actual, expected).Execute(new TestExecutionContext()).Success;

    private static bool IsNotEqual(object? actual, object? expected) =>
        new NotEqualsAssertion(actual, expected).Execute(new TestExecutionContext()).Success;

    [Theory]
    // The case that started this: a price of 2 x 12.50 read back from an API.
    [InlineData("25", "25.0")]
    [InlineData("25.0", "25")]
    [InlineData("25.50", "25.5")]
    [InlineData("0", "0.0")]
    [InlineData("-3", "-3.00")]
    [InlineData("1000", "1e3")]
    [InlineData("0.5", "5e-1")]
    public void JsonNumbers_ThatAreTheSameNumber_AreEqual(string actual, string expected)
    {
        Assert.True(IsEqual(Json(actual), Json(expected)),
            $"{actual} and {expected} are the same number and must compare equal.");
        Assert.False(IsNotEqual(Json(actual), Json(expected)));
    }

    [Theory]
    [InlineData("25", "26")]
    [InlineData("25.5", "25.6")]
    [InlineData("0", "-1")]
    [InlineData("1000", "1e4")]
    public void JsonNumbers_ThatDiffer_AreNotEqual(string actual, string expected)
    {
        Assert.False(IsEqual(Json(actual), Json(expected)));
        Assert.True(IsNotEqual(Json(actual), Json(expected)));
    }

    [Fact]
    public void LargeIntegers_BeyondDoublePrecision_StayDistinct()
    {
        // 2^53 and 2^53+1 are the same double. Comparing as doubles would call these equal and turn
        // a real difference — an id, a cent amount, a counter — into a passing assertion.
        Assert.False(IsEqual(Json("9007199254740993"), Json("9007199254740992")));
        Assert.True(IsEqual(Json("9007199254740993"), Json("9007199254740993")));
    }

    [Fact]
    public void DoublesThatDifferOnlyInBinaryFloatingPoint_StayDistinct()
    {
        // 0.1 + 0.2 is 0.30000000000000004, not 0.3. Routing doubles through decimal would round
        // both to 0.3 at 15 significant digits and call them equal — quietly passing an assertion
        // about a value that is genuinely different. Doubles therefore skip the decimal path.
        Assert.False(IsEqual(0.1d + 0.2d, 0.3d));
        Assert.True(IsEqual(0.1d + 0.2d, 0.1d + 0.2d));
        Assert.True(IsEqual(0.3d, 0.3d));

        // Same when the comparison is against a JSON literal.
        Assert.False(IsEqual(0.1d + 0.2d, Json("0.3")));
    }

    [Fact]
    public void VeryLargeNumbers_OutsideDecimalRange_StillCompare()
    {
        // Beyond decimal's range the comparison falls back to double, which is the best available.
        Assert.True(IsEqual(Json("1e300"), Json("1e300")));
        Assert.False(IsEqual(Json("1e300"), Json("1e301")));
    }

    [Fact]
    public void ClrNumbersAndJsonNumbers_Interoperate()
    {
        // A value saved into the context is a CLR number; one from a body is a JsonElement. Mixing
        // the two is routine and must not depend on which side came from where.
        Assert.True(IsEqual(25, Json("25.0")));
        Assert.True(IsEqual(Json("25.0"), 25));
        Assert.True(IsEqual(25.0d, Json("25")));
        Assert.True(IsEqual(Json("25"), 25.0d));
        Assert.False(IsEqual(25, Json("26")));
    }

    [Fact]
    public void StringsAreStillComparedAsText_NotCoercedToNumbers()
    {
        // Only number-to-number comparison changed. A string keeps comparing as text, so a suite
        // asserting on an id like "007" is not quietly turned into a numeric 7.
        Assert.True(IsEqual(Json("\"25\""), Json("\"25\"")));
        Assert.False(IsEqual(Json("\"25.0\""), Json("\"25\"")));
        Assert.False(IsEqual(Json("\"007\""), Json("7")));

        // A string against a number compares by text — documented in language-reference.md, so it
        // is pinned here rather than left as prose.
        Assert.True(IsEqual(Json("\"25\""), Json("25")));
        Assert.False(IsEqual(Json("\"25\""), Json("25.0")));
    }

    [Fact]
    public void NullAndBooleanComparisons_AreUnaffected()
    {
        Assert.True(IsEqual(null, null));
        Assert.False(IsEqual(null, Json("0")));
        Assert.False(IsEqual(Json("0"), null));
        Assert.True(IsEqual(Json("true"), Json("true")));
        Assert.False(IsEqual(Json("true"), Json("false")));

        // A boolean is not a number: false must not equal 0.
        Assert.False(IsEqual(Json("false"), Json("0")));
    }
}
