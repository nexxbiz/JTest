using JTest.Cli.Configuration;
using JTest.Cli.DI;
using JTest.Cli.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace JTest.Cli.Core;

/// <summary>
/// Main application orchestrator that handles initialization, configuration, and execution.
/// </summary>
internal static class JTestApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            // Create host with proper DI container
            using var host = CreateHost();
            await host.StartAsync();
            
            // Get console UI service from DI container
            var consoleUi = host.Services.GetRequiredService<IConsoleUiService>();
            consoleUi.ShowApplicationBanner();

            // Create and run command app
            var app = CreateCommandApp(host.Services);
            return await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return -1;
        }
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Add HttpClient
                services.AddHttpClient();
                
                // Configure all application services
                ConfigureApplicationServices(services);
            })
            .ConfigureLogging(logging =>
            {
                // Suppress hosting lifetime logs for cleaner CLI output
                logging.ClearProviders();
            })
            .UseConsoleLifetime(options =>
            {
                // Suppress hosting lifetime messages
                options.SuppressStatusMessages = true;
            })
            .Build();
    }

    private static void ConfigureApplicationServices(IServiceCollection services)
    {
        // Register console services
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
        services.AddSingleton<IConsoleUiService, ConsoleUiService>();
        services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
        
        // Register core services from JTest.Core
        DependencyRegistrationHelper.RegisterCoreServices(services);
    }

    private static CommandApp CreateCommandApp(IServiceProvider serviceProvider)
    {
        // Create services collection for Spectre.Console's TypeRegistrar
        var services = new ServiceCollection();
        services.AddHttpClient();
        
        var typeRegistrar = new TypeRegistrar(services);
        
        // Copy registrations from main DI container to Spectre's container
        CopyServicesFromProvider(typeRegistrar, serviceProvider);

        var app = new CommandApp(typeRegistrar);
        app.Configure(ApplicationConfiguration.Configure);
        
        return app;
    }

    private static void CopyServicesFromProvider(TypeRegistrar typeRegistrar, IServiceProvider serviceProvider)
    {
        // Register singleton instances from the main DI container
        typeRegistrar.RegisterInstance<IAnsiConsole>(serviceProvider.GetRequiredService<IAnsiConsole>());
        typeRegistrar.RegisterInstance<IConsoleUiService>(serviceProvider.GetRequiredService<IConsoleUiService>());
        typeRegistrar.RegisterInstance<IErrorHandlingService>(serviceProvider.GetRequiredService<IErrorHandlingService>());
        
        // Register other services using the existing registration helper
        DependencyRegistration.ConfigureServices(typeRegistrar);
    }
}
