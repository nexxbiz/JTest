# JTest

JTest runs JSON-defined API/integration tests and is published as the `jtest` .NET global tool. It
is built to run in CI/CD pipelines: it **fails honestly** — a non-zero, class-specific exit code
whenever anything is wrong — and produces **one self-contained HTML report** you can open offline.

## Quick start

```bash
dotnet tool install --global JTest.Cli
```

Create `tests/smoke.json`:

```json
{
  "version": "1.0",
  "tests": [
    {
      "name": "gets a user",
      "steps": [
        { "type": "http", "method": "GET", "url": "https://api.example.com/users/1" },
        { "type": "assert", "assert": [
          { "op": "equals", "actualValue": "{{$.this.statusCode}}", "expectedValue": 200 } ] }
      ]
    }
  ]
}
```

Validate and run it:

```bash
jtest validate "tests/**/*.json"
jtest run "tests/**/*.json" --report report.html --trace trace.json
```

The run exits `0` only if everything ran and passed. `report.html` is a single self-contained file;
`trace.json` is the machine-readable evidence every report is projected from.

## Why JTest 2.0

- **No false-green.** A crashing suite, an empty discovery, an invalid definition, a timeout, or a
  cancellation produces a non-zero, class-specific exit code and is visible in the report.
- **Trustworthy report.** A self-contained, failure-first, searchable HTML file that shows the
  complete nested execution (every loop iteration, template, and assertion) — safe to publish, with
  secrets redacted and no active markup.
- **Canonical evidence.** Every report is a read-only projection of one versioned execution trace.
- **Formal language.** The test-definition format has an authoritative, versioned JSON Schema that
  `jtest validate` enforces with located diagnostics.
- **Deterministic HTTP.** Case-insensitive keyed headers, `statusCode`/`status`, and automatic
  per-case cookie sessions.

## Exit codes

| Code | Meaning |
|------|---------|
| `0` | success |
| `1` | test/assertion failures |
| `2` | execution error (crash, bad load, or zero results when files were expected) |
| `3` | validation error (schema-invalid definition) |
| `4` | aborted (cancelled or timed out) |

## Documentation

- [Getting started](docs/getting-started.md)
- [Language reference](docs/language-reference.md)
- [HTTP steps](docs/http-steps.md)
- [CLI & exit codes](docs/cli-and-exit-codes.md)
- [Reporting](docs/reporting.md)
- [CI/CD integration](docs/ci.md)

See also the [CHANGELOG](CHANGELOG.md).

## Build from source

```bash
dotnet build JTest.sln -c Release
dotnet test JTest.sln -c Release
```

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
