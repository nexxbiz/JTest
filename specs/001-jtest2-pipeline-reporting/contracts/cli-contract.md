# Contract: `jtest` CLI Surface (JTest 2.0)

The CLI is the primary integration contract for pipelines. Commands use `Spectre.Console.Cli`.
All commands are deterministic and honor a `CancellationToken` (Ctrl+C / SIGINT / run timeout).

## Exit codes (all commands) — FR-008

| Code | Class | Meaning |
|------|-------|---------|
| 0 | success | Everything discovered ran and passed (or validated clean). |
| 1 | test-failure | At least one assertion/case failed; no execution or validation error. |
| 2 | execution-error | A suite/case crashed, failed to load/deserialize/set up, or discovery matched input but produced zero results. |
| 3 | validation-error | `validate` found an invalid definition, or a run was given a schema-invalid definition. |
| 4 | aborted | Run cancelled or timed out. |

**Precedence** when multiple classes occur in one run: `2 > 3 > 4 > 1`. The chosen code and the
per-class counts are recorded in the trace (`exitCode`, `counts`) and printed in the console summary.

## `jtest run` — execute tests

```
jtest run <paths...> [options]
```

| Option | Type | Default | Behavior |
|--------|------|---------|----------|
| `<paths...>` | glob(s) | required | Test files/globs to discover. |
| `-o, --output <dir>` | path | `artifacts` | Folder for the default report + trace when no explicit `--report`/`--trace` is given. |
| `-f, --output-format <html\|markdown>` | enum | `html` | Format of the default report: HTML (self-contained) or a clean Markdown projection (`report.md`). |
| `--report <file>` | path | `<output>/report.<html\|md>` | Explicit report path (overrides the default location); format from `--report-format`/`-f` or the file extension. |
| `--trace <file>` | path | `<output>/trace.json` | Explicit canonical execution-trace JSON path (FR-009). |
| `--report-format <html\|markdown>` | enum | `html` | Format for an explicit `--report`. |
| `--skip-output` | flag | off | Do not write the **default** report/trace. Explicit `--report`/`--trace` are still written. |
| `--parallel` | flag | off | Execute suites in parallel; results equivalent to sequential (FR-005). |
| `--timeout <duration>` | duration | none | Overall run timeout → `timedOut` + exit 4. |
| `--include-variables` | flag | off | Opt-in env/global/variable dump, values masked (FR-027/28). |
| `--fail-on-empty` | bool | true | Discovery matched input but zero results → exit 2 (FR-003). |

Behavior: the canonical trace is **always built in-memory**; the report and trace persist
projections of it. **By default a run writes exactly two files — `artifacts/report.html` and
`artifacts/trace.json`** (or `report.md` when `-f markdown`). JTest never writes any other report
file; in particular it does **not** dump a per-suite Markdown file into the suite/working folder.
Exit code per the table above. Nothing is dropped: a crashing suite is an `errored` node (FR-002).

## `jtest validate` — validate definitions against the language schema

```
jtest validate <paths...> [--format <text|json>] [--schema-version <v>]
```

- Enforces the versioned JSON Schema (FR-029/30). Emits located, machine-readable diagnostics
  (`--format json`) with JSON Pointer + rule id (FR-031).
- Honest reporting: reported `valid`/`invalid` counts equal the actual numbers; no check is
  mislabeled (FR-032).
- Exit `0` when all valid; `3` when any file is invalid (FR-004).

## `jtest report` — render a report from a saved trace

```
jtest report --trace <file> --report <file> [--report-format <html|markdown>] [--include-variables]
```

- Pure projection: reads a canonical trace JSON and writes a report. Adds/hides nothing beyond an
  explicit view option (Principle I; FR-016/17). Exit `0` on success, `2` on read/render error.

## `jtest debug` — verbose interactive view

Retained as an opt-in verbose console/report view over the same trace. It does **not** change the
default report (which already shows complete nested execution — FR-017); it only adds
extra developer detail. Same exit-code contract.

## `jtest create` / `jtest export`

Existing scaffolding/export commands retained; brought under the same schema (create emits
schema-valid definitions; export documented). Same exit-code contract.

## Invariants tested (integration)

- A corpus with {crash, fail, pass, empty-expected, cancelled, timed-out} yields exit
  {2,1,0,2,4,4} respectively (SC-001/002).
- `--report` output opens with zero network requests (SC-005).
- `validate` over the invalid corpus exits 3 with a located diagnostic per file (SC-003).
