using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace JTest.Cli.Options
{
    public sealed class RunCommandSettings : CommandSettings
    {
        [CommandArgument(0, "<test-file-patterns>")]
        [Description("List of test file patterns that are executed in order. Example: \"tests/**/*\" \"!tests/obsolete-tests/*\"")]
        public IEnumerable<string>? TestFilePatterns { get; set; }

        [CommandOption("-e|--env-file")]
        [Description("File path to environment variables")]
        public string? EnvironmentVariablesFile { get; set; }

        [CommandOption("-g|--globals-file")]
        [Description("File path to global variables")]
        public string? GlobalVariablesFile { get; set; }

        [CommandOption("-p|--parallel")]
        [Description("Run test files in parallel (default: 1)")]
        public int? ParallelTestExecutionCount { get; set; }

        [CommandOption("-o|--output")]
        [Description("Output folder path where reports are saved (default: working directory)")]
        public string? OutputDirectoryPath { get; set; }

        [CommandOption("-c|--categories")]
        [Description("Comma-separated list of test file categories to run (default: all)")]
        public IEnumerable<string>? Categories { get; set; }

        [CommandOption("--skip-output")]
        [Description("When specified, then does not output a report file (default: false)")]
        public bool? SkipOutput { get; set; }

        public override ValidationResult Validate()
        {
            if (TestFilePatterns?.Any() != true)
            {
                return ValidationResult.Error("At least one test file pattern is required.");
            }

            return ValidationResult.Success();
        }
    }
}
