using Spectre.Console;

namespace JTest.Cli.Services;

/// <summary>
/// Service for handling application errors and exceptions.
/// </summary>
public interface IErrorHandlingService
{
    int HandleException(Exception exception, string context = "");
    void LogError(string message);
    void LogWarning(string message);
}

public class ErrorHandlingService(IAnsiConsole console) : IErrorHandlingService
{
    public int HandleException(Exception exception, string context = "")
    {
        var displayMessage = string.IsNullOrEmpty(context) 
            ? "An error occurred" 
            : $"An error occurred in {context}";

        console.MarkupLine($"[red]{displayMessage}:[/]");
        console.WriteException(exception, ExceptionFormats.ShortenEverything);
        
        Environment.ExitCode = -1;
        return -1;
    }

    public void LogError(string message)
    {
        console.MarkupLine($"[red]Error: {message}[/]");
    }

    public void LogWarning(string message)
    {
        console.MarkupLine($"[yellow]Warning: {message}[/]");
    }
}
