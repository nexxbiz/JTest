using JTest.Cli.Settings;
using JTest.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace JTest.Cli.Commands;

public sealed class CreateCommand(IAnsiConsole ansiConsole) : CommandBase<CreateCommandSettings>(ansiConsole)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateCommandSettings settings, CancellationToken cancellationToken)
    {
        var testName = settings.Name!;
        var outputFile = $"{testName.Replace(" ", "_").ToLowerInvariant()}.json";

        Console.WriteLine($"Creating test template: {testName}");
        Console.WriteLine($"Output file: {outputFile}");

        try
        {
            var templateJson = TemplateBuilder.CreateTestTemplate(testName);
            await File.WriteAllTextAsync(outputFile, templateJson, cancellationToken);

            Console.WriteLine("Test template created successfully.");
            Console.WriteLine($"\nTo run the test:");
            Console.WriteLine($"  jtest run {outputFile} --env baseUrl=https://your-api.com");
            Console.WriteLine($"\nTo debug the test:");
            Console.WriteLine($"  jtest debug {outputFile} --env baseUrl=https://your-api.com");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteException(ex, ExceptionFormats.NoStackTrace);
            return 1;
        }
    }
}
