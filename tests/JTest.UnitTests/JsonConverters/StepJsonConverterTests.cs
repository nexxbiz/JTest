using JTest.Core.JsonConverters;
using JTest.Core.Steps;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptorRegistries;
using JTest.Core.Utilities;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console;
using System.Text.Json;

namespace JTest.UnitTests.JsonConverters;

public sealed class StepJsonConverterTests
{
    private const string httpStepJson =
    """
    {
      "type": "http",
      "name": "Execute endpoint",
      "url": "{{ $.env['api:baseUrl'] }}/{{ $.globals.endpoint }}",
      "method": "GET",
      "headers": [
        {
          "name": "request-id",
          "value": "{{ $.env.requestId}}"
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
          "actualValue": "{{ $.this.status }}",
          "expectedValue": "{{ $.globals.expectedStatusCode }}"
        }
      ]
    }    
    """;

    private readonly JsonSerializerOptions options = JsonSerializerOptionsCache.Default;

    public StepJsonConverterTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddSingleton(new HttpClient())
            .AddSingleton(AnsiConsole.Console)
            .AddSingleton(Substitute.For<ITemplateContext>())
            .AddSingleton(Substitute.For<IStepProcessor>())
            .AddSingleton<TypeDescriptorRegistryProvider>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        options.Converters.Add(
            new StepJsonConverter(serviceProvider)
        );
        options.Converters.Add(
            new AssertionOperationJsonConverter(serviceProvider)
        );
    }
}
