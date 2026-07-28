using CShells.Features;
using JTest.Cli.Composition;
using JTest.Engine.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace JTest.Cli.Console;

/// <summary>
/// The single console shell feature: registers the typed command handlers,
/// the run validator, and the composition they execute against. Handler and
/// validator contracts are scoped, as the generated host's registration
/// audit requires.
/// </summary>
public sealed class JTestCliShellFeature : IShellFeature
{
    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(static _ => new HttpClient());
        services.AddSingleton<IProcessEnvironment, SystemProcessEnvironment>();
        services.AddSingleton<IJTestCliSession, SystemJTestCliSession>();
        services.AddSingleton(static provider => CliComposition.CreateRouter(
            provider.GetRequiredService<HttpClient>()));
        services.AddScoped<IJTestRunHandler, JTestRunHandler>();
        services.AddScoped<IJTestValidateHandler, JTestValidateHandler>();
        services.AddScoped<IJTestDescribeHandler, JTestDescribeHandler>();
        services.AddScoped<IJTestRunValidator, JTestRunValidator>();
    }
}
