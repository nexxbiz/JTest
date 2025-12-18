using JTest.Core.Models;
using System.Text.Json;

namespace JTest.Cli.Utilities
{
    public sealed class GlobalConfigurationAccessor
    {
        private const string globalConfigFileEnvVar = "JTEST_CONFIG_FILE"; // Environment variable name for global config file path
        private static readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly Lazy<GlobalConfiguration?> globalConfiguration;

        public GlobalConfigurationAccessor()
        {
            globalConfiguration = new(LoadGlobalConfigurationFile);
        }

        public string GetOutputDirectory()
        {
            var globalConfig = globalConfiguration.Value;

            if (!string.IsNullOrWhiteSpace(globalConfig?.OutputDirectory))
            {
                return globalConfig.OutputDirectory;
            }

            return Directory.GetCurrentDirectory();
        }

        private static GlobalConfiguration? LoadGlobalConfigurationFile()
        {
            var globalConfigFilePath = Environment.GetEnvironmentVariable(globalConfigFileEnvVar, EnvironmentVariableTarget.Process);
            if (string.IsNullOrWhiteSpace(globalConfigFilePath))
            {
                return null;
            }
            if (!File.Exists(globalConfigFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"WARNING: Global configuration file at path '{globalConfigFilePath}' does not exist. Continuing without global config file."
                );
                Console.ResetColor();
                return null;
            }

            return JsonSerializer.Deserialize<GlobalConfiguration>(
                File.ReadAllText(globalConfigFilePath),
                options: jsonSerializerOptions
            );
        }
    }
}
