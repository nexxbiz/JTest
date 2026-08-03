# CI/CD integration

JTest is designed to run headless as a pipeline gate.

## The pattern

```bash
dotnet tool install --global JTest.Cli
jtest validate "tests/**/*.json"
jtest run "tests/**/*.json" --report artifacts/jtest-report.html --trace artifacts/jtest-trace.json
```

- `validate` fails the pipeline (exit `3`) if any definition is invalid, before you run anything.
- `run` exits non-zero whenever anything is wrong — a failing assertion, a crashing suite, a
  timeout, a cancellation, or zero results from a non-empty discovery. See
  [exit codes](cli-and-exit-codes.md).
- Attach `artifacts/jtest-report.html` as a build artifact; it is a single self-contained file, safe
  to publish (secrets are redacted, no active markup).

## GitHub Actions example

```yaml
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: "8.0.x"
- run: dotnet tool install --global JTest.Cli
- run: jtest validate "tests/**/*.json"
- run: jtest run "tests/**/*.json" --report artifacts/report.html --trace artifacts/trace.json
- uses: actions/upload-artifact@v4
  if: always()
  with:
    name: jtest-report
    path: artifacts/
```

## Why the exit code is trustworthy

The exit code is computed from the canonical execution trace, not from a partial view. A suite that
throws is captured as an errored node (never dropped), "no results" is treated as a failure, and
cancellation/timeout are distinct aborted outcomes. A clean run exits `0`; anything else does not.
