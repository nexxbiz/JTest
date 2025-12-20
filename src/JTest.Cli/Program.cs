using JTest.Cli.Commands;
using JTest.Cli.DI;
using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Output;
using JTest.Core.Output.Markdown;
using JTest.Core.Steps;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptorRegistries;
using JTest.Core.Utilities;
using JTest.Core.Variables;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace JTest.Cli;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient();

        var typeRegistrar = new TypeRegistrar(serviceCollection);
        RegisterDependencies(typeRegistrar);

        var app = new CommandApp(typeRegistrar);

        app.Configure(config =>
        {
            config
                .AddCommand<RunCommand>("run")
                .WithDescription("Run test file(s) - supports wildcards")
                .WithExample("my_api_tests.json", "-e", "apiKey=SecretValue", "-o", "C://output")
                .WithExample("tests/api-*.json")
                .WithExample("tests/**/*", "!tests/obsolete-tests/*");

            config
                .AddCommand<DebugCommand>("debug")
                .WithDescription("Run test file(s) in debug mode (verbose) - supports wildcards")
                .WithExample("my_api_tests.json", "-e", "apiKey=SecretValue", "-o", "C://output")
                .WithExample("tests/*")
                .WithExample("tests/**/*", "!tests/obsolete-tests/*");

            config
                .AddCommand<ExportCommand>("export")
                .WithDescription("Export tests to other frameworks")
                .WithExample("my_api_tests.json", "postman", "-o", "C://output");

            config
                .AddCommand<CreateCommand>("create")
                .WithDescription("Create a new test template")
                .WithExample("my_api_tests");

            config
                .AddCommand<ValidateCommand>("validate")
                .WithDescription("Create a new test template")
                .WithExample("my_api_tests");
        });

        AnsiConsole.Write(
            new FigletText("JTEST")
                .Centered()
                .Color(Color.GreenYellow)
        );

        await app.RunAsync(args);
    }

    private static void RegisterDependencies(TypeRegistrar typeRegistrar)
    {
        typeRegistrar
            .Register<IAssertionProcessor, AssertionProcessor>()
            .Register<IStepProcessor, StepProcessor>()
            .Register<IVariablesContext, VariablesContext>()
            .Register<GlobalConfigurationAccessor>()
            .Register<IJTestCaseExecutor, JTestCaseExecutor>()
            .Register<IJTestSuiteExecutionResultProcessor, JTestSuiteExecutionResultProcessor>()
            .Register<IJTestSuiteExecutor, JTestSuiteExecutor>()
            .RegisterInstance<IAnsiConsole>(AnsiConsole.Console)
            .Register<ITemplateContext, TemplateContext>()
            .Register<MarkdownOutputGenerator>()
            .Register<ITypeDescriptorRegistryProvider, TypeDescriptorRegistryProvider>()
            .RegisterInstance<IDictionary<string, IOutputGenerator>>(sp =>
            {
                return new Dictionary<string, IOutputGenerator>(StringComparer.OrdinalIgnoreCase)
                {
                    ["markdown"] = sp.GetRequiredService<MarkdownOutputGenerator>()
                };
            });
    }
}
