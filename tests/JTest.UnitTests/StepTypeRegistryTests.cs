using JTest.Core;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.Templates;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console;
using Xunit;

namespace JTest.UnitTests;

public sealed class StepTypeRegistryTests
{
    [Fact]
    public void When_GetDescriptors_Then_Returns_All_StepTypeDescriptors()
    {
        // Arrange        
        var sut = GetSut();

        // Act
        var result = sut.GetDescriptors();

        // Assert
        var expectedTypes = new Type[]
        {
            typeof(UseStep),
            typeof(WaitStep),
            typeof(HttpStep),
            typeof(AssertStep)
        };
        Assert.True(result.All(descriptor => expectedTypes.Contains(descriptor.Value.Type)));
    }

    [Theory]
    [MemberData(nameof(CreateInstancesAssertionInput))]
    public void When_Construct_Then_CreatesInstance(string typeIdentifier, Type expectedType, TypeDescriptorConstructorArgument[] arguments)
    {
        // Arrange        
        var sut = GetSut();

        // Act
        var result = sut.GetDescriptor(typeIdentifier);
        var instance = result.Constructor.Invoke(arguments);

        // Assert        
        Assert.NotNull(instance);
        Assert.IsType(expectedType, instance);

        var step = (IStep)instance;
        Assert.NotNull(step.Configuration);
        Assert.NotNull(step.Name);
        Assert.NotNull(step.Description);
        Assert.NotNull(step.Id);
    }

    static ITypeDescriptorRegistry GetSut()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(new HttpClient());
        serviceCollection.AddSingleton(ITypeDescriptorRegistry.CreateStepRegistry);
        serviceCollection.AddSingleton(AnsiConsole.Console);

        var templateContext = Substitute.For<ITemplateContext>();
        templateContext.GetTemplate(Arg.Any<string>()).Returns(new Template());
        serviceCollection.AddSingleton(templateContext);
        serviceCollection.AddSingleton(Substitute.For<IStepProcessor>());

        var serviceProvider = serviceCollection.BuildServiceProvider();

        return serviceProvider.GetRequiredService<ITypeDescriptorRegistry>();
    }

    public static IEnumerable<object[]> CreateInstancesAssertionInput => [CreateUseStepInstance, CreateAssertInstance];

    private static readonly object[] CreateAssertInstance =
    [
        "assert",
        typeof(AssertStep),
        new TypeDescriptorConstructorArgument[]
        {
            new("configuration", new StepConfiguration($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", $"{Guid.NewGuid()}", null,null))
        }
    ];

    private static readonly object[] CreateUseStepInstance =
    [
        "use",
        typeof(UseStep),
        new TypeDescriptorConstructorArgument[]
        {
            new("configuration", new UseStepConfiguration($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", $"{Guid.NewGuid()}", null,null, "template1", new Dictionary<string, object>()))
        }
    ];
}
