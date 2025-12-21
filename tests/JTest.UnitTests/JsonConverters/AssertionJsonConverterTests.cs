using JTest.Core.Assertions;
using JTest.Core.JsonConverters;
using JTest.Core.Steps;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptorRegistries;
using JTest.Core.TypeDescriptors;
using JTest.Core.Utilities;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.Exceptions;
using NuGet.Frameworks;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace JTest.UnitTests.JsonConverters;

public sealed class AssertionJsonConverterTests
{
    private static readonly JsonSerializerOptions options = GetSerializerOptions();    

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

    [Theory]
    [MemberData(nameof(SerializeTestsMemberData))]
    public void When_Serialize_Then_Returns_AssertionOperationJson(IAssertionOperation assertion, string expectedJson)
    {
        // Act
        var result = JsonSerializer.Serialize(assertion, options);

        // Arrange
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        var resultObject = JsonNode.Parse(result)!.AsObject()!;
        var expectedObject = JsonNode.Parse(expectedJson)!.AsObject();

        Assert.Equal(expectedObject.Count, resultObject.Count);
        foreach (var property in expectedObject)
        {
            Assert.Equal($"{property.Value}", $"{resultObject[property.Key]}");
        }
    }

    [Fact]
    public void When_Deserialize_And_InvalidJson_Then_ThrowsException()
    {
        // Arrange
        const string invalidJson = "{\"op\": \"equals\" { }]";

        // Act & Assert
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IAssertionOperation>(invalidJson, options)
        );
    }

    [Fact]
    public void When_Deserialize_And_MissingOpProperty_Then_ThrowsException()
    {
        // Arrange
        const string invalidAssertionJson = "{\"description\": \"test\", \"mask\": true, \"actualValue\": 12, \"expectedValue\": 12.3 }";

        // Act & Assert
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IAssertionOperation>(invalidAssertionJson, options)
        );
    }

    [Theory]
    [InlineData("[{\"op\": \"test\"}]")]
    [InlineData("textValue")]
    public void When_Deserialize_And_JsonIsNotObject_Then_ThrowsException(string invalidAssertionJson)
    {
        // Act & Assert
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IAssertionOperation>(invalidAssertionJson, options)
        );
    }

    [Theory]
    [InlineData("{\"op\": \"\", \"description\": \"test\", \"mask\": true, \"actualValue\": 12, \"expectedValue\": 12.3 }")]
    [InlineData("{\"op\": null, \"description\": \"test\", \"mask\": true, \"actualValue\": 12, \"expectedValue\": 12.3 }")]
    public void When_Deserialize_And_OpIsNullOrEmpty_Then_ThrowsException(string invalidAssertionJson)
    {        
        // Act & Assert
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IAssertionOperation>(invalidAssertionJson, options)
        );
    }

    [Theory]
    [InlineData("{\"op\": {}, \"description\": \"test\", \"mask\": true, \"actualValue\": 12, \"expectedValue\": 12.3 }")]
    [InlineData("{\"op\": [], \"description\": \"test\", \"mask\": true, \"actualValue\": 12, \"expectedValue\": 12.3 }")]
    [InlineData("{\"op\": 123, \"description\": \"test\", \"mask\": true, \"actualValue\": 12, \"expectedValue\": 12.3 }")]
    [InlineData("{\"op\": true, \"description\": \"test\", \"mask\": true, \"actualValue\": 12, \"expectedValue\": 12.3 }")]
    public void When_Deserialize_And_OpPropertyIsInvalidType_Then_ThrowsException(string invalidAssertionJson)
    {        
        // Act & Assert
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IAssertionOperation>(invalidAssertionJson, options)
        );
    }

    [Fact]
    public void When_Deserialize_And_Registry_Returns_InvalidDescriptorConstructor_Then_ThrowsException()
    {
        // Arrange
        const string json = "{\"op\": \"test\"}";

        var brokenDescriptorRegistry = Substitute.For<ITypeDescriptorRegistry>();
        brokenDescriptorRegistry
            .GetDescriptor("test")
            .Returns(new TypeDescriptor(args => new object(), "test", typeof(object), []));
        var registryProvider = Substitute.For<ITypeDescriptorRegistryProvider>();
        registryProvider.AssertionTypeRegistry.Returns(brokenDescriptorRegistry);

        var serializerOptions = GetSerializerOptions(registryProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => JsonSerializer.Deserialize<IAssertionOperation>(json, serializerOptions)
        );
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
    static readonly TypeAssertion typeAssertion = new("{{ $.variable }}", "object", "test", true);

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

    static readonly EqualsAssertion equalsAssertion = new(12.3, 12.1, "test", true);

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

    static readonly NotEqualsAssertion notEqualsAssertion = new(12.3, 12.1, "test", true);

    const string greaterThanAssertionJson =
    """
    {
        "op": "greaterthan",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;
    static readonly GreaterThanAssertion greaterThanAssertion = new(12.3, 12.1, "test", true);

    const string greaterOrEqualAssertionJson =
    """
    {
        "op": "greaterorequal",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    static readonly GreaterOrEqualAssertion greaterOrEqualAssertion = new(12.3, 12.1, "test", true);

    const string lessThanAssertionJson =
    """
    {
        "op": "lessthan",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    static readonly LessThanAssertion lessThanAssertion = new(12.3, 12.1, "test", true);

    const string lessOrEqualAssertionJson =
    """
    {
        "op": "lessorequal",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;
    static readonly LessOrEqualAssertion lessOrEqualAssertion = new(12.3, 12.1, "test", true);

    const string existsAssertionJson =
    """
    {
        "op": "exists",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    static readonly ExistsAssertion existsAssertion = new("{{ $.variable }}", "test", true);


    const string notExistsAssertionJson =
    """
    {
        "op": "notexists",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    static readonly NotExistsAssertion notExistsAssertion = new("{{ $.variable }}", "test", true);

    const string emptyAssertionJson =
    """
    {
        "op": "empty",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    static readonly EmptyAssertion emptyAssertion = new("{{ $.variable }}", "test", true);

    const string notEmptyAssertionJson =
    """
    {
        "op": "notempty",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    static readonly NotEmptyAssertion notEmptyAssertion = new("{{ $.variable }}", "test", true);

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

    static readonly ContainsAssertion containsAssertion = new("{{ $.variable }}","value", "test", true);


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

    static readonly NotContainsAssertion notContainsAssertion = new("{{ $.variable }}", "value", "test", true);

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

    static readonly InAssertion inAssertion = new("{{ $.variable }}", "value", "test", true);

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

    static readonly MatchAssertion matchAssertion = new("{{ $.variable }}", "value", "test", true);

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

    static readonly BetweenAssertion betweenAssertion = new(2, "{{ $.variable }}", "test", true);

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

    static readonly LengthAssertion lengthAssertion = new("{{ $.variable }}", 2, "test", true);

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

    static readonly StartsWithAssertion startsWithAssertion = new("{{ $.variable }}", "a", "test", true);

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

    static readonly EndsWithAssertion endsWithAssertion = new("{{ $.variable }}", "a", "test", true);

    public static readonly IEnumerable<object[]> SerializeTestsMemberData =
    [
        [typeAssertion, typeAssertionJson],
        [equalsAssertion, equalsAssertionJson],
        [notEqualsAssertion, notEqualsAssertionJson],
        [greaterThanAssertion, greaterThanAssertionJson],
        [greaterOrEqualAssertion, greaterOrEqualAssertionJson],
        [lessThanAssertion, lessThanAssertionJson],
        [lessOrEqualAssertion, lessOrEqualAssertionJson],
        [emptyAssertion, emptyAssertionJson],
        [notEmptyAssertion, notEmptyAssertionJson],
        [existsAssertion, existsAssertionJson],
        [notExistsAssertion, notExistsAssertionJson],
        [containsAssertion, containsAssertionJson],
        [notContainsAssertion, notContainsAssertionJson],
        [startsWithAssertion, startsWithAssertionJson],
        [endsWithAssertion, endsWithAssertionJson],
        [betweenAssertion, betweenAssertionJson],
        [inAssertion, inAssertionJson],
        [lengthAssertion, lengthAssertionJson],
        [matchAssertion, matchAssertionJson]
    ];

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
