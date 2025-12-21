using JTest.Core.Assertions;
using JTest.Core.Steps;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptorRegistries;
using JTest.Core.TypeDescriptors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console;
using Xunit;

namespace JTest.UnitTests.TypeDescriptors;

public sealed class AssertionTypeDescriptorRegistryTests
{
    [Fact]
    public void When_GetDescriptors_Then_Returns_All_StepTypeDescriptors()
    {
        // Arrange        
        var expectedTypes = GetKnownAssertionTypes();
        var sut = GetSut();

        // Act
        var result = sut.GetDescriptors();

        // Assert
        Assert.True(result.All(descriptor => expectedTypes.Contains(descriptor.Value.Type)));
    }

    [Theory]
    [MemberData(nameof(CreateAllAssertionsInput))]
    public void Can_Create_AllAssertions(string typeIdentifier, Type expectedType, TypeDescriptorConstructorArgument[] arguments)
    {
        // Arrange        
        var sut = GetSut();
        var descriptor = sut.GetDescriptor(typeIdentifier);

        // Act
        var instance = descriptor.Constructor.Invoke(arguments);

        // Assert        
        Assert.NotNull(instance);
        Assert.IsType(expectedType, instance);

        var assertion = (IAssertionOperation)instance;
        Assert.NotNull(assertion.Mask);
        Assert.NotNull(assertion.Description);
        Assert.NotNull(assertion.OperationName);
    }


    static ITypeDescriptorRegistry GetSut(bool registerStepDependencies = true)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddSingleton<TypeDescriptorRegistryProvider>();

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
            .AssertionTypeRegistry;
    }

    private static Type[] GetKnownAssertionTypes()
    {
        var assembly = typeof(IAssertionOperation).Assembly;
        var types = assembly
            .GetTypes()
            .Where(x => !x.IsAbstract && x.GetInterface(nameof(IAssertionOperation)) != null)
            .ToArray();

        return types;
    }

    public static IEnumerable<object[]> CreateAllAssertionsInput =>
    [
        CreateInAssertionStepInput,
        CreateTypeAssertionStepInput,
        CreateEqualsAssertionStepInput,
        CreateNotEqualsAssertionStepInput,
        CreateGreaterThanAssertionStepInput,
        CreateGreaterOrEqualAssertionStepInput,
        CreateLessThanAssertionStepInput,
        CreateLessOrEqualAssertionStepInput,
        CreateContainsAssertionStepInput,
        CreateNotContainsAssertionStepInput,
        CreateEmptyAssertionStepInput,
        CreateNotEmptyAssertionStepInput,
        CreateExistsAssertionStepInput,
        CreateNotExistsAssertionStepInput,
        CreateMatchAssertionStepInput,
        CreateBetweenAssertionStepInput,
        CreateLengthAssertionStepInput,
        CreateStartsWithAssertionStepInput,
        CreateEndsWithAssertionStepInput
    ];

    private static readonly TypeDescriptorConstructorArgument[] DefaultArguments =
    [
        new("expectedValue", $"{Guid.NewGuid()}"),
        new("actualValue", $"{Guid.NewGuid()}"),
        new("description", $"{Guid.NewGuid()}"),
        new("mask", true),
    ];

    private static readonly object[] CreateInAssertionStepInput =
   [
       "in",
        typeof(InAssertion),
        DefaultArguments
   ];

    private static readonly object[] CreateTypeAssertionStepInput =
   [
       "type",
        typeof(TypeAssertion),
        DefaultArguments
   ];

    private static readonly object[] CreateEqualsAssertionStepInput =
    [
        "equals",
        typeof(EqualsAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateNotEqualsAssertionStepInput =
    [
        "notequals",
        typeof(NotEqualsAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateGreaterThanAssertionStepInput =
    [
        "greaterthan",
        typeof(GreaterThanAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateGreaterOrEqualAssertionStepInput =
    [
        "greaterorequal",
        typeof(GreaterOrEqualAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateLessThanAssertionStepInput =
    [
        "lessthan",
        typeof(LessThanAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateLessOrEqualAssertionStepInput =
    [
        "lessorequal",
        typeof(LessOrEqualAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateContainsAssertionStepInput =
    [
        "contains",
        typeof(ContainsAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateNotContainsAssertionStepInput =
    [
        "notcontains",
        typeof(NotContainsAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateEmptyAssertionStepInput =
    [
        "empty",
        typeof(EmptyAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateNotEmptyAssertionStepInput =
    [
        "notempty",
        typeof(NotEmptyAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateExistsAssertionStepInput =
    [
        "exists",
        typeof(ExistsAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateNotExistsAssertionStepInput =
    [
        "notexists",
        typeof(NotExistsAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateMatchAssertionStepInput =
    [
        "match",
        typeof(MatchAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateBetweenAssertionStepInput =
    [
        "between",
        typeof(BetweenAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateLengthAssertionStepInput =
    [
        "length",
        typeof(LengthAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateStartsWithAssertionStepInput =
    [
        "startswith",
        typeof(StartsWithAssertion),
        DefaultArguments
    ];

    private static readonly object[] CreateEndsWithAssertionStepInput =
    [
        "endswith",
        typeof(EndsWithAssertion),
        DefaultArguments
    ];
}
