using JTest.Core;
using JTest.Core.Models;
using NSubstitute;
using Xunit;

namespace JTest.UnitTests;

public sealed class TypeDescriptorRegistryTests
{
    [Fact]
    public void When_RegisterType_And_TypeIdentifierPropertyNameInvalid_Then_ThrowsException()
    {
        // Arrange        
        var sut = GetSut<IMarkerInterfaceWithoutTypeIdentifier>("test");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            sut.RegisterTypes(typeof(MockImplementation));
        });
    }

    [Fact]
    public void When_RegisterTypes_And_TypeIdentifierPropertyTypeInvalid_Then_ThrowsException()
    {
        // Arrange
        var sut = GetSut<IMarkerInterfaceWithInvalidTypeIdentifierType>(nameof(IMarkerInterfaceWithInvalidTypeIdentifierType.Value));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            sut.RegisterTypes(typeof(MockImplementation));
        });
    }

    [Fact]
    public void When_RegisterTypes_And_ConstructorNotFound_Then_ThrowsException()
    {
        // Arrange                
        var sut = GetValidSut();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            sut.RegisterTypes(typeof(MockImplementationWithoutConstructor));
        });
    }

    [Fact]
    public void When_GetDescriptor_And_DescriptorNotFound_Then_ThrowsException()
    {
        // Arrange        
        const string unknownTypeIdentifier = "unknown_type";
        var sut = GetValidSut();
        sut.RegisterTypes(typeof(MockImplementation));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = sut.GetDescriptor(unknownTypeIdentifier);
        });
    }

    [Fact]
    public void When_Construct_And_ArgumentIsNull_Then_ThrowsException()
    {
        // Arrange        
        const string typeIdentifier = "test";
        var argument = new TypeDescriptorConstructorArgument("value", default(int?));
        var sut = GetValidSut();
        sut.RegisterTypes(typeof(MockImplementation));
        var descriptor = sut.GetDescriptor(typeIdentifier);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = descriptor.Constructor.Invoke([argument]);
        });
    }

    [Fact]
    public void When_Construct_And_ArgumentIsUnregisteredDependency_Then_ThrowsException()
    {
        // Arrange        
        const string typeIdentifier = "test-with-dependency";
        var validArgument = new TypeDescriptorConstructorArgument(
            "value",
            123
        );

        var sut = GetValidSut();
        sut.RegisterTypes(typeof(MockImplementationWithDependency));
        var descriptor = sut.GetDescriptor(typeIdentifier);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = descriptor.Constructor.Invoke([validArgument]);
        });
    }

    [Fact]
    public void When_Construct_And_AndAllArgumentsCanBeResolved_Then_CreatesInstance()
    {
        // Arrange        
        const string typeIdentifier = "test-with-dependency";
        var validArgument = new TypeDescriptorConstructorArgument(
            "value",
            123
        );
        var dependency = new MockImplementation(1);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider
            .GetService(typeof(IValidMarkerInterface))
            .Returns(dependency);

        var sut = GetValidSut(serviceProvider);
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

    private static TypeDescriptorRegistry<IValidMarkerInterface> GetValidSut(IServiceProvider? serviceProvider = null)
    {
        return GetSut<IValidMarkerInterface>(nameof(IValidMarkerInterface.ValidType), serviceProvider);
    }


    private static TypeDescriptorRegistry<TMarkerInterface> GetSut<TMarkerInterface>(string typeIdentifierPropertyName, IServiceProvider? serviceProvider = null)
    {
        return new TypeDescriptorRegistry<TMarkerInterface>(
            types: [],
            serviceProvider ?? Substitute.For<IServiceProvider>(),
            typeIdentifierPropertyName
        );        
    }

    private class MockImplementation(int? value) : IMarkerInterfaceWithoutTypeIdentifier, IMarkerInterfaceWithInvalidTypeIdentifierType, IValidMarkerInterface
    {
        public int Value => value ?? 0;

        public string ValidType => "test";
    }

    private class MockImplementationWithDependency(int? value, IValidMarkerInterface? dependency) : IValidMarkerInterface
    {
        public string ValidType => "test-with-dependency";

        public int? Value { get; } = value;

        public IValidMarkerInterface? Dependency { get; } = dependency;
    }

    private class MockImplementationWithoutConstructor() : IValidMarkerInterface
    {
        public string ValidType => "testWithoutConstructor";
    }

    private interface IMarkerInterfaceWithoutTypeIdentifier
    {
    }

    private interface IMarkerInterfaceWithInvalidTypeIdentifierType
    {
        int Value { get; }
    }

    private interface IValidMarkerInterface
    {
        string ValidType { get; }
    }
}
