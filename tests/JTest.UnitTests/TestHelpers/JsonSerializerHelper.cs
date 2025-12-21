using JTest.Core.JsonConverters;
using JTest.Core.Steps;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptorRegistries;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console;
using System.Text.Json;

namespace JTest.UnitTests.TestHelpers;

internal static class JsonSerializerHelper
{
    internal static readonly JsonSerializerOptions Options = GetSerializerOptions();

    internal static JsonSerializerOptions GetSerializerOptions(ITypeDescriptorRegistryProvider? registryProvider = null, ITemplateContext? templateContext = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddSingleton(new HttpClient())
            .AddSingleton(AnsiConsole.Console)            
            .AddSingleton(Substitute.For<IStepProcessor>());

        if (registryProvider is not null)
        {
            serviceCollection.AddSingleton(registryProvider);
        }
        else
        {
            serviceCollection.AddSingleton<ITypeDescriptorRegistryProvider, TypeDescriptorRegistryProvider>();
        }

        if (templateContext is not null)
        {
            serviceCollection.AddSingleton(templateContext);
        }
        else
        {
            serviceCollection.AddSingleton(Substitute.For<ITemplateContext>());
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
