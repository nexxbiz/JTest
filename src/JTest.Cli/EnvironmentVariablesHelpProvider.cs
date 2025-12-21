using JTest.Cli.Commands;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;

namespace JTest.Cli;

public sealed class EnvironmentVariablesHelpProvider(ICommandAppSettings settings) : HelpProvider(settings)
{
    public override IEnumerable<IRenderable> Write(
        ICommandModel model,
        ICommandInfo? command)
    {
        // Yield the default help first
        foreach (var renderable in base.Write(model, command))
        {
            yield return renderable;
        }

        yield return new Text(Environment.NewLine);
        yield return new Markup("[yellow]ENVIRONMENT VARIABLES:[/]");
        yield return new Text(Environment.NewLine);

        var grid = new Grid();

        var envNameColumn = new GridColumn()
            .NoWrap()
            // spacing between env name & description
            .Padding(4, 0, 4, 0);

        grid.AddColumn(envNameColumn);
        grid.AddColumn();
        grid.AddRow(
            "JTEST_CONFIG_FILE",
            "Path to global JTest config file (JSON)"
        );

        yield return grid;
    }
}
