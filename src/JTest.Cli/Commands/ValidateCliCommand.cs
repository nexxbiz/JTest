using JTest.Cli.Invocation;
using JTest.Cli.Loading;
using JTest.Cli.Ports;
using JTest.Language.Diagnostics;

namespace JTest.Cli.Commands;

/// <summary>Validates suites and reports machine-readable diagnostics.</summary>
public sealed class ValidateCliCommand
{
    private readonly SuiteBundleLoader loader;
    private readonly IConsoleWriter console;

    /// <summary>Creates the command.</summary>
    /// <param name="loader">Suite loader.</param>
    /// <param name="console">Console output.</param>
    public ValidateCliCommand(SuiteBundleLoader loader, IConsoleWriter console)
    {
        this.loader = loader;
        this.console = console;
    }

    /// <summary>Executes validation for the invocation.</summary>
    /// <param name="invocation">The parsed invocation.</param>
    /// <param name="environment">Ambient session facts.</param>
    public int Execute(CliInvocation invocation, CliEnvironment environment)
    {
        var format = invocation.LastValue("diagnostics") ?? "text";
        var files = SuiteFileDiscovery.Resolve(environment.WorkingDirectory, invocation.Arguments);
        if (files.Count == 0)
        {
            console.ErrorLine($"No suite files matched: {string.Join(", ", invocation.Arguments)}");
            return CliExitCodes.InvalidInput;
        }

        var allDiagnostics = new List<LanguageDiagnostic>();
        var invalid = 0;
        foreach (var file in files)
        {
            var loaded = loader.Load(file);
            allDiagnostics.AddRange(loaded.Diagnostics);
            if (!loaded.IsValid)
            {
                invalid++;
            }
        }

        DiagnosticsPrinter.Print(allDiagnostics, format, console);
        if (format != "json")
        {
            console.Out($"Validated {files.Count} file(s): {files.Count - invalid} valid, {invalid} invalid.");
        }

        return invalid == 0 ? CliExitCodes.Passed : CliExitCodes.InvalidInput;
    }
}
