using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Orbyss.ProgramKit.Artifacts.Compatibility;
using Orbyss.ProgramKit.Artifacts.Primitives;
using Orbyss.ProgramKit.Artifacts.References;
using Orbyss.ProgramKit.DotNet.Composition;
using Orbyss.ProgramKit.DotNet.Documentation;
using Orbyss.ProgramKit.DotNet.Documentation.Console;
using Orbyss.ProgramKit.DotNet.Inputs;
using Orbyss.ProgramKit.DotNet.Operations;
using Orbyss.ProgramKit.DotNet.Packages;
using Orbyss.ProgramKit.DotNet.Shells;
using Orbyss.ProgramKit.Operations.Contracts;
using Orbyss.ProgramKit.Serialization.Json.Canonicalization;
using Orbyss.ProgramKit.Serialization.Json.Composition;
using Orbyss.ProgramKit.Serialization.Json.Contributions;
using Orbyss.ProgramKit.Serialization.Json.Profiles;
using Orbyss.ProgramKit.Serialization.Json.Serialization;

namespace JTest.HostInputs;

/// <summary>
/// Deterministically writes the jtest CLI host generation inputs
/// (hosting/shell.json, hosting/artifact-manifest.json,
/// hosting/inputs/*.json) with exact content digests, so the backed
/// Program Kit console generation can be reviewed and repeated.
/// </summary>
internal static class Program
{
    private const string HostIdentityValue = "pkid:host:jtest:cli";

    internal static int Main(string[] args)
    {
        var hostingRoot = Path.GetFullPath(args.Length > 0 ? args[0] : "hosting");
        var inputsRoot = Path.Combine(hostingRoot, "inputs");
        Directory.CreateDirectory(inputsRoot);

        var serializer = CreateSerializer();
        var limits = JsonSerializationLimits.Default;
        var profile = DotNetJsonProfiles.ShellBootstrap.Reference;

        var versionInput = "{}"u8.ToArray();
        var versionMapRevision = new ArtifactReference(
            new ProgramKitIdentifier("pkid:version-map:jtest:cli"),
            new SemanticVersion("1.0.0"),
            Digest(versionInput));
        var versionSelectionRevision = new ArtifactReference(
            new ProgramKitIdentifier("pkid:version-selection:jtest:cli"),
            new SemanticVersion("1.0.0"),
            Digest(versionInput));

        var shellIdentity = new ProgramKitIdentifier("pkid:shell:jtest:cli");
        var hostIdentity = new ProgramKitIdentifier(HostIdentityValue);
        var compatibility = Compatibility();

        var runOperation = Reference("pkid:operation:jtest:cli-run", '1');
        var validateOperation = Reference("pkid:operation:jtest:cli-validate", 'b');
        var describeOperation = Reference("pkid:operation:jtest:cli-describe", 'c');
        var argumentSchema = Reference("pkid:schema:jtest:cli-argument", '2');
        var resultSchema = Reference("pkid:schema:jtest:cli-result", '3');
        var generatorRevision = Reference("pkid:generator:jtest:cli-host", '4');
        var authorityRevision = Reference("pkid:authority:jtest:cli", 'a');

        var operationBindings = ImmutableArray.CreateRange(
        [
            Binding(runOperation, [argumentSchema], resultSchema, compatibility, "pkid:projection:jtest:cli-run", '8'),
            Binding(validateOperation, [argumentSchema], resultSchema, compatibility, "pkid:projection:jtest:cli-validate", 'd'),
            Binding(describeOperation, [], resultSchema, compatibility, "pkid:projection:jtest:cli-describe", 'e'),
        ]);

        var host = new DotNetHostDefinition(
            hostIdentity,
            new SemanticVersion("1.0.0"),
            DotNetHostKind.Console,
            Reference("pkid:profile:program-kit:dotnet-10", '5'),
            generatorRevision,
            [shellIdentity],
            [],
            [
                Package("CShells", "0.0.28", '6'),
                Package("Microsoft.Extensions.Hosting", "10.0.10", '7'),
            ],
            operationBindings,
            [],
            [],
            [],
            null,
            compatibility);

        var shell = new DotNetShellDocument(
            "pkid:schema:program-kit:dotnet-shell@11.0.0",
            new SemanticVersion("11.0.0"),
            versionMapRevision,
            versionSelectionRevision,
            new DotNetShellComposition(
                "cshells",
                new SemanticVersion("0.0.28"),
                [new DotNetShellSelection(shellIdentity, [])]),
            [],
            new DotNetJsonSerializationSelection(
                ImmutableArray<JsonSerializationProfileRef>.Empty,
                ImmutableArray<JsonSerializationContributionRef>.Empty),
            [host],
            compatibility);
        var shellBytes = serializer.Write(shell, profile, limits).ToArray();
        var shellRevision = new ArtifactReference(
            new ProgramKitIdentifier("pkid:shell-document:jtest:cli"),
            new SemanticVersion("1.0.0"),
            Digest(shellBytes));

        var document = BuildOpenConsoleDocument(
            hostIdentity,
            host.Version,
            runOperation,
            validateOperation,
            describeOperation,
            argumentSchema,
            authorityRevision,
            compatibility,
            shellRevision,
            generatorRevision);
        var documentBytes = serializer.Write(document, profile, limits).ToArray();
        var documentRevision = new ArtifactReference(
            new ProgramKitIdentifier("pkid:open-console-document:jtest:cli"),
            document.DocumentVersion,
            Digest(documentBytes));

        var manifest = new DotNetArtifactInputManifest(
            "pkid:schema:program-kit:dotnet-artifact-input-manifest@1.0.0",
            new SemanticVersion("1.0.0"),
            [
                new DotNetArtifactInputEntry(versionMapRevision, "inputs/version-map.json"),
                new DotNetArtifactInputEntry(versionSelectionRevision, "inputs/version-selection.json"),
                new DotNetArtifactInputEntry(documentRevision, "inputs/open-console.json"),
            ],
            [new DotNetHostDocumentInput(hostIdentity, documentRevision)]);

        WriteBytes(Path.Combine(hostingRoot, "shell.json"), shellBytes);
        WriteBytes(Path.Combine(inputsRoot, "version-map.json"), versionInput);
        WriteBytes(Path.Combine(inputsRoot, "version-selection.json"), versionInput);
        WriteBytes(Path.Combine(inputsRoot, "open-console.json"), documentBytes);
        WriteBytes(
            Path.Combine(hostingRoot, "artifact-manifest.json"),
            serializer.Write(manifest, profile, limits).ToArray());

        Console.Out.WriteLine($"host inputs written to {hostingRoot} (host {HostIdentityValue})");
        return 0;
    }

    private static DotNetOperationBinding Binding(
        ArtifactReference operationRevision,
        ImmutableArray<ArtifactReference> argumentSchemas,
        ArtifactReference resultSchema,
        ArtifactCompatibility compatibility,
        string projectionIdentity,
        char projectionDigest) =>
        new(
            new OperationContractDescriptor(
                operationRevision,
                argumentSchemas,
                [new OperationResultContract(resultSchema, OperationResultDisposition.Terminal)],
                [],
                [],
                [],
                null,
                null,
                OperationExpectedRevisionPolicy.Unsupported,
                OperationIdempotencyPolicy.Unsupported,
                OperationCancellationPolicy.Cooperative,
                OperationProgressPolicy.Unsupported,
                compatibility,
                new OperationDeprecation(false, null)),
            Reference(projectionIdentity, projectionDigest));

    private static OpenConsoleDocument BuildOpenConsoleDocument(
        ProgramKitIdentifier hostIdentity,
        SemanticVersion hostVersion,
        ArtifactReference runOperation,
        ArtifactReference validateOperation,
        ArtifactReference describeOperation,
        ArtifactReference argumentSchema,
        ArtifactReference authorityRevision,
        ArtifactCompatibility compatibility,
        ArtifactReference shellRevision,
        ArtifactReference generatorRevision)
    {
        var diagnosticsOption = ValueOption(
            "diagnostics", null, "text", "Diagnostic output format: text or json.");

        var runCommand = new OpenConsoleCommand(
            runOperation,
            ["run"],
            [],
            "Executes suites and writes truthful reports; the exit code is computed from the canonical evidence.",
            [PatternsArgument(argumentSchema)],
            [
                ValueOption("env-file", null, null, "Path of a JSON object file merged into env (CLI wins)."),
                MultiValueOption("env", "e", "Environment value as key=value; repeatable."),
                ValueOption("globals-file", null, null, "Path of a JSON object file merged into globals (CLI wins)."),
                MultiValueOption("secret-env", null, "Name of a CLI env entry whose value is redacted in evidence; repeatable."),
                ValueOption("report", null, "catalog", "Report mode: catalog (default) or standalone."),
                ValueOption("report-dir", null, null, "Catalog reports directory (default .jtest/reports)."),
                ValueOption("report-out", null, null, "Standalone report output directory."),
                ValueOption("parallel", "p", "1", "Maximum suites executed concurrently."),
                ValueOption("timeout", null, null, "Overall run timeout in milliseconds."),
                FlagOption("open", "Force opening the report page."),
                FlagOption("no-open", "Suppress opening the report page."),
                diagnosticsOption,
            ],
            null,
            null,
            null,
            StandardExitCodes(),
            authorityRevision,
            [
                new OpenConsoleExample(
                    ["run", "tests/**/*.suite.json", "--env", "baseUrl=https://api.example.test"],
                    "Runs every suite under tests and writes the catalog report."),
            ],
            null);

        var validateCommand = new OpenConsoleCommand(
            validateOperation,
            ["validate"],
            [],
            "Validates suites and prints machine-readable diagnostics without executing anything.",
            [PatternsArgument(argumentSchema)],
            [diagnosticsOption],
            null,
            null,
            null,
            StandardExitCodes(),
            authorityRevision,
            [
                new OpenConsoleExample(
                    ["validate", "tests/**/*.suite.json", "--diagnostics=json"],
                    "Validates every suite and emits diagnostics as JSON."),
            ],
            null);

        var describeCommand = new OpenConsoleCommand(
            describeOperation,
            ["describe"],
            [],
            "Emits the agent-facing language manifest or an exact published schema.",
            [],
            [
                ValueOption("schema", null, "manifest", "Artifact to emit: manifest, suite, templates, or result."),
                ValueOption("output", null, "-", "Target file path, or - for standard output."),
            ],
            null,
            null,
            null,
            StandardExitCodes(),
            authorityRevision,
            [
                new OpenConsoleExample(
                    ["describe", "--schema=suite", "--output=jtest-suite.schema.json"],
                    "Writes the published suite schema to a file."),
            ],
            null);

        return new OpenConsoleDocument(
            "pkid:schema:program-kit:open-console@1.0.0",
            new SemanticVersion("1.0.0"),
            new IntegratorDocumentInfo(
                "jtest",
                "Declarative JSON end-to-end API tests: run, validate, and describe.",
                new SemanticVersion("2.0.0")),
            new ArtifactReference(hostIdentity, hostVersion, Digest('9')),
            new OpenConsoleParsing(true, "--", true, true, "invariant", "bounded-by-occurrence"),
            [],
            [runCommand, validateCommand, describeCommand],
            new OpenConsoleHelp("help", "h", 0),
            new OpenConsoleCompletion("complete", true, true),
            compatibility,
            new IntegratorDocumentProvenance(shellRevision, generatorRevision, [runOperation, validateOperation, describeOperation]));
    }

    private static OpenConsoleArgument PatternsArgument(ArtifactReference argumentSchema) =>
        new(
            0,
            "patterns",
            "string",
            new ConsoleValueArity(1, 1),
            new ConsoleOccurrence(1, 64),
            true,
            null,
            argumentSchema,
            "Suite file patterns; a leading ! excludes.");

    private static ImmutableArray<OpenConsoleExitCode> StandardExitCodes() =>
    [
        new OpenConsoleExitCode(0, "Every discovered suite produced a complete passing trace.", []),
        new OpenConsoleExitCode(1, "At least one case failed, errored, timed out, or was cancelled.", []),
        new OpenConsoleExitCode(2, "Usage, input, discovery, or validation failure.", []),
        new OpenConsoleExitCode(3, "Unexpected internal failure.", []),
    ];

    private static OpenConsoleOption ValueOption(
        string longName,
        string? shortName,
        string? defaultValue,
        string summary) =>
        new(
            longName,
            shortName,
            [],
            ConsoleOptionKind.Value,
            "string",
            new ConsoleValueArity(1, 1),
            new ConsoleOccurrence(0, 1),
            false,
            defaultValue,
            null,
            null,
            [],
            [],
            summary);

    private static OpenConsoleOption MultiValueOption(string longName, string? shortName, string summary) =>
        new(
            longName,
            shortName,
            [],
            ConsoleOptionKind.Value,
            "string",
            new ConsoleValueArity(1, 1),
            new ConsoleOccurrence(0, 64),
            false,
            null,
            null,
            null,
            [],
            [],
            summary);

    private static OpenConsoleOption FlagOption(string longName, string summary) =>
        new(
            longName,
            null,
            [],
            ConsoleOptionKind.Flag,
            "boolean",
            new ConsoleValueArity(0, 0),
            new ConsoleOccurrence(0, 1),
            false,
            null,
            null,
            null,
            [],
            [],
            summary);

    private static ArtifactCompatibility Compatibility() =>
        new(
            new ProgramKitIdentifier("pkid:policy:jtest:cli-compatibility"),
            [new CompatibilityClaim(CompatibilityDimension.WireRead, CompatibilityClassification.Unknown, [])],
            new SemanticVersionRange("[1.0.0]"),
            new SemanticVersionRange("[1.0.0]"),
            []);

    private static DotNetPackageReference Package(string packageId, string version, char digest) =>
        new(packageId, new SemanticVersion(version), Digest(digest));

    private static ArtifactReference Reference(string identity, char digest) =>
        new(new ProgramKitIdentifier(identity), new SemanticVersion("1.0.0"), Digest(digest));

    private static Sha256Digest Digest(char value) =>
        new(string.Concat("sha256:", new string(value, 64)));

    private static Sha256Digest Digest(ReadOnlySpan<byte> content) =>
        new(string.Concat("sha256:", Convert.ToHexStringLower(SHA256.HashData(content))));

    private static ProgramKitJsonSerializer CreateSerializer()
    {
        ProgramKitJsonRegistryFactory registryFactory = new();
        ProgramKitJsonBuilder builder = new(registryFactory);
        DotNetJsonProfileRegistration registration = new();
        registration.Register(builder);
        return new ProgramKitJsonSerializer(builder.Freeze(), new ProgramKitJsonCanonicalizer());
    }

    private static void WriteBytes(string path, byte[] content) =>
        File.WriteAllBytes(path, content);
}
