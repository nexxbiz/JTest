# JTest 2.0 release readiness snapshot

Prepared: 2026-07-27, at the completion of implementation plan 1.0.0
(work units JT2-W010 through JT2-W100) on branch `design/jtest-2-review`.

**This document states readiness; it is not a release.** Publishing
JTest 2.0.0 requires a separate human decision and process — the Program
Kit release-cycle capabilities (`release-software`,
`qualify-release-candidate`, `promote-qualified-release`) are unavailable
in its canonical index, and nothing here invents a substitute.

## Versions

| identity | version |
| --- | --- |
| Tool and packages (`Directory.Build.props`, single source) | 2.0.0-alpha.1 |
| Language discriminator | 2.0 |
| Suite schema / templates schema / result schema | 2.0.0 |
| Language manifest | 2.0.0 |
| Program Kit dependency pin (local feed) | commit `b4b14cd88a1e931531cbcdeddc2c2273ad96f4f4`, packages 0.1.0-alpha.1 |

## Package inventory

| package | content |
| --- | --- |
| `JTest.Language` | Language models, embedded schemas and manifest, fail-closed validation, diagnostics. |
| `JTest.Engine` | Execution engine, expressions, assertions, redaction, trace. |
| `JTest.Reporting` | Canonical result writer, report writers, embedded static viewer, result schema. |
| `JTest.Cli` | Command library (run/validate/describe, exit codes, report URL/open). |
| `JTest.Tool` (`GeneratedHost.csproj`, `PackAsTool`, command `jtest`) | The generated console host. |

## Contract artifact digests (exact bytes at this snapshot)

| artifact | sha256 |
| --- | --- |
| schemas/jtest-suite-2.0.0.schema.json | `88be5b7e3840df266d0db0328664ad6d4753442f2239a651be04c461f005f796` |
| schemas/jtest-templates-2.0.0.schema.json | `c5657b90903ed6f8908220ebbed664f78db71164a6d915cd9a3a4ab2a2ecbf3a` |
| schemas/jtest-result-2.0.0.schema.json | `7c4ec525715b699c809ecaf24b0e0752d72bf4c205422d3c5ddcf73b284eb784` |
| schemas/jtest-language-manifest-2.0.0.json | `9b096653fac1d8b5ea24699cfad786b686f228e75822b2c333d86422443e26b1` |

## Verification evidence

- 71/71 tests green across the solution (`dotnet test -c Release`),
  zero warnings under warnings-as-errors with locked-mode-capable restore.
- Fixture coverage: every shipped language diagnostic code fires with its
  exact code and JSON pointer (31 fixtures); the published schemas and the
  native validator agree on every fixture verdict.
- Trace truthfulness: no-false-green tree invariant, iteration
  preservation, skip trails, timeout/cancel outcomes, template ancestry
  paths (unit + acceptance).
- Determinism: byte-identical double serialization of the canonical
  result; byte-identical catalog and standalone report artifacts for
  identical inputs.
- Security: secret-absence sweeps over every evidence node; end-to-end
  acceptance proving a `--secret-env` value echoed by a live local API
  reaches no report artifact and no stdout; viewer static scans (no
  innerHTML/eval, no external requests) plus inline-script breakout
  prevention.
- CLI: process-level exit-code matrix against the generated binary
  (0/1/2 paths, parser diagnostics, standalone artifact, describe
  byte-equality with the embedded manifest); one acceptance test per 1.x
  finding F1–F8; parallel/sequential content equivalence.
- Host generation: regenerating from the committed typed inputs is
  repeatable via `tools/generate-cli-host.ps1`; the generated tree is
  committed and doc-digest-locked (`shell.lock.json`,
  `console-command-dispatch.lock.json`).

## Known limitations (deliberate, documented)

- Secret taint-tracking: values derived from secrets are not tracked;
  documented in the manifest, reporting guide, and agent guide.
- Public step extensibility is deferred (approved decision D4).
- The Program Kit C# source gate is adopted as governance plus
  editorconfig/analyzers, not as the (non-packable) gate analyzer
  (approved decision D2).
- The containerized echo-API acceptance variant was deferred in JT2-W080;
  the in-process API covers the HTTP path end to end.
- exit code 3 (unexpected internal failure) has no dedicated end-to-end
  test; it is guarded by the router's catch-all and the generated host's
  fail-closed dispatcher resolution.
- The human visual-polish checkpoint for the report viewer (JT2-W060)
  remains open: functionality is browser-verified, aesthetics await the
  human's eye.

## Before publishing (human decisions required)

1. Review and merge `design/jtest-2-review`.
2. Close the viewer visual checkpoint.
3. Decide the release version (2.0.0) and version the packages
   accordingly; decide the publishing target and process (out of scope
   here by design).
4. Program Kit is unreleased: publishing JTest packages that depend on
   `Orbyss.ProgramKit.*` 0.1.0-alpha.1 requires either a Program Kit
   release or shipping the pinned dependencies alongside — a human call.
