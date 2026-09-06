# Quickstart: JTest 2.0 in a CI/CD Pipeline

Goal: run JTest as a pipeline gate that **fails honestly** and produces **one self-contained HTML
report** (plus an optional machine-readable trace) that a human can open offline.

## Install

```bash
dotnet tool install --global JTest.Cli   # provides the `jtest` command
```

## Run as a gate (typical pipeline step)

```bash
jtest run "tests/**/*.json" --report artifacts/jtest-report.html --trace artifacts/jtest-trace.json
```

- Exits **0** only if every discovered suite ran and every case passed.
- Exits non-zero on any failure — and the code tells you *why*:

| Exit | Meaning | Pipeline reading |
|------|---------|------------------|
| 0 | all passed | gate green |
| 1 | test/assertion failures | tests failed — see report |
| 2 | execution error (crash, bad load, or zero results when files were expected) | JTest couldn't trust the run |
| 3 | validation error (schema-invalid definition) | fix the test definition |
| 4 | aborted (cancelled or timed out) | run did not finish |

Attach `artifacts/jtest-report.html` as a build artifact — it is a single file with no external
dependencies, safe to open offline and to share.

## Validate definitions before running (optional pre-gate)

```bash
jtest validate "tests/**/*.json" --format json
```

Exits **3** if any file is invalid, with located diagnostics (JSON Pointer + rule id).

## Render a report later from a saved trace

```bash
jtest report --trace artifacts/jtest-trace.json --report artifacts/jtest-report.html
```

The report is a pure projection of the trace — it adds and hides nothing (except explicit view
toggles).

## What the HTML report gives you

- **Failure-first**: failed / errored / cancelled / timed-out nodes surface before passing detail.
- **Complete nesting by default**: every suite → case → dataset → step → template → loop
  → iteration → child-step → assertion, with expected vs actual, timings, and diagnostics.
- **Searchable / collapsible / keyboard-navigable**, light and dark.
- **Safe**: all values are HTML-encoded (no XSS), and secrets are redacted by default — in headers,
  bodies, and query strings. Variable/environment dumps appear only with `--include-variables`
  and stay masked.

## Options worth knowing

| Option | Effect |
|--------|--------|
| `--parallel` | run suites in parallel (results equivalent to sequential) |
| `--timeout <duration>` | overall run timeout → exit 4 |
| `--fail-on-empty` (default on) | zero results from a non-empty discovery → exit 2 |
| `--include-variables` | opt in to masked env/global/variable dumps in the report |
| `--report-format markdown` | emit the Markdown projection instead of HTML |

## Local developer loop

```bash
jtest run "tests/smoke/*.json" --report out/report.html
# open out/report.html in a browser (works offline)
```
