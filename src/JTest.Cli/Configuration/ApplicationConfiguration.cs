using JTest.Cli.Commands;
using Spectre.Console.Cli;

namespace JTest.Cli.Configuration;

/// <summary>
/// Configures the CLI application commands and settings.
/// </summary>
internal static class ApplicationConfiguration
{
    public static void Configure(IConfigurator config)
    {
        config.Settings.ApplicationName = "jtest";
        config.SetHelpProvider(new EnvironmentVariablesHelpProvider(config.Settings));

        ConfigureRunCommand(config);
        ConfigureDebugCommand(config);
        ConfigureExportCommand(config);
        ConfigureCreateCommand(config);
        ConfigureValidateCommand(config);
    }

    private static void ConfigureRunCommand(IConfigurator config)
    {
        config
            .AddCommand<RunCommand>(CommandNames.Run)
            .WithDescription("Run test file(s) - supports wildcards")
            .WithExample("my_api_tests.json", "-e", "apiKey=SecretValue", "-o", "C://output")
            .WithExample("tests/api-*.json")
            .WithExample("tests/**/*", "!tests/obsolete-tests/*");
    }

    private static void ConfigureDebugCommand(IConfigurator config)
    {
        config
            .AddCommand<DebugCommand>(CommandNames.Debug)
            .WithDescription("Run test file(s) in debug mode (verbose) - supports wildcards")
            .WithExample("my_api_tests.json", "-e", "apiKey=SecretValue", "-o", "C://output")
            .WithExample("tests/*")
            .WithExample("tests/**/*", "!tests/obsolete-tests/*");
    }

    private static void ConfigureExportCommand(IConfigurator config)
    {
        config
            .AddCommand<ExportCommand>(CommandNames.Export)
            .WithDescription("Export tests to other frameworks")
            .WithExample("my_api_tests.json", "postman", "-o", "C://output");
    }

    private static void ConfigureCreateCommand(IConfigurator config)
    {
        config
            .AddCommand<CreateCommand>(CommandNames.Create)
            .WithDescription("Create a new test template")
            .WithExample("my_api_tests");
    }

    private static void ConfigureValidateCommand(IConfigurator config)
    {
        config
            .AddCommand<ValidateCommand>(CommandNames.Validate)
            .WithDescription("Validate a test file")
            .WithExample("my_api_tests");
    }
}
