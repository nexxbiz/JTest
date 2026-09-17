using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using JetBrains.Annotations;

namespace JTest.Cli.Settings;

[UsedImplicitly]
public sealed class ReportCommandSettings : CommandSettings
{
    // Must be an array: Spectre.Console.Cli only binds repeated occurrences of an option to an array
    // property (see RunCommandSettings.EnvironmentVariables / #74).
    [CommandOption("--trace")]
    [Description("Execution-trace JSON to render. Repeat the option to merge several traces in the given order.")]
    public string[]? TraceFiles { get; set; }

    [CommandOption("-o|--output|--report")]
    [Description("File path for the rendered report (see --format).")]
    public string? OutputFile { get; set; }

    [CommandOption("--merged-trace")]
    [Description("Also write the merged canonical trace JSON to this file path.")]
    public string? MergedTraceFile { get; set; }

    [CommandOption("-f|--format|--report-format")]
    [Description("Report format: 'html' (default, self-contained) or 'markdown'.")]
    public string? Format { get; set; }

    public override ValidationResult Validate()
    {
        if (!(TraceFiles?.Length > 0))
        {
            return ValidationResult.Error("At least one --trace file is required.");
        }

        foreach (var trace in TraceFiles)
        {
            if (!File.Exists(trace))
            {
                return ValidationResult.Error($"Trace file at path '{trace}' cannot be found.");
            }
        }

        if (string.IsNullOrWhiteSpace(OutputFile))
        {
            return ValidationResult.Error("An output file path (-o/--output) is required.");
        }

        if (!string.IsNullOrWhiteSpace(Format)
            && !IsHtml(Format)
            && !IsMarkdown(Format))
        {
            return ValidationResult.Error($"Unknown report format '{Format}'. Use 'html' or 'markdown'.");
        }

        return ValidationResult.Success();
    }

    private static bool IsHtml(string format) =>
        format.Equals("html", StringComparison.OrdinalIgnoreCase);

    private static bool IsMarkdown(string format) =>
        format.Equals("markdown", StringComparison.OrdinalIgnoreCase)
        || format.Equals("md", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Markdown when the format says so, or — absent an explicit format — when the output path ends
    /// in <c>.md</c>. Same rule the <c>run</c> command applies to <c>--report</c>.
    /// </summary>
    public bool WantsMarkdown()
    {
        if (!string.IsNullOrWhiteSpace(Format)) return IsMarkdown(Format);
        return OutputFile is not null && OutputFile.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    }
}
