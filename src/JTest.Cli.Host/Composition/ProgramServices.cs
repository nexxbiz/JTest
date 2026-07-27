using GeneratedHost.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeneratedHost.Composition;

internal static partial class Program
{
    static partial void ConfigureProgramKitConsoleServices(
        IServiceCollection services)
    {
        services.AddSingleton<
            IProgramKitConsoleCommandDispatcher,
            JTestConsoleCommandDispatcher>();

        // The console is jtest's user interface; host lifetime chatter would
        // pollute command output.
        services.AddLogging(static logging => logging.SetMinimumLevel(LogLevel.Warning));
    }
}
