# JTest C# build gate

Establishment record for `pkid:gate:jtest:csharp` (work unit JT2H-W010 of
the approved host-rebuild plan). The gate selects exactly one analyzer
component: the public Program Kit generated-source contract analyzer
(`Orbyss.ProgramKit.GeneratedSourceContract.Analyzers` 0.1.0-alpha.1,
diagnostic `PKCC001`), packed from the pinned Program Kit checkout into
`packages/local-feed` and attached to `src/JTest.Cli.Host` only through the
consumer-owned `Directory.Build.targets` opt-in
(`ProgramKitGeneratedSourceContract=1.0.0`).

## Files

- `definition.json` — CSharpBuildGateDefinitionDocument 1.0.0; passes the
  backed `csharp-gate validate-definition`.
- `bind-request.json` — CSharpGateBindRequest replayed by
  `csharp-gate bind`; re-running it against an unchanged tree reproduces
  `selection-lock.json` byte-for-byte.
- `selection-lock.json` — canonical lock emitted by `csharp-gate bind`
  (digest-checks every local asset before serializing).
- `verify-request.json` — CSharpGateVerificationRequest executed by
  `csharp-gate verify` (pinned SDK 10.0.302, `dotnet build --no-restore`,
  work-unit boundary and profile).
- `activation-evidence.json` — evidence emitted by the verify harness:
  `succeeded: true`, normalized-output digest, participation-receipt digest,
  analyzer-package asset-hygiene digest.

## Conventions this gate fixes

- `definition.json#/revisionDigest` is the sha256 of the definition file
  bytes with the `revisionDigest` value set to sixty-four zeros; every
  document that references the definition (the selection lock's
  `gateDefinition`) uses that digest.
- The lock's `inputDigest` is the sha256 of the newline-terminated lines
  `<repositoryRelativePath> <digest>` over the six lock inventories in
  document order; `outputDigest` is the harness `outputDigest` of the
  establishing verify run.
- The participation-receipt nonce is deterministic: the first 32 hex
  characters of `sha256("pkid:gate:jtest:csharp")` =
  `b6c86d7dc7d1011c9c23ef55de0e2325`. The analyzer's same-assembly receipt
  generator emits the receipt source under
  `obj/ProgramKitCompilerGenerated/.../ProgramKitGenerated/PublicAnalyzerReceipt/`,
  which itself satisfies PKCC001.

## Activation proof

Enforcement was probed, not assumed: a temporary
`src/JTest.Cli.Host/TamperProbe.cs` carrying the exact ownership header
outside `ProgramKitGenerated/` failed the build with `error PKCC001`
(2026-07-28); removing the probe restored a zero-warning build. The
inverse direction (generated path without header) is covered by the
analyzer's biconditional check and the upstream fixtures.

## Rebind boundary

The inventories bind the current, dispatcher-era generated host. Work unit
JT2H-W030 replaces that tree; its completion includes re-running
`csharp-gate bind` (and verify) against the regenerated inventory so the
lock never lies about bytes on disk.
