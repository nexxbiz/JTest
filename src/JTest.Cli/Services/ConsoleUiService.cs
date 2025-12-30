using Spectre.Console;

namespace JTest.Cli.Services;

/// <summary>
/// Service responsible for handling console UI elements like banners and messages.
/// </summary>
public interface IConsoleUiService
{
    void ShowApplicationBanner();
    void ShowError(string message);
    void ShowWarning(string message);
}

public class ConsoleUiService(IAnsiConsole console) : IConsoleUiService
{
    public void ShowApplicationBanner()
    {
        console.Write(
            new FigletText("JTEST")
                .Centered()
                .Color(Color.GreenYellow)
        );
    }

    public void ShowError(string message)
    {
        console.MarkupLine($"[red]Error: {message}[/]");
    }

    public void ShowWarning(string message)
    {
        console.MarkupLine($"[yellow]Warning: {message}[/]");
    }
}
