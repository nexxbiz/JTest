using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.JsonConverters;
using JTest.Core.TypeDescriptorRegistries;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace JTest.UnitTests
{
    public sealed class AssertionProcessorTests
    {
        private static readonly JsonSerializerOptions options = GetSerializerOptions();

        [Fact]
        public async Task AssertionProcessor_WithEqualsAssertion_ProcessesCorrectly()
        {
            // Arrange
            var context = new TestExecutionContext();
            context.Variables["response"] = new { status = 200 };

            var assertionJson = """
            [
                {
                    "op": "equals",
                    "actualValue": "{{$.response.status}}",
                    "expectedValue": 200
                }
            ]
            """;

            var assertions = JsonSerializer.Deserialize<IEnumerable<IAssertionOperation>>(assertionJson)!;
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

                var assertionJson = """
                [
                    {
                        "op": "lessthan",
                        "actualValue": "{{$.response.duration}}",
                        "expectedValue": 60.0
                    }
                ]
                """;

                var assertionsElement = JsonSerializer.Deserialize<IEnumerable<IAssertionOperation>>(assertionJson)!;

                // Act
                var results = await sut.ProcessAssertionsAsync(assertionsElement, context);

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
        public async Task AssertionProcessor_WithUnknownOperation_ReturnsFailure()
        {
            // Arrange
            var context = new TestExecutionContext();

            var assertionJson = """
            [
                {
                    "op": "unknown-operation",
                    "actualValue": "test"
                }
            ]
            """;

            var assertionsElement = JsonSerializer.Deserialize<IEnumerable<IAssertionOperation>>(assertionJson)!;
            var sut = new AssertionProcessor();

            // Act
            var results = await sut.ProcessAssertionsAsync(assertionsElement, context);

            // Assert
            Assert.Single(results);
            Assert.False(results.First().Success);
            Assert.Contains("Unknown assertion operation: 'unknown-operation'", results.First().ErrorMessage);
        }

        private static JsonSerializerOptions GetSerializerOptions(ITypeDescriptorRegistryProvider? registryProvider = null)
        {
            var serviceCollection = new ServiceCollection();

            if (registryProvider is not null)
            {
                serviceCollection.AddSingleton(registryProvider);
            }
            else
            {
                serviceCollection.AddSingleton<ITypeDescriptorRegistryProvider, TypeDescriptorRegistryProvider>();
            }

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            options.Converters.Add(
                new AssertionOperationJsonConverter(serviceProvider)
            );

            return options;
        }
    }
}
