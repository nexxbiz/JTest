using JTest.Core.JsonConverters;
using JTest.Core.Steps;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spectre.Console;
using System.Text.Json;

namespace JTest.UnitTests.TestHelpers;

internal static class JsonSerializerHelper
{
    internal static readonly JsonSerializerOptions Options = GetSerializerOptions();

    /// <summary>The accessor form, for code that takes the real DI-registered dependency.</summary>
    internal static JTest.Core.Utilities.JsonSerializerOptionsAccessor OptionsAccessor => BuildAccessor();

    private static JTest.Core.Utilities.JsonSerializerOptionsAccessor BuildAccessor()
    {
        var services = new ServiceCollection()
            .AddSingleton(new HttpClient())
            .AddSingleton(AnsiConsole.Console)
            .AddSingleton(StepProcessor.Default)
            .AddSingleton<ITypeDescriptorRegistryProvider, TypeDescriptorRegistryProvider>()
            .AddSingleton(Substitute.For<ITemplateContext>())
            .AddSingleton<JTest.Core.Utilities.JsonSerializerOptionsAccessor>();

        return services.BuildServiceProvider()
            .GetRequiredService<JTest.Core.Utilities.JsonSerializerOptionsAccessor>();
    }

    internal static JsonSerializerOptions GetSerializerOptions(ITypeDescriptorRegistryProvider? registryProvider = null, ITemplateContext? templateContext = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddSingleton(new HttpClient())
            .AddSingleton(AnsiConsole.Console)
            .AddSingleton(StepProcessor.Default);

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
