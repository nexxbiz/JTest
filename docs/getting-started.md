# Getting started

This walkthrough takes you from a clean checkout to your first passing test
and report in about five minutes. JTest 2.0 is not published yet, so you
run it from source.

## 1. Build jtest once

Prerequisites: the exact .NET SDK pinned by [global.json](../global.json)
(`dotnet --version` must print `10.0.302`) and a local Program Kit
checkout.

```powershell
# from the JTest repository root
powershell -File tools/prepare-program-kit-feed.ps1 -ProgramKitRoot ..\program-kit
dotnet restore JTest.sln --configfile NuGet.Config
dotnet build JTest.sln -c Release --no-restore
```

Make a `jtest` shorthand for your shell session:

```powershell
# PowerShell
function jtest { dotnet "$PSScriptRoot\src\JTest.Cli.Host\bin\Release\net10.0\GeneratedHost.dll" @args }
```

```bash
# bash
alias jtest='dotnet "$(pwd)/src/JTest.Cli.Host/bin/Release/net10.0/GeneratedHost.dll"'
```

(Once the `JTest.Tool` package is published, this becomes
`dotnet tool install --global JTest.Tool`.)

## 2. Write your first suite

Create `my-first.suite.json` anywhere:

```json
{
  "jtest": "2.0",
  "info": { "name": "My first suite" },
  "env": { "baseUrl": "https://api.github.com" },
  "tests": [
    {
      "name": "github api is up",
      "steps": [
        {
          "type": "http",
          "id": "meta",
          "method": "GET",
          "url": "{{$.env.baseUrl}}/zen",
          "headers": { "User-Agent": "jtest-getting-started" },
          "assert": [
            { "op": "equals", "actual": "{{$.this.response.status}}", "expected": 200 },
            { "op": "notEmpty", "actual": "{{$.this.response.raw}}" }
          ]
        }
      ]
    }
  ]
}
```

Point it at any HTTP API you like — swap `baseUrl` and the path.

## 3. Validate, then run

```bash
jtest validate my-first.suite.json
jtest run my-first.suite.json
```

The run prints a summary, the canonical evidence path, and the report as a
clickable `file:///` URL — in an interactive terminal the report opens by
itself. Exit code 0 means every case passed; 1 means test failures; 2
means your input was invalid (with `JT****` diagnostics telling you
exactly where); 3 means jtest itself failed.

## 4. Explore the report

`.jtest/reports/index.html` is a persistent catalog: keep the tab open,
rerun tests, press refresh, and the newest run appears at the top. Click a
run for the failure-first trace tree; step into templates, loops, and
dataset runs with the breadcrumbs. The `result.json` next to each run is
the canonical evidence for machines.

Make the assertion fail once (expect `418` instead of `200`) and rerun —
the report shows the failed assertion with actual vs expected, and the
process exits 1.

## 5. Go further

- A realistic example with an auth template, polling `while`, a `for`
  loop, and datasets lives in [examples/orders/](../examples/orders/) —
  the acceptance tests run it against a local API.
- [Language reference](language/reference.md) — every step, scope, and
  assertion operator.
- [CLI reference](cli.md) — all options,
  including `--report standalone` for CI artifacts and `--env`/
  `--secret-env` for credentials that must never reach a report.
- Ask the tool itself: `jtest describe` (language manifest) and
  `jtest describe --schema suite` (the exact JSON Schema).
