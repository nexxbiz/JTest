using JTest.Core.Models;
using JTest.Core.TypeDescriptorRegistries;
using JTest.Core.TypeDescriptors;
using NSubstitute;
using Xunit;

namespace JTest.UnitTests.TypeDescriptors;

public sealed class TypeDescriptorRegistryTests
{
    [Fact]
    public void When_RegisterTypes_And_ConstructorNotFound_Then_ThrowsException()
    {
        // Arrange                
        var sut = GetSut();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            sut.RegisterTypes(typeof(MockImplementationWithoutValidConstructor));
        });
    }

    [Fact]
    public void When_GetDescriptor_And_DescriptorNotFound_Then_ThrowsException()
    {
        // Arrange        
        const string unknownTypeIdentifier = "unknown_type";
        var sut = GetSut();
        sut.RegisterTypes(typeof(MockImplementation));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = sut.GetDescriptor(unknownTypeIdentifier);
        });
    }

    [Fact]
    public void When_Construct_And_AndAllArgumentsCanBeResolved_Then_CreatesInstance()
    {
        // Arrange        
        const string typeIdentifier = nameof(MockImplementationWithDependency);
        var validArgument = new TypeDescriptorConstructorArgument(
            "value",
            123
        );
        var dependency = new MockImplementation(1);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider
            .GetService(typeof(IMarkerInterface))
            .Returns(dependency);

        var sut = GetSut(serviceProvider);
        sut.RegisterTypes(typeof(MockImplementationWithDependency));
        var descriptor = sut.GetDescriptor(typeIdentifier);

        // Act
        var instance = descriptor.Constructor.Invoke([validArgument])
            as MockImplementationWithDependency;

        // Assert
        Assert.NotNull(instance);
        Assert.Equal(instance.Value, validArgument.Value);
        Assert.Same(instance.Dependency, dependency);
    }

    private static TypeDescriptorRegistry<IMarkerInterface> GetSut(IServiceProvider? serviceProvider = null)
    {
        return new TypeDescriptorRegistry<IMarkerInterface>(
            types: [],
            serviceProvider ?? Substitute.For<IServiceProvider>(),
            new MockDescriptorIdentification()
        );
    }

    private class MockDescriptorIdentification : ITypeDescriptorIdentification
    {
        public string Identify(Type type)
        {
            return type.Name;
        }
    }

    private class MockImplementation(int? value) : IMarkerInterface
    {
        public int Value => value ?? 0;
    }

    private class MockImplementationWithDependency(int? value, IMarkerInterface? dependency) : IMarkerInterface
    {
        public int? Value { get; } = value;

        public IMarkerInterface? Dependency { get; } = dependency;
    }

    private class MockImplementationWithoutValidConstructor() : IMarkerInterface
    {
    }

    private interface IMarkerInterface
    {
    }
}
