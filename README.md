# JTest

JTest is a declarative JSON language and command-line tool for end-to-end
API integration tests: arrange, act, and assert against real HTTP
endpoints, with truthful execution evidence and beautiful static reports.
It is built for both humans and AI agents — the whole language is published
as machine-readable contracts the tool itself emits.

```json
{
  "jtest": "2.0",
  "env": { "baseUrl": "https://api.example.test" },
  "tests": [
    {
      "name": "create order",
      "steps": [
        {
          "type": "http",
          "method": "POST",
          "url": "{{$.env.baseUrl}}/orders",
          "body": { "sku": "widget" },
          "assert": [
            { "op": "equals", "actual": "{{$.this.response.status}}", "expected": 201 }
          ]
        }
      ]
    }
  ]
}
```

```bash
jtest run "tests/**/*.suite.json" --env baseUrl=https://api.example.test
```

Every run writes canonical evidence (`result.json`, schema-published) and a
static HTML report; the report URL is printed as a clickable link and opens
automatically in interactive sessions. Exit codes are frozen and computed
from the evidence: `0` pass, `1` test failures, `2` input/validation,
`3` internal.

## Why 2.0

JTest 2.0 is a ground-up rebuild applying Program Kit engineering rules —
deterministic generation, stable diagnostics, pinned dependencies,
fail-closed validation. The full execution history (every loop iteration,
template invocation, and assertion) is preserved in an immutable trace
whose aggregate outcomes are pure functions of their children: a false
green is impossible by construction. Secrets are redacted when evidence is
captured, so no report can leak them. See [CHANGELOG.md](CHANGELOG.md) and
the approved design in [design/jtest-2.0/](design/jtest-2.0/).

## Documentation

- [Language reference](docs/language/reference.md)
- [CLI reference](docs/cli.md) · [Reports and evidence](docs/reporting.md)
- [Authoring guide for AI agents](docs/agents.md)
- [Diagnostic registry](docs/language/diagnostics.md)
- Runnable examples in [examples/orders/](examples/orders/)

The contracts always win over prose: `jtest describe` emits the language
manifest, and `jtest describe --schema suite|templates|result` emits the
exact embedded JSON Schemas.

## Building from source

Prerequisites: the exact .NET SDK pinned by [global.json](global.json), and
a local Program Kit checkout (unreleased dependency, consumed as locally
built packages — never as project references).

```powershell
# 1. Build the local Program Kit package feed (once per Program Kit pin)
powershell -File tools/prepare-program-kit-feed.ps1 -ProgramKitRoot <path-to-program-kit>

# 2. Restore, build, test
dotnet restore JTest.sln --configfile NuGet.Config
dotnet build JTest.sln -c Release --no-restore
dotnet test JTest.sln -c Release --no-build --no-restore
```

The CLI binary is `src/JTest.Cli.Host/bin/Release/net10.0/GeneratedHost.dll`
(`dotnet <path> run …`); it packs as the `jtest` dotnet tool. The host is
generated from the typed Open Console document in
[hosting/](hosting/) via `tools/generate-cli-host.ps1`.

## Repository layout

- `src/JTest.Language` — the versioned language: models, schemas,
  fail-closed validation, stable diagnostics, agent-facing manifest.
- `src/JTest.Engine` — execution: contexts, expressions, steps,
  assertions, redaction, and the truthful execution trace.
- `src/JTest.Reporting` — canonical evidence and deterministic report
  writers with the static viewer.
- `src/JTest.Cli` — the command library; `src/JTest.Cli.Host` — the
  generated console host that packs as the `jtest` tool.
- `schemas/` — the published contract artifacts; `docs/` — documentation;
  `examples/` — runnable suites; `design/jtest-2.0/` — the approved design
  and plan; `governance/` — the adopted C# source quality gate.

## License

MIT — see [LICENSE](LICENSE).
