# JTest 2.0 documentation

- [Getting started](getting-started.md) — from a clean checkout to your
  first passing test and report.
- [Language reference](language/reference.md) — the JTest 2.0 language: suites,
  templates, steps, scopes, expressions, assertions.
- [Diagnostic registry](language/diagnostics.md) — every stable `JT****` code.
- [CLI reference](cli.md) — `jtest run | validate | describe`, options, exit codes.
- [Reports and evidence](reporting.md) — the canonical result document, the
  catalog viewer, and the standalone artifact.
- [Authoring guide for agents](agents.md) — the contract-first workflow for AI
  authoring.
- [JTest as a Program Kit development tool](program-kit-tool.md) — the
  canonical page for website projection: integration mode, exact contracts,
  consumption boundary, and source-owned assets.

The machine-readable contract always wins over prose: `jtest describe` emits
the language manifest, and `jtest describe --schema suite|templates|result`
emits the exact published JSON Schemas embedded in the running tool.
