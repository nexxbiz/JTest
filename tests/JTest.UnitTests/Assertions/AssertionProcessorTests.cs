using JTest.Core.Assertions;
using JTest.Core.Execution;
using System.Globalization;
using Xunit;

namespace JTest.UnitTests.Assertions
{
    public sealed class AssertionProcessorTests
    {
        [Fact]
        public async Task AssertionProcessor_WithEqualsAssertion_ProcessesCorrectly()
        {
            // Arrange
            var context = new TestExecutionContext();
            context.Variables["response"] = new { status = 200 };

            var assertions = new EqualsAssertion[]
            {
                new(50, 50)
            };

            var sut = new AssertionProcessor();

            // Act
            var results = await sut.ProcessAssertionsAsync(assertions, context);

            // Assert
            Assert.Single(results);
            Assert.True(results.First().Success);
        }

        [Fact]
        public async Task AssertionProcessor_WithNumericComparison_InDifferentCulture_WorksCorrectly()
        {
            // Arrange
            var sut = new AssertionProcessor();            
            var originalCulture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                // Arrange
                var context = new TestExecutionContext();
                context.Variables["response"] = new { duration = 30.5 };

                var assertions = new LessThanAssertion[]
                {
                    new("{{$.response.duration}}", 60.0)
                };

                // Act
                var results = await sut.ProcessAssertionsAsync(assertions, context);

                // Assert
                Assert.Single(results);
                Assert.True(results.First().Success);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Fact]
        public async Task AssertionProcessor_AssertionNotSuccess_ReturnsFailure()
        {
            // Arrange
            var context = new TestExecutionContext();            

            var assertions = new EqualsAssertion[]
            {
                new(50, 100)
            };
            var sut = new AssertionProcessor();

            // Act
            var results = await sut.ProcessAssertionsAsync(assertions, context);

            // Assert
            Assert.Single(results);
            Assert.False(results.First().Success);            
        }

        [Fact]
        public async Task AssertionProcessor_ResolvesVariablesInDescription()
        {
            // Arrange
            var context = new TestExecutionContext();
            context.Variables["globals"] = new Dictionary<string, object>
            {
                ["var"] = "custom description component"
            };
            var assertions = new EqualsAssertion[]
            {
                new(50, 100, "some description with {{ $.globals.var }}")
            };
            var expectedDescription = "some description with custom description component";

            var sut = new AssertionProcessor();

            // Act
            var results = await sut.ProcessAssertionsAsync(assertions, context);

            // Assert
            Assert.Single(results);
            Assert.Equal(expectedDescription, results.First().Description);
        }

        [Fact]
        public async Task AssertionProcessor_SetsMaskValue()
        {
            // Arrange
            var context = new TestExecutionContext();

            var assertions = new EqualsAssertion[]
            {
                new(50, 100, mask: true)
            };
            var sut = new AssertionProcessor();

            // Act
            var results = await sut.ProcessAssertionsAsync(assertions, context);

            // Assert
            Assert.Single(results);
            Assert.True(results.First().MaskValue);
        }
    }
}
