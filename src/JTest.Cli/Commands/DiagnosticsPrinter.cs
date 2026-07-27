using System.Text.Json.Nodes;
using JTest.Cli.Ports;
using JTest.Language.Diagnostics;

namespace JTest.Cli.Commands;

/// <summary>Static diagnostic rendering: human text or canonical JSON.</summary>
public static class DiagnosticsPrinter
{
    /// <summary>Prints diagnostics in the selected format.</summary>
    /// <param name="diagnostics">The findings to print.</param>
    /// <param name="format">Either <c>text</c> or <c>json</c>.</param>
    /// <param name="console">Output sink; errors go to standard error in text mode.</param>
    public static void Print(
        IReadOnlyList<LanguageDiagnostic> diagnostics,
        string format,
        IConsoleWriter console)
    {
        if (format == "json")
        {
            var array = new JsonArray();
            foreach (var diagnostic in diagnostics)
            {
                var entry = new JsonObject
                {
                    ["code"] = diagnostic.Code,
                    ["severity"] = diagnostic.Severity == DiagnosticSeverity.Error ? "error" : "warning",
                    ["message"] = diagnostic.Message,
                    ["source"] = diagnostic.Source,
                    ["pointer"] = diagnostic.JsonPointer,
                };
                if (diagnostic.Hint is not null)
                {
                    entry["hint"] = diagnostic.Hint;
                }

                array.Add(entry);
            }

            console.Out(array.ToJsonString());
            return;
        }

        foreach (var diagnostic in diagnostics)
        {
            var severity = diagnostic.Severity == DiagnosticSeverity.Error ? "error" : "warning";
            var line = $"{diagnostic.Code} {severity} {diagnostic.Source}{FormatPointer(diagnostic.JsonPointer)}: {diagnostic.Message}";
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                console.ErrorLine(line);
            }
            else
            {
                console.Out(line);
            }
        }
    }

    private static string FormatPointer(string jsonPointer) =>
        jsonPointer.Length == 0 ? string.Empty : $" at '{jsonPointer}'";
}
