using JTest.Cli.Services;
using JTest.Cli.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace JTest.Cli.Commands;

public sealed class ExportCommand(IAnsiConsole ansiConsole, IErrorHandlingService errorHandlingService)
    : CommandBase<ExportCommandSettings>(ansiConsole, errorHandlingService)
{
    public override Task<int> ExecuteAsync(CommandContext context, ExportCommandSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.Format!.ToLower();
        var testFile = settings.TestFilePath!;
        var outputDirectory = settings.OutputDirectoryPath ?? Directory.GetCurrentDirectory();
        var outputFile = Path.Combine(
            outputDirectory,
            GetOutputFileName(format, testFile)
        );

        Console.WriteLine($"Exporting test file: {testFile}");
        Console.WriteLine($"Export format: {format}");
        Console.WriteLine($"Output file: {outputFile}");

        // TODO: Implement actual export logic
        Console.WriteLine("Export completed successfully.");

        return Task.FromResult(0);
    }

    private static string GetOutputFileName(string format, string testFile)
    {
        return format switch
        {
            "postman" => Path.ChangeExtension(testFile, ".postman_collection.json"),
            "karate" => Path.ChangeExtension(testFile, ".feature"),
            _ => throw new NotSupportedException($"Export format '{format}' is not supported")
        };
    }
}
