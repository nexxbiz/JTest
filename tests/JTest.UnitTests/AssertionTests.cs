using JTest.Core.Assertions;
using JTest.Core.Execution;
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace JTest.UnitTests;

public class AssertionTests
{

    [Fact]
    public void EqualsAssertion_WithIntegerValues_ReturnsTrue()
    {
        // Arrange
        var assertion = new EqualsAssertion(42, 42);
        var context = new TestExecutionContext();

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void EqualsAssertion_WithDifferentIntegerValues_ReturnsFalse()
    {
        // Arrange
        var assertion = new EqualsAssertion(42, 43);
        var context = new TestExecutionContext();

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Condition failed: 42 == 43", result.ErrorMessage);
    }

    [Fact]
    public void EqualsAssertion_WithDoubleValues_InDifferentCultures_ReturnsConsistentResult()
    {
        // Save current culture
        var originalCulture = CultureInfo.CurrentCulture;
        var context = new TestExecutionContext();

        try
        {
            // Test with English culture (uses dot)
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            var assertion = new EqualsAssertion(30.5, 30.5);
            var result1 = assertion.Execute(context);

            // Test with German culture (uses comma)
            assertion = new EqualsAssertion(30.5, 30.5);
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var result2 = assertion.Execute(context);

            // Both should succeed regardless of culture
            Assert.True(result1.Success);
            Assert.True(result2.Success);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void EqualsAssertion_WithStringRepresentationOfNumbers_InDifferentCultures_HandlesCorrectly()
    {
        // Save current culture
        var originalCulture = CultureInfo.CurrentCulture;
        var context = new TestExecutionContext();
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var assertion = new EqualsAssertion(30.5, "30.5");

            // Both values should be compared using invariant culture formatting
            var result = assertion.Execute(context);

            Assert.True(result.Success);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void GreaterThanAssertion_WithNumericValues_InDifferentCultures_WorksCorrectly()
    {
        // Save current culture
        var originalCulture = CultureInfo.CurrentCulture;
        var context = new TestExecutionContext();
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var assertion = new GreaterThanAssertion(30.5, 20.3);

            // Act
            var result = assertion.Execute(context);

            // Assert
            Assert.True(result.Success);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void GreaterThanAssertion_WithDateTimeValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var expected = new DateTime(2020, 1, 1, 12, 34, 55).ToString(CultureInfo.InvariantCulture);
        var actual = new DateTime(2020, 1, 1, 12, 35, 0).ToString(CultureInfo.InvariantCulture);
        var assertion = new GreaterThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void GreaterThanAssertion_WithDateTimeOffsetValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new DateTimeOffset(2020, 1, 1, 11, 0, 0, TimeSpan.Zero)
            .ToString(CultureInfo.InvariantCulture);
        // This seems like the later time, but there is an offset of 1 hour
        var expected = new DateTimeOffset(2020, 1, 1, 11, 55, 0, TimeSpan.FromHours(1))
            .ToString(CultureInfo.InvariantCulture);
        var assertion = new GreaterThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void GreaterThanAssertion_WithDateOnlyValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new DateOnly(2020, 1, 2).ToString(CultureInfo.InvariantCulture);
        var expected = new DateOnly(2020, 1, 1).ToString(CultureInfo.InvariantCulture);
        var assertion = new GreaterThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void GreaterThanAssertion_WithTimeOnlyValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new TimeOnly(1, 2, 1).ToString();
        var expected = new TimeOnly(1, 1, 1).ToString();
        var assertion = new GreaterThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessThanAssertion_WithDateTimeValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new DateTime(2020, 1, 1, 12, 34, 55).ToString(CultureInfo.InvariantCulture);
        var expected = new DateTime(2020, 1, 1, 12, 35, 0).ToString(CultureInfo.InvariantCulture);
        var assertion = new LessThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessThanAssertion_WithDateOnlyValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new DateOnly(2020, 1, 1).ToString(CultureInfo.InvariantCulture);
        var expected = new DateOnly(2020, 1, 2).ToString(CultureInfo.InvariantCulture);
        var assertion = new LessThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessThanAssertion_WithTimeOnlyValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new TimeOnly(1, 1, 1).ToString("hh:mm:ss");
        var expected = new TimeOnly(1, 1, 2).ToString("hh:mm:ss");
        var assertion = new LessThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void StronglyTypedDateTimeAssertion_WithDateOnlyValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new DateOnly(2020, 1, 1);
        var expected = new DateOnly(2020, 1, 2);
        var assertion = new LessThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void StronglyTypedDateTimeAssertion_WithDateTimeValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new DateTime(2020, 1, 1, 1, 1, 1);
        var expected = new DateTime(2020, 1, 1, 1, 1, 2);
        var assertion = new LessThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void StronglyTypedDateTimeAssertion_WithDateTimeOffsetValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new DateTimeOffset(2020, 1, 1, 1, 1, 1, TimeSpan.FromHours(1));
        var expected = new DateTimeOffset(2020, 1, 1, 1, 1, 2, TimeSpan.Zero);
        var assertion = new LessThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void StronglyTypedDateTimeAssertion_WithTimeOnlyValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new TimeOnly(1, 1, 1, 1);
        var expected = new TimeOnly(1, 1, 1, 2);
        var assertion = new LessThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessThanAssertion_WithDateTimeOffsetValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        // This seems like the later time, but there is an offset of 1 hour
        var actual = new DateTimeOffset(2020, 1, 1, 11, 55, 0, TimeSpan.FromHours(1))
            .ToString(CultureInfo.InvariantCulture);
        var expected = new DateTimeOffset(2020, 1, 1, 11, 0, 0, TimeSpan.Zero)
            .ToString(CultureInfo.InvariantCulture);
        var assertion = new LessThanAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessThanOrEqualsAssertion_WithDateTimeValues_And_ActualLessThanExpected_Then_Succeeds()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new DateTime(2020, 1, 1, 12, 34, 55).ToString(CultureInfo.InvariantCulture);
        var expected = new DateTime(2020, 1, 1, 12, 35, 0).ToString(CultureInfo.InvariantCulture);
        var assertion = new LessOrEqualAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessThanOrEqualsAssertion_WithDateTimeOffsetValues_And_ActualLessThanExpected_Then_Succeeds()
    {
        // Arrange
        var context = new TestExecutionContext();
        // This seems like the later time, but there is an offset of 1 hour
        var actual = new DateTimeOffset(2020, 1, 1, 11, 55, 0, TimeSpan.FromHours(1))
            .ToString(CultureInfo.InvariantCulture);
        var expected = new DateTimeOffset(2020, 1, 1, 11, 0, 0, TimeSpan.Zero)
            .ToString(CultureInfo.InvariantCulture);
        var assertion = new LessOrEqualAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessThanOrEqualsAssertion_WithDateTimeValues_And_ActualEqualsExpected_Then_Succeeds()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new DateTime(2020, 1, 1, 12, 35, 0).ToString(CultureInfo.InvariantCulture);
        var expected = new DateTime(2020, 1, 1, 12, 35, 0).ToString(CultureInfo.InvariantCulture);
        var assertion = new LessOrEqualAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessThanOrEqualsAssertion_WithDateTimeOffsetValues_And_ActualEqualsExpected_Then_Succeeds()
    {
        // Arrange
        var context = new TestExecutionContext();
        // This seems like a later time, but there is an offset of 1 hour
        var earlier = new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(1))
            .ToString(CultureInfo.InvariantCulture);
        var later = new DateTimeOffset(2020, 1, 1, 11, 0, 0, TimeSpan.Zero)
            .ToString(CultureInfo.InvariantCulture);
        var assertion = new LessOrEqualAssertion(earlier, later);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessThanAssertion_WithNumericValues_InDifferentCultures_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();        
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var assertion = new LessThanAssertion(20.3, 30.5);

            // Act
            var result = assertion.Execute(context);

            // Assert
            Assert.True(result.Success);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void ExistsAssertion_WithNonNullValue_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new ExistsAssertion("test");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void ExistsAssertion_WithNullValue_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new ExistsAssertion(null);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Value does not exist or is null/empty", result.ErrorMessage);
    }

    [Fact]
    public void NotEqualsAssertion_WithDifferentValues_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new NotEqualsAssertion(42, 43);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void NotEqualsAssertion_WithSameValues_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new NotEqualsAssertion(42, 42);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Condition failed: 42 != 42", result.ErrorMessage);
    }

    [Fact]
    public void NotExistsAssertion_WithNullValue_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new NotExistsAssertion(null);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void NotExistsAssertion_WithNonNullValue_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new NotExistsAssertion("test");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Expected value {assertion.ActualValue} to not exist.", result.ErrorMessage);
    }

    [Fact]
    public void ContainsAssertion_WithMatchingSubstring_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new ContainsAssertion("Hello World", "World");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void ContainsAssertion_WithNonMatchingSubstring_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new ContainsAssertion("Hello World", "xyz");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Expected '{assertion.ActualValue}' to contain '{assertion.ExpectedValue}'", result.ErrorMessage);
    }

    [Fact]
    public void NotContainsAssertion_WithNonMatchingSubstring_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new NotContainsAssertion("Hello World", "xyz");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void NotContainsAssertion_WithMatchingSubstring_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new NotContainsAssertion("Hello World", "World");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Expected '{assertion.ActualValue}' to not contain '{assertion.ExpectedValue}'", result.ErrorMessage);
    }

    [Fact]
    public void StartsWithAssertion_WithMatchingPrefix_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new StartsWithAssertion("Hello World", "Hello");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void StartsWithAssertion_WithNonMatchingPrefix_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new StartsWithAssertion("Hello World", "World");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Expected '{assertion.ActualValue}' to start with '{assertion.ExpectedValue}'", result.ErrorMessage);
    }

    [Fact]
    public void EndsWithAssertion_WithMatchingSuffix_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new EndsWithAssertion("Hello World", "World");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void EndsWithAssertion_WithNonMatchingSuffix_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new EndsWithAssertion("Hello World", "Hello");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Expected '{assertion.ActualValue}' to end with '{assertion.ExpectedValue}'", result.ErrorMessage);
    }

    [Fact]
    public void MatchesAssertion_WithValidRegex_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new MatchAssertion("test123", @"\d+");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void MatchesAssertion_WithNonMatchingRegex_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new MatchAssertion("test", @"\d+");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Expected '{assertion.ActualValue}' to match pattern '{assertion.ExpectedValue}'", result.ErrorMessage);
    }


    [Fact]
    public void GreaterOrEqualAssertion_WithGreaterValue_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new GreaterOrEqualAssertion(10, 5);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void GreaterOrEqualAssertion_WithEqualValue_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new GreaterOrEqualAssertion(5, 5);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void GreaterOrEqualAssertion_WithLessValue_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new GreaterOrEqualAssertion(5, 10);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Condition failed: {assertion.ActualValue} >= {assertion.ExpectedValue}", result.ErrorMessage);
    }

    [Fact]
    public void GreaterOrEqualAssertion_WithDateOnlyValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new DateOnly(2020, 1, 2).ToString(CultureInfo.InvariantCulture);
        var expected = new DateOnly(2020, 1, 1).ToString(CultureInfo.InvariantCulture);
        var assertion = new GreaterOrEqualAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void GreaterOrEqualAssertion_WithTimeOnlyValues_WorksCorrectly()
    {
        // Arrange
        var context = new TestExecutionContext();
        var actual = new TimeOnly(1, 1, 2).ToString(CultureInfo.InvariantCulture);
        var expected = new TimeOnly(1, 1, 1).ToString(CultureInfo.InvariantCulture);
        var assertion = new GreaterOrEqualAssertion(actual, expected);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }


    [Fact]
    public void LessOrEqualAssertion_WithLessValue_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new LessOrEqualAssertion(5, 10);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessOrEqualAssertion_WithEqualValue_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new LessOrEqualAssertion(5, 5);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LessOrEqualAssertion_WithGreaterValue_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new LessOrEqualAssertion(10,5);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Condition failed: {assertion.ActualValue} <= {assertion.ExpectedValue}", result.ErrorMessage);
    }

    [Fact]
    public void BetweenAssertion_WithValueInRange_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var range = JsonSerializer.Deserialize<JsonElement>("[5, 15]");
        var assertion = new BetweenAssertion(10, range);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void BetweenAssertion_WithValueOutOfRange_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var range = JsonSerializer.Deserialize<JsonElement>("[5, 15]");
        var assertion = new BetweenAssertion(20, range);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected 20 to be between 5 and 15", result.ErrorMessage);
    }

    [Fact]
    public void BetweenAssertion_WithInvalidRange_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new BetweenAssertion(10, "not-an-array");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Operator 'between' requires an array of [min, max] values as expectedValue", result.ErrorMessage);
    }

    [Fact]
    public void LengthAssertion_WithStringOfCorrectLength_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new LengthAssertion("Hello", 5);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void LengthAssertion_WithStringOfIncorrectLength_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new LengthAssertion("Hello", 3);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected length 3 but got 5", result.ErrorMessage);
    }

    [Fact]
    public void LengthAssertion_WithArray_ReturnsCorrectLength()
    {
        // Arrange
        var context = new TestExecutionContext();
        var array = new[] { "a", "b", "c" };
        var assertion = new LengthAssertion(array, 3);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void EmptyAssertion_WithEmptyString_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new EmptyAssertion(string.Empty);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void EmptyAssertion_WithNonEmptyString_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new EmptyAssertion("Hello");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected value to be empty but it has 5 items/characters", result.ErrorMessage);
    }

    [Fact]
    public void NotEmptyAssertion_WithNonEmptyString_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new NotEmptyAssertion("Hello");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void NotEmptyAssertion_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new NotEmptyAssertion(string.Empty);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected value to not be empty, but it is.", result.ErrorMessage);
    }

    [Fact]
    public void InAssertion_WithValueInArray_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var array = new[] { "apple", "banana", "orange" };
        var assertion = new InAssertion("banana", array);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void InAssertion_WithValueNotInArray_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var array = new[] { "apple", "banana", "orange" };
        var assertion = new InAssertion("grape", array);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Expected '{assertion.ActualValue}' to be in {assertion.ExpectedValue}", result.ErrorMessage);
    }

    [Fact]
    public void InAssertion_WithJsonArray_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var jsonArray = JsonSerializer.Deserialize<JsonElement>(@"[""apple"", ""banana"", ""orange""]");
        var assertion = new InAssertion("banana", jsonArray);

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void TypeAssertion_WithCorrectType_ReturnsTrue()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new TypeAssertion("Hello", "string");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void TypeAssertion_WithIncorrectType_ReturnsFalse()
    {
        // Arrange
        var context = new TestExecutionContext();
        var assertion = new TypeAssertion(42, "string");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected type 'string' but got 'integer'", result.ErrorMessage);
    }

    [Fact]
    public void TypeAssertion_WithJsonElement_ReturnsCorrectType()
    {
        // Arrange
        var context = new TestExecutionContext();
        var jsonNumber = JsonSerializer.Deserialize<JsonElement>("42");
        var assertion = new TypeAssertion(jsonNumber, "integer");

        // Act
        var result = assertion.Execute(context);

        // Assert
        Assert.True(result.Success);
    }
}