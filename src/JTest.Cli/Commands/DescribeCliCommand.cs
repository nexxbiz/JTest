using JTest.Cli.Invocation;
using JTest.Cli.Ports;
using JTest.Language;
using JTest.Reporting;

namespace JTest.Cli.Commands;

/// <summary>Emits the agent-facing language manifest or an exact published schema.</summary>
public sealed class DescribeCliCommand
{
    private readonly IConsoleWriter console;

    /// <summary>Creates the command.</summary>
    /// <param name="console">Console output.</param>
    public DescribeCliCommand(IConsoleWriter console)
    {
        this.console = console;
    }

    /// <summary>Executes describe for the invocation.</summary>
    /// <param name="invocation">The parsed invocation.</param>
    /// <param name="environment">Ambient session facts.</param>
    public int Execute(CliInvocation invocation, CliEnvironment environment)
    {
        var schema = invocation.LastValue("schema");
        var content = schema switch
        {
            null or "manifest" => LanguageContract.LanguageManifestJson,
            "suite" => LanguageContract.SuiteSchemaJson,
            "templates" => LanguageContract.TemplatesSchemaJson,
            "result" => ReportingContract.ResultSchemaJson,
            _ => null,
        };

        if (content is null)
        {
            console.ErrorLine($"Unknown schema '{schema}'. Known values: manifest, suite, templates, result.");
            return CliExitCodes.InvalidInput;
        }

        var output = invocation.LastValue("output") ?? "-";
        if (output == "-")
        {
            console.Out(content.TrimEnd('\n', '\r'));
        }
        else
        {
            var path = Path.GetFullPath(Path.Combine(environment.WorkingDirectory, output));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, new System.Text.UTF8Encoding(false));
            console.Out($"Wrote {schema ?? "manifest"} to {path}");
        }

        return CliExitCodes.Passed;
    }
}
