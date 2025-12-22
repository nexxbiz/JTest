using JTest.Core.Assertions;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console;

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

    private static ITypeDescriptorRegistry GetSut(bool registerStepDependencies = true)
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
            new("configuration", new AssertStepConfiguration([new EqualsAssertion(1, 1)], $"{Guid.NewGuid()}", $"{Guid.NewGuid()}", $"{Guid.NewGuid()}"))
        }
    ];

    private static readonly object[] CreateUseStepInput =
    [
        "use",
        typeof(UseStep),
        new TypeDescriptorConstructorArgument[]
        {
            new("configuration", new UseStepConfiguration("template1",new Dictionary<string, object?>(), $"{Guid.NewGuid()}", $"{Guid.NewGuid()}", $"{Guid.NewGuid()}"))
        }
    ];

    private static readonly object[] CreateWaitStepInput =
    [
        "wait",
        typeof(WaitStep),
        new TypeDescriptorConstructorArgument[]
        {
            new("configuration", new WaitStepConfiguration(500, $"{Guid.NewGuid()}", $"{Guid.NewGuid()}", $"{Guid.NewGuid()}"))
        }
    ];

    private static readonly object[] CreateHttpStepInput =
    [
       "http",
        typeof(HttpStep),
        new TypeDescriptorConstructorArgument[]
        {
            new("configuration", new HttpStepConfiguration("GET", "https://url.com", Id:$"{Guid.NewGuid()}", Name:$"{Guid.NewGuid()}", Description:$"{Guid.NewGuid()}"))
        }
   ];
}
