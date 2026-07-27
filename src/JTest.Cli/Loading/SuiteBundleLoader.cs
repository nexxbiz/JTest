using JTest.Language.Diagnostics;
using JTest.Language.Reading;
using JTest.Language.Semantics;

namespace JTest.Cli.Loading;

/// <summary>
/// Loads one suite file with its template files (resolved relative to the
/// suite) and runs the full fail-closed validation stack.
/// </summary>
public sealed class SuiteBundleLoader
{
    private readonly ISuiteDocumentReader suiteReader;
    private readonly ITemplateFileReader templateReader;
    private readonly ISuiteBundleValidator bundleValidator;

    /// <summary>Creates the loader.</summary>
    /// <param name="suiteReader">Suite document reader.</param>
    /// <param name="templateReader">Template file reader.</param>
    /// <param name="bundleValidator">Cross-file semantics validator.</param>
    public SuiteBundleLoader(
        ISuiteDocumentReader suiteReader,
        ITemplateFileReader templateReader,
        ISuiteBundleValidator bundleValidator)
    {
        this.suiteReader = suiteReader;
        this.templateReader = templateReader;
        this.bundleValidator = bundleValidator;
    }

    /// <summary>Loads and validates one suite file.</summary>
    /// <param name="filePath">Absolute suite file path.</param>
    public LoadedSuite Load(string filePath)
    {
        var diagnostics = new List<LanguageDiagnostic>();
        var relativeName = Path.GetFileName(filePath);

        string suiteJson;
        try
        {
            suiteJson = File.ReadAllText(filePath);
        }
        catch (IOException exception)
        {
            diagnostics.Add(new LanguageDiagnostic(
                DiagnosticCodes.InvalidJson,
                DiagnosticSeverity.Error,
                $"The suite file could not be read: {exception.Message}",
                relativeName,
                string.Empty));
            return new LoadedSuite(filePath, null, diagnostics);
        }

        var suiteResult = suiteReader.Read(relativeName, suiteJson);
        diagnostics.AddRange(suiteResult.Diagnostics);
        if (suiteResult.Document is null)
        {
            return new LoadedSuite(filePath, null, diagnostics);
        }

        var templateFiles = new List<(string Source, JTest.Language.Documents.TemplateFileDocument Document)>();
        var suiteDirectory = Path.GetDirectoryName(filePath)!;
        foreach (var reference in suiteResult.Document.Using)
        {
            var templatePath = Path.GetFullPath(Path.Combine(suiteDirectory, reference));
            if (!File.Exists(templatePath))
            {
                diagnostics.Add(new LanguageDiagnostic(
                    DiagnosticCodes.UnknownTemplate,
                    DiagnosticSeverity.Error,
                    $"Template file '{reference}' does not exist relative to the suite.",
                    relativeName,
                    "/using"));
                continue;
            }

            var templateResult = templateReader.Read(reference, File.ReadAllText(templatePath));
            diagnostics.AddRange(templateResult.Diagnostics);
            if (templateResult.Document is not null)
            {
                templateFiles.Add((reference, templateResult.Document));
            }
        }

        var bundle = new SuiteBundle(relativeName, suiteResult.Document, templateFiles);
        diagnostics.AddRange(bundleValidator.Validate(bundle));

        return diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error)
            ? new LoadedSuite(filePath, null, diagnostics)
            : new LoadedSuite(filePath, bundle, diagnostics);
    }
}
