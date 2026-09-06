# JTest 2.0 Documentation

JTest runs JSON-defined API/integration tests and is published as the `jtest` .NET global tool.
It is built to run in CI/CD pipelines: it fails honestly (a non-zero, class-specific exit code
whenever anything is wrong) and produces one self-contained HTML report you can open offline.

These docs describe the JTest 2.0 system as implemented. The test-definition language is defined by
an authoritative, versioned JSON Schema shipped with the tool and enforced by `jtest validate`.

## Contents

- [Getting started](getting-started.md) — install, write a first suite, run it, read the report.
- [Language reference](language-reference.md) — the test-definition format: suites, cases, datasets,
  steps, assertions, variables, and JSONPath.
- [HTTP steps](http-steps.md) — the HTTP request/response contract, headers, `statusCode`/`status`,
  and deterministic cookie sessions.
- [CLI & exit codes](cli-and-exit-codes.md) — commands, options, and the pipeline exit-code contract.
- [Reporting](reporting.md) — the canonical execution trace and the self-contained HTML report.
- [CI/CD integration](ci.md) — using JTest as a pipeline gate.

## The 30-second version

```bash
dotnet tool install --global JTest.Cli
jtest run "tests/**/*.json" --report report.html --trace trace.json
```

Exit `0` only if every discovered suite ran and every case passed; non-zero (with a specific code)
otherwise. `report.html` is a single self-contained file; `trace.json` is the machine-readable
evidence every report is projected from.
