# JTest

JTest is a declarative JSON language and command-line tool for end-to-end
API integration tests: arrange, act, and assert against real HTTP endpoints,
with truthful execution evidence and human-readable reports.

**Status: JTest 2.0 is being rebuilt from the ground up** on the approved
design in [`design/jtest-2.0/`](design/jtest-2.0/), applying Program Kit
engineering rules (deterministic generation, stable diagnostics, pinned
dependencies, fail-closed validation). Version 1.x has been removed from the
working tree (it remains in git history) and had no external consumers.

## Building

Prerequisites: the exact .NET SDK pinned by [`global.json`](global.json),
and a local Program Kit checkout (unreleased dependency, consumed as locally
built packages — never as project references).

```powershell
# 1. Build the local Program Kit package feed (one-time per Program Kit pin)
powershell -File tools/prepare-program-kit-feed.ps1 -ProgramKitRoot <path-to-program-kit>

# 2. Restore, build, test
dotnet restore JTest.sln --configfile NuGet.Config
dotnet build JTest.sln -c Release --no-restore
dotnet test JTest.sln -c Release --no-build --no-restore
```

The feed's exact package digests and source commit are recorded in
[`packages/local-feed.manifest.json`](packages/local-feed.manifest.json).

## Repository layout

- `src/JTest.Language` — the versioned JTest language: models, JSON Schema,
  fail-closed validation, stable diagnostics, agent-facing manifest.
- `src/JTest.Engine` — execution: contexts, expressions, steps, assertions,
  and the truthful execution trace.
- `src/JTest.Reporting` — canonical result evidence and deterministic report
  writers with the static HTML viewer.
- `src/JTest.Cli` — the `jtest` dotnet tool.
- `tests/` — mirrored test projects plus acceptance fixtures.
- `design/jtest-2.0/` — the approved design, plan, and findings register.
- `governance/` — the adopted C# source quality gate.

## License

MIT — see [LICENSE](LICENSE).
