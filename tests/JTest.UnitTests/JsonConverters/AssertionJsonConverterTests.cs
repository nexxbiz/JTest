using JTest.Core.Assertions;
using JTest.Core.TypeDescriptors;
using JTest.UnitTests.TestHelpers;
using NSubstitute;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JTest.UnitTests.JsonConverters;

[Collection(GlobalCultureCollection.DefinitionName)]
public sealed class AssertionJsonConverterTests
{
    private static readonly JsonSerializerOptions options = JsonSerializerHelper.Options;

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
    [InlineData(betweenAssertionJson, 2, "{{ $.variable }}", typeof(BetweenAssertion))]
    [InlineData(lengthAssertionJson, "{{ $.variable }}", 2, typeof(LengthAssertion))]
    [InlineData(startsWithAssertionJson, "{{ $.variable }}", "a", typeof(StartsWithAssertion))]
    [InlineData(endsWithAssertionJson, "{{ $.variable }}", "a", typeof(EndsWithAssertion))]
    public void When_Deserialize_Then_Returns_AssertionOperation(string json, object? actualValue, object? expectedValue, Type expectedType)
    {
        // Act
        var result = JsonSerializer.Deserialize<IAssertionOperation>(json, options);

        // Arrange
        Assert.NotNull(result);
        Assert.IsType(expectedType, result);

        var exp = $"{expectedValue}";
        var otherExp = $"{result.ExpectedValue}";
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
#pragma warning disable JSON001 // Invalid JSON pattern
        const string invalidJson = "{\"op\": \"equals\" { }]";
#pragma warning restore JSON001 // Invalid JSON pattern

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

        var serializerOptions = JsonSerializerHelper.GetSerializerOptions(registryProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => JsonSerializer.Deserialize<IAssertionOperation>(json, serializerOptions)
        );
    }

    private const string typeAssertionJson =
    """
    {
        "op": "type",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "object",
        "description": "test",
        "mask": true
    }
    """;
    private static readonly TypeAssertion typeAssertion = new("{{ $.variable }}", "object", "test", true);

    private const string equalsAssertionJson =
    """
    {
        "op": "equals",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    private static readonly EqualsAssertion equalsAssertion = new(12.3, 12.1, "test", true);

    private const string notEqualsAssertionJson =
    """
    {
        "op": "notequals",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    private static readonly NotEqualsAssertion notEqualsAssertion = new(12.3, 12.1, "test", true);

    private const string greaterThanAssertionJson =
    """
    {
        "op": "greaterthan",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;
    private static readonly GreaterThanAssertion greaterThanAssertion = new(12.3, 12.1, "test", true);

    private const string greaterOrEqualAssertionJson =
    """
    {
        "op": "greaterorequal",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    private static readonly GreaterOrEqualAssertion greaterOrEqualAssertion = new(12.3, 12.1, "test", true);

    private const string lessThanAssertionJson =
    """
    {
        "op": "lessthan",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;

    private static readonly LessThanAssertion lessThanAssertion = new(12.3, 12.1, "test", true);

    private const string lessOrEqualAssertionJson =
    """
    {
        "op": "lessorequal",
        "actualValue": 12.3,
        "expectedValue": 12.1,
        "description": "test",
        "mask": true
    }
    """;
    private static readonly LessOrEqualAssertion lessOrEqualAssertion = new(12.3, 12.1, "test", true);

    private const string existsAssertionJson =
    """
    {
        "op": "exists",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    private static readonly ExistsAssertion existsAssertion = new("{{ $.variable }}", "test", true);


    private const string notExistsAssertionJson =
    """
    {
        "op": "notexists",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    private static readonly NotExistsAssertion notExistsAssertion = new("{{ $.variable }}", "test", true);

    private const string emptyAssertionJson =
    """
    {
        "op": "empty",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    private static readonly EmptyAssertion emptyAssertion = new("{{ $.variable }}", "test", true);

    private const string notEmptyAssertionJson =
    """
    {
        "op": "notempty",
        "actualValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    private static readonly NotEmptyAssertion notEmptyAssertion = new("{{ $.variable }}", "test", true);

    private const string containsAssertionJson =
    """
    {
        "op": "contains",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "value",
        "description": "test",
        "mask": true
    }
    """;

    private static readonly ContainsAssertion containsAssertion = new("{{ $.variable }}", "value", "test", true);


    private const string notContainsAssertionJson =
    """
    {
        "op": "notcontains",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "value",
        "description": "test",
        "mask": true
    }
    """;

    private static readonly NotContainsAssertion notContainsAssertion = new("{{ $.variable }}", "value", "test", true);

    private const string inAssertionJson =
    """
    {
        "op": "in",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "value",
        "description": "test",
        "mask": true
    }
    """;

    private static readonly InAssertion inAssertion = new("{{ $.variable }}", "value", "test", true);

    private const string matchAssertionJson =
    """
    {
        "op": "match",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "value",
        "description": "test",
        "mask": true
    }
    """;

    private static readonly MatchAssertion matchAssertion = new("{{ $.variable }}", "value", "test", true);

    private const string betweenAssertionJson =
    """
    {
        "op": "between",
        "actualValue": 2,
        "expectedValue": "{{ $.variable }}",
        "description": "test",
        "mask": true
    }
    """;

    private static readonly BetweenAssertion betweenAssertion = new(2, "{{ $.variable }}", "test", true);

    private const string lengthAssertionJson =
    """
    {
        "op": "length",
        "actualValue": "{{ $.variable }}",
        "expectedValue": 2,
        "description": "test",
        "mask": true
    }
    """;

    private static readonly LengthAssertion lengthAssertion = new("{{ $.variable }}", 2, "test", true);

    private const string startsWithAssertionJson =
    """
    {
        "op": "startswith",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "a",
        "description": "test",
        "mask": true
    }
    """;

    private static readonly StartsWithAssertion startsWithAssertion = new("{{ $.variable }}", "a", "test", true);

    private const string endsWithAssertionJson =
    """
    {
        "op": "endswith",
        "actualValue": "{{ $.variable }}",
        "expectedValue": "a",
        "description": "test",
        "mask": true
    }
    """;

    private static readonly EndsWithAssertion endsWithAssertion = new("{{ $.variable }}", "a", "test", true);

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
}
