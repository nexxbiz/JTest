using JTest.Core.TypeDescriptorRegistries;
using JTest.Core.TypeDescriptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace JTest.UnitTests.TypeDescriptors
{
    public sealed class AssertionTypeDescriptorIdentificationTests
    {
        [Fact]
        public void When_Identify_Then_Returns_DefaultName()
        {
            // Arrange
            var sut = new AssertionTypeDescriptorIdentification();

            // Act
            var result = sut.Identify(typeof(MockAssertion));

            // Assert
            Assert.Equal("mock", result);
        }

        [Fact]
        public void When_Identify_And_TypeIdentifierAttributeSpecified_Then_Returns_IdentifierValue()
        {
            // Arrange
            var sut = new AssertionTypeDescriptorIdentification();

            // Act
            var result = sut.Identify(typeof(MockWithIdentifierAttributeAssertion));

            // Assert
            Assert.Equal("mock-assertion", result);
        }

        private sealed record MockAssertion();

        [TypeIdentifier("mock-assertion")]
        private sealed record MockWithIdentifierAttributeAssertion();
    }
}
