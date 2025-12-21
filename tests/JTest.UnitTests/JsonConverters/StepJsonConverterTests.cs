using JTest.Core.Assertions;
using JTest.Core.JsonConverters;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptorRegistries;
using JTest.Core.TypeDescriptors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace JTest.UnitTests.JsonConverters;

public sealed class StepJsonConverterTests
{
    private static readonly JsonSerializerOptions options = GetSerializerOptions();

    [Fact]
    public void When_DeserializeHttpStep_Then_Returns_HttpStep()
    {
        // Act
        var result = JsonSerializer.Deserialize<IStep>(httpStepJson, options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<HttpStep>(result);

        var configuration = result.Configuration as HttpStepConfiguration;
        Assert.NotNull(configuration);
        Assert.Equal("http-id", configuration.Id);
        Assert.Equal("Execute endpoint", configuration.Name);
        Assert.Equal("{{ $.env['api:baseUrl'] }}/{{ $.globals.endpoint }}", configuration.Url);
        Assert.Equal("GET", configuration.Method);
        Assert.Equal("test", configuration.Description);
        Assert.Equal("some-path.json", configuration.File);
        Assert.Equal("{{ $.globals.body }}", $"{configuration.Body}");
        Assert.Equal("application/json", configuration.ContentType);

        Assert.NotNull(configuration.Headers);
        Assert.Single(configuration.Headers);
        var header = configuration.Headers.First();
        Assert.Equal("request-id", header.Name);
        Assert.Equal("{{ $.env.requestId }}", header.Value);

        Assert.NotNull(configuration.Query);
        Assert.Single(configuration.Query);
        var query = configuration.Query.First();
        Assert.Equal("param1", query.Key);
        Assert.Equal("value1", query.Value);

        Assert.NotNull(configuration.FormFiles);
        Assert.Single(configuration.FormFiles);
        var formFile = configuration.FormFiles.First();
        Assert.Equal("testFile", formFile.Name);
        Assert.Equal("testFile.json", formFile.FileName);
        Assert.Equal("somepath.json", formFile.Path);
        Assert.Equal("application/json", formFile.ContentType);

        Assert.NotNull(configuration.Assert);
        Assert.Single(configuration.Assert);
        var assertion = configuration.Assert.First();
        Assert.IsType<EqualsAssertion>(assertion);
        Assert.Equal("{{ $.this.value }}", $"{assertion.ActualValue}");
        Assert.Equal("{{ $.globals.value }}", $"{assertion.ExpectedValue}");

        Assert.NotNull(configuration.Save);
        Assert.Single(configuration.Save);
        var save = configuration.Save.First();
        Assert.Equal("$.globals.var", save.Key);
        Assert.Equal("{{ $.this.value }}", $"{save.Value}");
    }

    [Fact]
    public void When_SerializeHttpStep_Then_Returns_HttpStepJson()
    {
        // Arrange
        var save = new KeyValuePair<string, object?>("$.globals.var","{{ $.this.value }}");
        var query = new KeyValuePair<string, string>("param1", "value1");
        var header = new HttpStepRequestHeaderConfiguration("header1", "value1");
        var formFile = new HttpStepFormFileConfiguration("name1","fileName1", "path1.json", "application/xml");
        var assert = new EqualsAssertion(null, null, "test", null);
        var httpStepConfiguration = new HttpStepConfiguration(
            $"{Guid.NewGuid()}",
            $"{Guid.NewGuid()}",
            $"{Guid.NewGuid()}",
            [assert],
            new Dictionary<string, object?>([save]),
            "GET",
            "https://url.com",
            new Dictionary<string, string>([query]),
            "some-path.json",
            "{{ $.globals.body }}",
            "application/json",
            [header],
            [formFile]
        );
        IStep httpStep = new HttpStep(Substitute.For<HttpClient>(), httpStepConfiguration);

        // Act
        var result = JsonSerializer.Serialize(httpStep, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        var resultObject = JsonNode.Parse(result)!.AsObject();
                
        Assert.Equal(httpStepConfiguration.Id, $"{resultObject["id"]}");
        Assert.Equal(httpStepConfiguration.Name, $"{resultObject["name"]}");
        Assert.Equal(httpStepConfiguration.Description, $"{resultObject["description"]}");
        Assert.Equal(httpStepConfiguration.Url, $"{resultObject["url"]}");
        Assert.Equal(httpStepConfiguration.Method, $"{resultObject["method"]}");
        Assert.Equal(httpStepConfiguration.File, $"{resultObject["file"]}");
        Assert.Equal(httpStepConfiguration.Body, $"{resultObject["body"]}");
        Assert.Equal(httpStepConfiguration.ContentType, $"{resultObject["contentType"]}");

        Assert.NotNull(resultObject["headers"]?.AsArray());
        Assert.Single(resultObject["headers"]!.AsArray());
        var headerObject = resultObject["headers"]![0]!;
        Assert.Equal(header.Name, $"{headerObject["name"]}");
        Assert.Equal(header.Value, $"{headerObject["value"]}");

        Assert.NotNull(resultObject["formFiles"]?.AsArray());
        Assert.Single(resultObject["formFiles"]!.AsArray());
        var formFileObject = resultObject["formFiles"]![0]!;
        Assert.Equal(formFile.Name, $"{formFileObject["name"]}");
        Assert.Equal(formFile.FileName, $"{formFileObject["fileName"]}");
        Assert.Equal(formFile.Path, $"{formFileObject["path"]}");
        Assert.Equal(formFile.ContentType, $"{formFileObject["contentType"]}");

        Assert.NotNull(resultObject["assert"]?.AsArray());
        Assert.Single(resultObject["assert"]!.AsArray());
        var assertObject = resultObject["assert"]![0]!;
        Assert.Equal("equals", $"{assertObject["op"]}");
        Assert.Equal(assert.Description, $"{assertObject["description"]}");

        Assert.NotNull(resultObject["save"]?.AsObject());
        Assert.Single(resultObject["save"]!.AsObject());
        var saveObject = resultObject["save"]!.AsObject();
        Assert.True(saveObject.ContainsKey(save.Key));
        Assert.Equal($"{save.Value}", $"{saveObject[save.Key]}");

        Assert.NotNull(resultObject["query"]?.AsObject());
        Assert.Single(resultObject["query"]!.AsObject());
        var queryObject = resultObject["query"]!.AsObject();
        Assert.True(queryObject.ContainsKey(query.Key));
        Assert.Equal(query.Value, $"{queryObject[query.Key]}");
    }

    [Fact]
    public void When_DeserializeUseStep_Then_Returns_UseStep()
    {
        // Act
        var result = JsonSerializer.Deserialize<IStep>(useStepJson, options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<UseStep>(result);

        var configuration = result.Configuration as UseStepConfiguration;
        Assert.NotNull(configuration);
        Assert.Equal("use-id", configuration.Id);
        Assert.Equal("Execute template", configuration.Name);
        Assert.Equal("templateName", configuration.Template);
        Assert.Equal("test", configuration.Description);
        
        Assert.NotNull(configuration.Assert);
        Assert.Single(configuration.Assert);
        var assertion = configuration.Assert.First();
        Assert.IsType<EqualsAssertion>(assertion);
        Assert.Equal("{{ $.this.value }}", $"{assertion.ActualValue}");
        Assert.Equal("{{ $.globals.value }}", $"{assertion.ExpectedValue}");

        Assert.NotNull(configuration.Save);
        Assert.Single(configuration.Save);
        var save = configuration.Save.First();
        Assert.Equal("$.globals.var", save.Key);
        Assert.Equal("{{ $.this.value }}", $"{save.Value}");

        Assert.NotNull(configuration.With);
        Assert.Single(configuration.With);
        var with = configuration.With.First();
        Assert.Equal("param1", with.Key);
        Assert.Equal("value1", $"{with.Value}");
    }

    [Fact]
    public void When_SerializeUseStep_Then_Returns_UseStepJson()
    {
        // Arrange
        var with = new KeyValuePair<string, object?>("param1", "value1");
        var save = new KeyValuePair<string, object?>("$.globals.var", "{{ $.this.value }}");     
        var assert = new EqualsAssertion(null, null, "test", null);
        var useStepConfiguration = new UseStepConfiguration(
            $"{Guid.NewGuid()}",
            $"{Guid.NewGuid()}",
            $"{Guid.NewGuid()}",
            [assert],
            new Dictionary<string, object?>([save]),
            $"{Guid.NewGuid()}",
            new Dictionary<string, object?>([with])
        );
        IStep step = new UseStep(Substitute.For<IAnsiConsole>(),Substitute.For<ITemplateContext>(), Substitute.For<IStepProcessor>(), useStepConfiguration);

        // Act
        var result = JsonSerializer.Serialize(step, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        var resultObject = JsonNode.Parse(result)!.AsObject();

        Assert.Equal(useStepConfiguration.Id, $"{resultObject["id"]}");
        Assert.Equal(useStepConfiguration.Name, $"{resultObject["name"]}");
        Assert.Equal(useStepConfiguration.Description, $"{resultObject["description"]}");        
        Assert.Equal(useStepConfiguration.Template, $"{resultObject["template"]}");

        Assert.NotNull(resultObject["assert"]?.AsArray());
        Assert.Single(resultObject["assert"]!.AsArray());
        var assertObject = resultObject["assert"]![0]!;
        Assert.Equal("equals", $"{assertObject["op"]}");
        Assert.Equal(assert.Description, $"{assertObject["description"]}");

        Assert.NotNull(resultObject["save"]?.AsObject());
        Assert.Single(resultObject["save"]!.AsObject());
        var saveObject = resultObject["save"]!.AsObject();
        Assert.True(saveObject.ContainsKey(save.Key));
        Assert.Equal($"{save.Value}", $"{saveObject[save.Key]}");

        Assert.NotNull(resultObject["with"]?.AsObject());
        Assert.Single(resultObject["with"]!.AsObject());
        var withObject = resultObject["with"]!.AsObject();
        Assert.True(withObject.ContainsKey(with.Key));
        Assert.Equal(with.Value, $"{withObject[with.Key]}");
    }

    [Fact]
    public void When_DeserializeWaitStep_Then_Returns_WaitStep()
    {
        // Act
        var result = JsonSerializer.Deserialize<IStep>(waitStepJson, options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<WaitStep>(result);

        var configuration = result.Configuration as WaitStepConfiguration;
        Assert.NotNull(configuration);
        Assert.Equal("wait-id", configuration.Id);
        Assert.Equal("Execute wait", configuration.Name);
        Assert.Equal("test", configuration.Description);
        Assert.Equal(500, configuration.Ms);

        Assert.NotNull(configuration.Assert);
        Assert.Single(configuration.Assert);
        var assertion = configuration.Assert.First();
        Assert.IsType<EqualsAssertion>(assertion);
        Assert.Equal("{{ $.this.value }}", $"{assertion.ActualValue}");
        Assert.Equal("{{ $.globals.value }}", $"{assertion.ExpectedValue}");

        Assert.NotNull(configuration.Save);
        Assert.Single(configuration.Save);
        var save = configuration.Save.First();
        Assert.Equal("$.globals.var", save.Key);
        Assert.Equal("{{ $.this.value }}", $"{save.Value}");
    }

    [Fact]
    public void When_DeserializeAssertStep_Then_Returns_AssertStep()
    {
        // Act
        var result = JsonSerializer.Deserialize<IStep>(assertStepJson, options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<AssertStep>(result);

        var configuration = result.Configuration as StepConfiguration;
        Assert.NotNull(configuration);
        Assert.Equal("assert-id", configuration.Id);
        Assert.Equal("Execute assert", configuration.Name);
        Assert.Equal("test", configuration.Description);        

        Assert.NotNull(configuration.Assert);
        Assert.Single(configuration.Assert);
        var assertion = configuration.Assert.First();
        Assert.IsType<EqualsAssertion>(assertion);
        Assert.Equal("{{ $.this.value }}", $"{assertion.ActualValue}");
        Assert.Equal("{{ $.globals.value }}", $"{assertion.ExpectedValue}");

        Assert.NotNull(configuration.Save);
        Assert.Single(configuration.Save);
        var save = configuration.Save.First();
        Assert.Equal("$.globals.var", save.Key);
        Assert.Equal("{{ $.this.value }}", $"{save.Value}");
    }


    [Fact]
    public void When_Deserialize_And_InvalidJson_Then_ThrowsException()
    {
        // Arrange
        const string invalidJson = "{\"op\": \"equals\" { }]";

        // Act & Assert
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IStep>(invalidJson, options)
        );
    }

    [Fact]
    public void When_Deserialize_And_MissingTypeProperty_Then_ThrowsException()
    {
        // Arrange
        const string invalidStepJson = "{\"description\": \"test\", \"name\": \"testName\", \"id\": \"123\" }";

        // Act & Assert
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IStep>(invalidStepJson, options)
        );
    }

    [Theory]
    [InlineData("[{\"type\": \"test\"}]")]
    [InlineData("textValue")]
    public void When_Deserialize_And_JsonIsNotObject_Then_ThrowsException(string invalidStepJson)
    {
        // Act & Assert
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IStep>(invalidStepJson, options)
        );
    }

    [Theory]
    [InlineData("{\"type\": \"\", \"description\": \"test\", \"mask\": true, \"actualValue\": 12, \"expectedValue\": 12.3 }")]
    [InlineData("{\"type\": null, \"description\": \"test\", \"mask\": true, \"actualValue\": 12, \"expectedValue\": 12.3 }")]
    public void When_Deserialize_And_TypeIsNullOrEmpty_Then_ThrowsException(string invalidStepJson)
    {
        // Act & Assert
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IStep>(invalidStepJson, options)
        );
    }

    [Theory]
    [InlineData("{\"type\": {}, \"description\": \"test\", \"id\": \"12\", \"name\": \"name\" }")]
    [InlineData("{\"type\": [], \"description\": \"test\", \"id\": \"12\", \"name\": \"name\" }")]
    [InlineData("{\"type\": 123, \"description\": \"test\", \"id\": \"12\", \"name\": \"name\" }")]
    [InlineData("{\"type\": true, \"description\": \"test\", \"id\": \"12\", \"name\": \"name\" }")]
    public void When_Deserialize_And_TypePropertyIsInvalidType_Then_ThrowsException(string invalidStepJson)
    {
        // Act & Assert
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IStep>(invalidStepJson, options)
        );
    }

    [Fact]
    public void When_Deserialize_And_Registry_Returns_DescriptorConstructor_WithoutRequiredStepConfigurationParameter_Then_ThrowsException()
    {
        // Arrange
        const string json = "{\"type\": \"test\"}";

        var brokenDescriptorRegistry = Substitute.For<ITypeDescriptorRegistry>();
        brokenDescriptorRegistry
            .GetDescriptor("test")
            .Returns(new TypeDescriptor(args => Substitute.For<IStep>(), "test", typeof(IStep), []));
        var registryProvider = Substitute.For<ITypeDescriptorRegistryProvider>();
        registryProvider.StepTypeRegistry.Returns(brokenDescriptorRegistry);

        var serializerOptions = GetSerializerOptions(registryProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => JsonSerializer.Deserialize<IStep>(json, serializerOptions)
        );
    }

    [Fact]
    public void When_Deserialize_And_Registry_Returns_Descriptor_ThatConstructsWrongType_Then_ThrowsException()
    {
        // Arrange
        const string json = "{\"type\": \"test\"}";

        var brokenDescriptorRegistry = Substitute.For<ITypeDescriptorRegistry>();
        brokenDescriptorRegistry
            .GetDescriptor("test")
            .Returns(new TypeDescriptor(args => new object(), "test", typeof(object), [new("config", typeof(StepConfiguration))]));
        var registryProvider = Substitute.For<ITypeDescriptorRegistryProvider>();
        registryProvider.StepTypeRegistry.Returns(brokenDescriptorRegistry);

        var serializerOptions = GetSerializerOptions(registryProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => JsonSerializer.Deserialize<IStep>(json, serializerOptions)
        );
    }

    private const string assertStepJson =
    """
    {
        "type": "assert",
        "id": "assert-id",
        "name": "Execute assert",
        "description": "test",
        "assert": [
            {
                "op": "equals",
                "actualValue": "{{ $.this.value }}",
                "expectedValue": "{{ $.globals.value }}"
            }
        ],
        "save":{
            "$.globals.var": "{{ $.this.value }}"
        }
    }
    """;

    private const string waitStepJson =
    """
    {
        "type": "wait",
        "id": "wait-id",
        "name": "Execute wait",
        "description": "test",
        "ms": 500,
        "assert": [
            {
                "op": "equals",
                "actualValue": "{{ $.this.value }}",
                "expectedValue": "{{ $.globals.value }}"
            }
        ],
        "save":{
            "$.globals.var": "{{ $.this.value }}"
        }
    }
    """;

    private const string useStepJson =
    """
    {
        "type": "use",
        "id": "use-id",
        "name": "Execute template",
        "description": "test",
        "template": "templateName",
        "with":{
            "param1": "value1"
        },
        "assert": [
            {
                "op": "equals",
                "actualValue": "{{ $.this.value }}",
                "expectedValue": "{{ $.globals.value }}"
            }
        ],
        "save":{
            "$.globals.var": "{{ $.this.value }}"
        }
    }
    """;

    private const string httpStepJson =
    """
    {
      "type": "http",
      "id": "http-id",
      "name": "Execute endpoint",
      "description": "test",
      "contentType": "application/json",
      "url": "{{ $.env['api:baseUrl'] }}/{{ $.globals.endpoint }}",
      "method": "GET",
      "headers": [
        {
          "name": "request-id",
          "value": "{{ $.env.requestId }}"
        }
      ],
      "query":{
        "param1": "value1"
      },
      "file": "some-path.json",
      "formFiles":[
        {
          "contentType": "application/json",
          "name": "testFile",
          "fileName": "testFile.json",
          "path": "somepath.json"
        }
      ],
      "body": "{{ $.globals.body }}",
      "assert": [
        {
          "op": "equals",
          "actualValue": "{{ $.this.value }}",
          "expectedValue": "{{ $.globals.value }}"
        }
      ],
      "save":{
        "$.globals.var": "{{ $.this.value }}"
      }
    }    
    """;


    private static JsonSerializerOptions GetSerializerOptions(ITypeDescriptorRegistryProvider? registryProvider = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddSingleton(new HttpClient())
            .AddSingleton(AnsiConsole.Console)
            .AddSingleton(Substitute.For<ITemplateContext>())
            .AddSingleton(Substitute.For<IStepProcessor>());

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
        options.Converters.Add(
            new StepJsonConverter(serviceProvider)
        );

        return options;
    }
}
