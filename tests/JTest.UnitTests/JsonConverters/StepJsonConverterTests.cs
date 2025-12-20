using JTest.Core.Assertions;
using JTest.Core.JsonConverters;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptorRegistries;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console;
using System.Text.Json;
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
        Assert.Equal("http-id", result.Id);
        Assert.Equal("Execute endpoint", result.Name);
        Assert.Equal("test", result.Description);

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
    public void When_DeserializeUseStep_Then_Returns_UseStep()
    {
        // Act
        var result = JsonSerializer.Deserialize<IStep>(useStepJson, options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<UseStep>(result);
        Assert.Equal("use-id", result.Id);
        Assert.Equal("Execute template", result.Name);
        Assert.Equal("test", result.Description);

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
    public void When_DeserializeWaitStep_Then_Returns_WaitStep()
    {
        // Act
        var result = JsonSerializer.Deserialize<IStep>(waitStepJson, options);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<WaitStep>(result);
        Assert.Equal("wait-id", result.Id);
        Assert.Equal("Execute wait", result.Name);
        Assert.Equal("test", result.Description);

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
            WriteIndented = true
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
