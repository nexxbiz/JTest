using JTest.Core.Assertions;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptorRegistries;
using JTest.Core.TypeDescriptors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console;
using Xunit;

namespace JTest.UnitTests.TypeDescriptors;

public sealed class StepTypeDescriptorRegistryTests
{
    [Fact]
    public void When_GetDescriptors_Then_Returns_All_StepTypeDescriptors()
    {
        // Arrange        
        var expectedTypes = GetKnownStepTypes();
        var sut = GetSut();

        // Act
        var result = sut.GetDescriptors();

        // Assert
        Assert.True(result.All(descriptor => expectedTypes.Contains(descriptor.Value.Type)));
    }

    [Theory]
    [MemberData(nameof(CreateAllStepsInput))]
    public void Can_Create_AllSteps(string typeIdentifier, Type expectedType, TypeDescriptorConstructorArgument[] arguments)
    {
        // Arrange        
        var sut = GetSut();
        var descriptor = sut.GetDescriptor(typeIdentifier);

        // Act
        var instance = descriptor.Constructor.Invoke(arguments);

        // Assert        
        Assert.NotNull(instance);
        Assert.IsType(expectedType, instance);

        var step = (IStep)instance;
        Assert.NotNull(step.Configuration);
        Assert.NotNull(step.Configuration.Name);
        Assert.NotNull(step.Configuration.Description);
        Assert.NotNull(step.Configuration.Id);
    }

    private static Type[] GetKnownStepTypes()
    {
        var assembly = typeof(IStep).Assembly;
        var types = assembly
            .GetTypes()
            .Where(x => !x.IsAbstract && x.GetInterface(nameof(IStep)) != null)
            .ToArray();

        return types;
    }

    static ITypeDescriptorRegistry GetSut(bool registerStepDependencies = true)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<TypeDescriptorRegistryProvider>();

        if (registerStepDependencies)
        {
            serviceCollection
                .AddSingleton(new HttpClient())
                .AddSingleton(AnsiConsole.Console)
                .AddSingleton(Substitute.For<ITemplateContext>())
                .AddSingleton(Substitute.For<IStepProcessor>());
        }

        var serviceProvider = serviceCollection.BuildServiceProvider();

        return serviceProvider
            .GetRequiredService<TypeDescriptorRegistryProvider>()
            .StepTypeRegistry;
    }

    public static IEnumerable<object[]> CreateAllStepsInput => [CreateHttpStepInput, CreateWaitStepInput, CreateUseStepInput, CreateAssertStepInput];

    private static readonly object[] CreateAssertStepInput =
    [
        "assert",
        typeof(AssertStep),
        new TypeDescriptorConstructorArgument[]
        {
            new("configuration", new StepConfiguration($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", $"{Guid.NewGuid()}", null,null))
        }
    ];

    private static readonly object[] CreateUseStepInput =
    [
        "use",
        typeof(UseStep),
        new TypeDescriptorConstructorArgument[]
        {
            new("configuration", new UseStepConfiguration($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", $"{Guid.NewGuid()}", null,null, "template1", new Dictionary<string, object?>()))
        }
    ];

    private static readonly object[] CreateWaitStepInput =
    [
        "wait",
        typeof(WaitStep),
        new TypeDescriptorConstructorArgument[]
        {
            new("configuration", new WaitStepConfiguration($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", $"{Guid.NewGuid()}", null,null, 500))
        }
    ];

    private static readonly object[] CreateHttpStepInput =
    [
       "http",
        typeof(HttpStep),
        new TypeDescriptorConstructorArgument[]
        {
            new("configuration", new HttpStepConfiguration($"{Guid.NewGuid()}", $"{Guid.NewGuid()}", $"{Guid.NewGuid()}", null,null, "GET", "https://url.com", null, null, null, null, null, null))
        }
   ];
}
