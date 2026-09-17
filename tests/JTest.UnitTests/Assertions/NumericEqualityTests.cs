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
    private static bool Equals(object? actual, object? expected) =>
        new EqualsAssertion(actual, expected).Execute(new TestExecutionContext()).Success;

    private static bool NotEquals(object? actual, object? expected) =>
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
        Assert.True(Equals(Json(actual), Json(expected)),
            $"{actual} and {expected} are the same number and must compare equal.");
        Assert.False(NotEquals(Json(actual), Json(expected)));
    }

    [Theory]
    [InlineData("25", "26")]
    [InlineData("25.5", "25.6")]
    [InlineData("0", "-1")]
    [InlineData("1000", "1e4")]
    public void JsonNumbers_ThatDiffer_AreNotEqual(string actual, string expected)
    {
        Assert.False(Equals(Json(actual), Json(expected)));
        Assert.True(NotEquals(Json(actual), Json(expected)));
    }

    [Fact]
    public void LargeIntegers_BeyondDoublePrecision_StayDistinct()
    {
        // 2^53 and 2^53+1 are the same double. Comparing as doubles would call these equal and turn
        // a real difference — an id, a cent amount, a counter — into a passing assertion.
        Assert.False(Equals(Json("9007199254740993"), Json("9007199254740992")));
        Assert.True(Equals(Json("9007199254740993"), Json("9007199254740993")));
    }

    [Fact]
    public void VeryLargeNumbers_OutsideDecimalRange_StillCompare()
    {
        // Beyond decimal's range the comparison falls back to double, which is the best available.
        Assert.True(Equals(Json("1e300"), Json("1e300")));
        Assert.False(Equals(Json("1e300"), Json("1e301")));
    }

    [Fact]
    public void ClrNumbersAndJsonNumbers_Interoperate()
    {
        // A value saved into the context is a CLR number; one from a body is a JsonElement. Mixing
        // the two is routine and must not depend on which side came from where.
        Assert.True(Equals(25, Json("25.0")));
        Assert.True(Equals(Json("25.0"), 25));
        Assert.True(Equals(25.0d, Json("25")));
        Assert.True(Equals(Json("25"), 25.0d));
        Assert.False(Equals(25, Json("26")));
    }

    [Fact]
    public void StringsAreStillComparedAsText_NotCoercedToNumbers()
    {
        // Only number-to-number comparison changed. A string keeps comparing as text, so a suite
        // asserting on an id like "007" is not quietly turned into a numeric 7.
        Assert.True(Equals(Json("\"25\""), Json("\"25\"")));
        Assert.False(Equals(Json("\"25.0\""), Json("\"25\"")));
        Assert.False(Equals(Json("\"007\""), Json("7")));
    }

    [Fact]
    public void NullAndBooleanComparisons_AreUnaffected()
    {
        Assert.True(Equals(null, null));
        Assert.False(Equals(null, Json("0")));
        Assert.False(Equals(Json("0"), null));
        Assert.True(Equals(Json("true"), Json("true")));
        Assert.False(Equals(Json("true"), Json("false")));

        // A boolean is not a number: false must not equal 0.
        Assert.False(Equals(Json("false"), Json("0")));
    }
}
