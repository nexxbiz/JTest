# Getting started

## Install

```bash
dotnet tool install --global JTest.Cli
```

This provides the `jtest` command.

## Your first suite

A suite is a JSON file with a `version` and a list of `tests` (cases). Each case has a `name` and a
list of `steps`. Save this as `tests/smoke.json`:

```json
{
  "version": "1.0",
  "info": { "name": "Smoke suite" },
  "tests": [
    {
      "name": "gets a user",
      "steps": [
        {
          "type": "http",
          "method": "GET",
          "url": "https://api.example.com/users/1"
        },
        {
          "type": "assert",
          "assert": [
            { "op": "equals", "actualValue": "{{$.this.statusCode}}", "expectedValue": 200 }
          ]
        }
      ]
    }
  ]
}
```

An `http` step performs a request and exposes the response as `$.this` (see
[HTTP steps](http-steps.md)). An `assert` step checks conditions using JSONPath.

## Validate it

```bash
jtest validate "tests/**/*.json"
```

`validate` checks every file against the JTest language schema and exits non-zero if any file is
invalid, with a located diagnostic pointing at the problem.

## Run it

```bash
jtest run "tests/**/*.json" --report report.html --trace trace.json
```

- The process exits `0` only if everything ran and passed (see [exit codes](cli-and-exit-codes.md)).
- `report.html` is a self-contained HTML report — open it in any browser, offline.
- `trace.json` is the canonical execution trace: the machine-readable source of truth.

## Next

- [Language reference](language-reference.md) for the full test-definition format.
- [Reporting](reporting.md) to understand the report and trace.
