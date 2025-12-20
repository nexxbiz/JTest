using JTest.Core.Assertions;
using JTest.Core.JsonConverters;
using JTest.Core.Steps;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptorRegistries;
using JTest.Core.Utilities;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.Exceptions;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace JTest.UnitTests.JsonConverters;

public sealed class AssertionJsonConverterTests
{
    private static readonly JsonSerializerOptions options = GetOptions();
    private static JsonSerializerOptions GetOptions()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddSingleton(new HttpClient())
            .AddSingleton(AnsiConsole.Console)
            .AddSingleton(Substitute.For<ITemplateContext>())
            .AddSingleton(Substitute.For<IStepProcessor>())
            .AddSingleton<ITypeDescriptorRegistryProvider, TypeDescriptorRegistryProvider>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var options = JsonSerializerOptionsCache.Default;
        options.Converters.Add(
            new AssertionOperationJsonConverter(serviceProvider)
        );
        return options;
    }

    [Theory]
    [InlineData(typeAssertionJson, "{{ $.variable }}", "object", typeof(TypeAssertion))]
    [InlineData(equalsAssertionJson, 12.3, 12.1, typeof(EqualsAssertion))]
    [InlineData(notEqualsAssertionJson, 12.3, 12.1, typeof(NotEqualsAssertion))]
    [InlineData(lessThanAssertionJson, 12.3, 12.1, typeof(LessThanAssertion))]
    [InlineData(lessOrEqualAssertionJson, 12.3, 12.1, typeof(LessOrEqualAssertion))]
    [InlineData(greaterThanAssertionJson, 12.3, 12.1, typeof(GreaterThanAssertion))]
    [InlineData(greaterOrEqualAssertionJson, 12.3, 12.1, typeof(GreaterOrEqualAssertion))]
    [InlineData(existsAssertionJson, "{{ $.variable }}", null, typeof(ExistsAssertion))]
    [InlineData(notExistsAssertionJson, "{{ $.variable }}", null, typeof(NotExistsAssertion))]
    [InlineData(emptyAssertionJson, "{{ $.variable }}", null, typeof(EmptyAssertion))]
    [InlineData(notEmptyAssertionJson, "{{ $.variable }}", null, typeof(NotEmptyAssertion))]
    [InlineData(containsAssertionJson, "{{ $.variable }}", "value", typeof(ContainsAssertion))]
    [InlineData(notContainsAssertionJson, "{{ $.variable }}", "value", typeof(NotContainsAssertion))]
    [InlineData(inAssertionJson, "{{ $.variable }}", "value", typeof(InAssertion))]
    [InlineData(matchAssertionJson, "{{ $.variable }}", "value", typeof(MatchAssertion))]
    [InlineData(betweenAssertionJson,2, "{{ $.variable }}", typeof(BetweenAssertion))]
    [InlineData(lengthAssertionJson, "{{ $.variable }}",2, typeof(LengthAssertion))]
    [InlineData(startsWithAssertionJson, "{{ $.variable }}", "a", typeof(StartsWithAssertion))]
    [InlineData(endsWithAssertionJson, "{{ $.variable }}", "a", typeof(EndsWithAssertion))]
    public void When_Deserialize_Then_Returns_AssertionOperation(string json, object? actualValue, object? expectedValue, Type expectedType)
    {
        // Act
        var result = JsonSerializer.Deserialize<IAssertionOperation>(json, options);

        // Arrange
        Assert.NotNull(result);
        Assert.IsType(expectedType, result);
        Assert.Equal($"{expectedValue}", $"{result.ExpectedValue}");
        Assert.Equal($"{actualValue}", $"{result.ActualValue}");
        Assert.Equal("test", result.Description);
        Assert.True(result.Mask);
    }

    const string typeAssertionJson =
    """
    {
        "op": "type",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "object",
        "description": "test",
        "mask": true
    }
    """;

    const string equalsAssertionJson = 
    """
    {
        "op": "equals",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    const string notEqualsAssertionJson =
    """
    {
        "op": "notequals",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    const string greaterThanAssertionJson =
    """
    {
        "op": "greaterThan",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    const string greaterOrEqualAssertionJson =
    """
    {
        "op": "greaterOrEqual",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    const string lessThanAssertionJson =
    """
    {
        "op": "lessThan",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    const string lessOrEqualAssertionJson =
    """
    {
        "op": "lessOrEqual",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    const string existsAssertionJson =
    """
    {
        "op": "exists",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;


    const string notExistsAssertionJson =
    """
    {
        "op": "notexists",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    const string emptyAssertionJson =
    """
    {
        "op": "empty",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;


    const string notEmptyAssertionJson =
    """
    {
        "op": "notempty",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    const string containsAssertionJson =
    """
    {
        "op": "contains",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "value",
        "description": "test",
        "mask": true
    }
    """;


    const string notContainsAssertionJson =
    """
    {
        "op": "notcontains",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "value",
        "description": "test",
        "mask": true
    }
    """;

    const string inAssertionJson =
    """
    {
        "op": "in",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "value",
        "description": "test",
        "mask": true
    }
    """;

    const string matchAssertionJson =
    """
    {
        "op": "match",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "value",
        "description": "test",
        "mask": true
    }
    """;

    const string betweenAssertionJson =
    """
    {
        "op": "between",
        "actualValue": 2,
        "expectedValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    const string lengthAssertionJson =
    """
    {
        "op": "length",
        "actualValue": "{{ $.variable }}",
        "expectedValue": 2,
        "description": "test",
        "mask": true
    }
    """;

    const string startsWithAssertionJson =
    """
    {
        "op": "startswith",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "a",
        "description": "test",
        "mask": true
    }
    """;

    const string endsWithAssertionJson =
    """
    {
        "op": "endswith",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "a",
        "description": "test",
        "mask": true
    }
    """;
}
