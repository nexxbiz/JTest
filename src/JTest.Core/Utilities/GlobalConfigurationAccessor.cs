using JTest.Core.Models;
using Spectre.Console;
using System.Text.Json;

namespace JTest.Core.Utilities;

public sealed class GlobalConfigurationAccessor : IGlobalConfigurationAccessor
{
    private const string globalConfigFileEnvVar = "JTEST_CONFIG_FILE"; // Environment variable name for global config file path        

    private readonly Lazy<GlobalConfiguration> globalConfiguration;
    private readonly IAnsiConsole console;

    public GlobalConfigurationAccessor(IAnsiConsole console)
    {
        globalConfiguration = new(() => LoadGlobalConfigurationFile() ?? new());
        this.console = console;
    }

    public GlobalConfiguration Get()
    {
        return globalConfiguration.Value;
    }

    private GlobalConfiguration? LoadGlobalConfigurationFile()
    {
        var globalConfigFilePath = Environment.GetEnvironmentVariable(globalConfigFileEnvVar, EnvironmentVariableTarget.Process);
        if (string.IsNullOrWhiteSpace(globalConfigFilePath))
        {
            return null;
        }

        if (!File.Exists(globalConfigFilePath))
        {
            console.WriteLine(
                $"WARNING: Global configuration file at path '{globalConfigFilePath}' does not exist. Continuing without global config file.",
                new Style(foreground: Color.Yellow)
            );

            return null;
        }

        return JsonSerializer.Deserialize<GlobalConfiguration>(
            File.ReadAllText(globalConfigFilePath),
            options: JsonSerializerOptionsAccessor.Default
        );
    }
}
