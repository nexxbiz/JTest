# JTest as a Program Kit development tool

This page is the canonical, source-owned description of JTest for
projection by the Program Kit website. All technical truth lives in this
repository; the website may quote or embed it but owns none of it.

## Purpose

JTest is a declarative JSON language and command-line tool for end-to-end
API integration tests — arrange, act, assert against real HTTP endpoints —
producing truthful, canonical execution evidence and static human reports.
It is designed for mixed human/AI authoring: the entire language is
published as machine-readable contracts the tool itself emits.

## Maturity

- Version: **2.0.0-alpha.1** (pre-release; not yet published to a feed).
- Ground-up rebuild completed against an approved, digest-bound design
  ([design/jtest-2.0/](../design/jtest-2.0/)); release readiness stated in
  [release-readiness.md](../design/jtest-2.0/release-readiness.md).
- 71/71 tests, zero warnings, with process-level acceptance against the
  shipped binary.

## Architecture

Four products with strict downward-only dependencies:

| product | owns |
| --- | --- |
| `JTest.Language` | What a test means: typed models, published JSON Schemas, three-layer fail-closed validation, stable `JT****` diagnostics, agent-facing manifest. |
| `JTest.Engine` | What happened: contexts and scopes, fail-closed expressions, step execution, assertions, capture-time redaction, the immutable execution trace. |
| `JTest.Reporting` | How it is evidenced and shown: the RFC 8785-canonical result document, deterministic catalog/standalone writers, the embedded static viewer. |
| `JTest.Cli` + `JTest.Cli.Host` | Invocation: the command library plus the **Program Kit-generated** console host (`jtest` dotnet tool). |

Key invariant: trace outcomes aggregate as pure functions of children — a
node can never out-claim its weakest child, so a false-green result is
structurally impossible.

Deep dives: [language](language/reference.md) ·
[diagnostics](language/diagnostics.md) · [CLI](cli.md) ·
[reports and security](reporting.md) · [agent authoring](agents.md) ·
[getting started](getting-started.md).

## Standalone journey (no Program Kit knowledge required)

1. Author a suite (`"jtest": "2.0"`), guided by
   `jtest describe --schema suite`.
2. `jtest validate` — fail-closed, machine-readable diagnostics.
3. `jtest run` — exit codes computed from evidence (`0` pass, `1` test
   failures, `2` usage/input/validation, `3` internal); the report URL is
   always printed as a clickable `file:///` link and opens automatically
   in interactive sessions.
4. Inspect the catalog report (`.jtest/reports/index.html`, refresh after
   each run) or ship the standalone single-file artifact
   (`--report standalone`) plus `result.json` to downstream automation.

The complete walkthrough is [getting-started.md](getting-started.md); the
full grammar is locked to the typed Open Console document at
[hosting/inputs/open-console.json](../hosting/inputs/open-console.json).

## Program Kit integration mode

JTest consumes Program Kit in exactly two ways:

1. **Runtime packages** — `Orbyss.ProgramKit.Serialization.JSON` (RFC 8785
   canonicalization and digests for the result document) and
   `Orbyss.ProgramKit.Artifacts` (revision/digest primitives).
2. **Build-time generation** — the backed
   `program-kit dotnet generate-host console` operation generates the
   `jtest` CLI host from typed inputs authored by
   [tools/JTest.HostInputs](../tools/JTest.HostInputs/Program.cs) and
   regenerated via [tools/generate-cli-host.ps1](../tools/generate-cli-host.ps1).
   Generated applications do not reference the generator at runtime.

### Exact contracts and compatibility

| contract | selection |
| --- | --- |
| Program Kit source pin | commit `b4b14cd88a1e931531cbcdeddc2c2273ad96f4f4`, packages `0.1.0-alpha.1` (see [packages/local-feed.manifest.json](../packages/local-feed.manifest.json) for per-package digests) |
| Target profile | .NET SDK `10.0.302` (rollForward disable), `net10.0`, C# 14 |
| Open Console document schema | `pkid:schema:program-kit:open-console@1.0.0` |
| Shell document schema | `pkid:schema:program-kit:dotnet-shell@11.0.0` |
| Artifact input manifest schema | `pkid:schema:program-kit:dotnet-artifact-input-manifest@1.0.0` |
| Console dispatch contract | `pkid:contract:program-kit:console-command-dispatcher` 1.0.0 (`IProgramKitConsoleCommandDispatcher`) |
| Host packages pinned by generation | `CShells [0.0.28]`, `Microsoft.Extensions.Hosting [10.0.10]` |

### Generated evidence

The generated host tree is committed and lock-bound:
[shell.lock.json](../src/JTest.Cli.Host/shell.lock.json),
[console-command-dispatch.lock.json](../src/JTest.Cli.Host/ProgramKitGenerated/Commands/console-command-dispatch.lock.json),
and
[console-command-dispatch.evidence.json](../src/JTest.Cli.Host/ProgramKitGenerated/Evidence/console-command-dispatch.evidence.json),
plus the generated configuration provenance under
[src/JTest.Cli.Host/configuration/](../src/JTest.Cli.Host/configuration/provenance.json).

### Consumption boundary (proven, not promised)

JTest consumes Program Kit **only** through locally prepared NuGet
packages: [tools/prepare-program-kit-feed.ps1](../tools/prepare-program-kit-feed.ps1)
packs an explicit project list with Program Kit's own
`build/ProgramKit.Pack.proj` into `packages/local-feed/`, and
[NuGet.Config](../NuGet.Config) clears inherited sources and maps
`Orbyss.ProgramKit.*` to that feed with package source mapping — the same
isolation flow Program Kit's own consumer proof uses. There is no
`ProjectReference`, source inclusion, file reference, or assembly hint
path into the Program Kit repository; the acceptance test
`ProgramKitConsumptionBoundaryTests` fails the build if one ever appears.

## Website projection assets (source-owned)

| asset | path |
| --- | --- |
| Representative semantic test definition | [docs/assets/example.suite.json](assets/example.suite.json) |
| Representative report (self-contained HTML, includes one honest failure) | [docs/assets/example-report/index.html](assets/example-report/index.html) |
| Canonical evidence behind that report | [docs/assets/example-report/result.json](assets/example-report/result.json) |
| Published contracts | [schemas/](../schemas/) (suite, templates, result, language manifest) |
| End-to-end example family | [examples/orders/](../examples/orders/) |

## Proposed catalog entry (for human review — no Program Kit contract exists yet)

Program Kit currently defines no machine-readable Development Tool or
website catalog contract (its only catalog is the development-capability
index). JTest deliberately does not invent one. If Program Kit adds such a
contract, the following fields map directly onto this page:

```json
{
  "toolId": "jtest",
  "name": "JTest",
  "summary": "Declarative JSON end-to-end API tests with truthful evidence and reports.",
  "category": "testing",
  "maturity": "alpha",
  "version": "2.0.0-alpha.1",
  "repository": "https://github.com/nexxbiz/JTest",
  "canonicalDocumentation": "docs/program-kit-tool.md",
  "gettingStarted": "docs/getting-started.md",
  "contracts": ["schemas/jtest-suite-2.0.0.schema.json", "schemas/jtest-templates-2.0.0.schema.json", "schemas/jtest-result-2.0.0.schema.json", "schemas/jtest-language-manifest-2.0.0.json"],
  "programKitConsumption": { "mode": ["runtime-packages", "build-time-generation"], "pin": "packages/local-feed.manifest.json" },
  "projectionAssets": ["docs/assets/example.suite.json", "docs/assets/example-report/index.html", "docs/assets/example-report/result.json"]
}
```
