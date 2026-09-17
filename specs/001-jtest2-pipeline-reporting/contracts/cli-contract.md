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
| `-e, --env <key=value>` | repeatable | none | Sets `$.env.<key>` for the run, overriding a suite-level `env` entry of the same name (FR-053). |
| `--env-file <file>` | path | none | JSON object of environment variables, same precedence over suite `env`. |
| `--globals-file <file>` | path | none | JSON object of global variables (`$.globals`). |
| `-c, --categories <list>` | csv | all | Only run suites in the given categories. |

**Environment precedence** (FR-053), lowest to highest: suite `env` block → `--env-file` → `-e/--env`.
Repeating `-e` defines several variables; a key given twice takes the last occurrence. Only the
first `=` separates key from value, so values may contain `=`. An entry with no key
(`-e baseUrl`) is a usage error — never a silent drop.

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

## `jtest report` — render a report from saved trace(s)

```
jtest report --trace <file> [--trace <file>...] -o <file> [--merged-trace <file>] [--format <html|markdown>]
```

| Option | Type | Default | Behavior |
|--------|------|---------|----------|
| `--trace <file>` | path, repeatable | required | Canonical execution-trace JSON to render. Repeating it merges the runs in the order given. |
| `-o, --output <file>` | path | required | Report output path. `--report` is an accepted alias. |
| `--merged-trace <file>` | path | none | Also write the merged canonical trace JSON. |
| `-f, --format <html\|markdown>` | enum | `html`, or `markdown` when the output path ends in `.md` | Report format. `--report-format` is an accepted alias. |

- Pure projection: reads canonical trace JSON and writes a report. Adds/hides nothing beyond an
  explicit view option (Principle I; FR-016/17). Exit `0` on success, `2` on read/merge/render error.
  The exit code reports whether the report was produced; the catalog's own result is the merged
  trace's `outcome`/`exitCode`, printed in the summary line.
- **A single `--trace` is a re-render**, byte-identical to rendering that trace directly. Merge
  semantics engage only from two inputs up.

### Merge semantics (two or more `--trace`)

A catalog that cannot be one invocation — the system under test is restarted part-way through —
produces one trace per invocation. The merged root is not a trace of one run, so each field is
defined explicitly rather than inherited from whatever the types make easy:

| Field | Merged value |
|-------|--------------|
| `traceSchemaVersion` | Inputs must share a MAJOR version; a major mismatch is **refused** (exit 2). A minor difference is additive and merges. The merged trace declares `1.1.0`, the version that adds `merge`. |
| `startedAt` / `endedAt` | min / max — the true outer window. |
| `durationMs` | **Sum of the inputs' durations**, not `endedAt - startedAt`. An input with no `durationMs` contributes its own span. |
| `counts` | Element-wise sum. |
| `outcome` | Most severe input outcome, per the parent-node precedence already in the data model: `errored` > `timedOut` > `cancelled` > `failed` > `passed`; `skipped` only when all inputs were skipped. |
| `exitCode` | Most severe input exit code, under this document's own `2 > 3 > 4 > 1` class precedence. |
| `suites` | Concatenated in input order, every id and path namespaced by input (`suite[0]` → `input[0]/suite[0]`, nested nodes follow). |
| `toolVersion` | The shared value, or all distinct values plus a `warning` diagnostic. |
| `diagnostics` | Inputs' root diagnostics, plus a warning if the runs used different tool versions. The merge's own facts are NOT prose here — see `merge`. |
| `merge` | A structured record of the merge: `durationSemantics`, `testDurationMs`, `elapsedMs`, `betweenRunsMs`, and a `sources[]` entry per run (`index`, `idPrefix`, `source`, timings, `outcome`, `exitCode`, `counts`, `gapBeforeMs`, `run`, `environment`). |
| `run`, `environment` | Off the merged root; each run's own values live on its `merge.sources[n]`. |

Three choices carry their rationale in the trace itself, because a reader cannot recover them from
the numbers:

- **`durationMs` is a sum, not a span.** The span silently absorbs the gap between invocations, so a
  74-second catalog would read as minutes. The `merge` block separates the two structurally —
  `testDurationMs`, `betweenRunsMs`, `elapsedMs` — rather than explaining them in a sentence a
  reader must parse and a pipeline cannot read. Both reports render it as a labelled panel.
- **Suite ids are namespaced by input, not refused on collision.** Ids are positional within a run
  (`TraceBuilder.Path`), so every invocation's first suite is `suite[0]`: collisions are the norm,
  not an edge case. Refusing would reject every real merge, since a positional id cannot be given a
  different value by the consumer. Namespacing is uniform rather than collision-triggered, so a
  node's identity does not depend on what it was merged with. Ids that still collide after
  namespacing (a hand-edited trace) are refused. The root diagnostics map each `input[n]` to its file.
- **`run`/`environment` are re-homed rather than picked or dropped.** They are per-invocation
  dictionaries; presenting one invocation's values as the run's would be false, and dropping them
  would lose evidence. Each run's own set lives on its `merge.sources[n]` entry.

An unreadable or malformed `--trace` input is refused (exit 2) rather than skipped: skipping one
would produce a report that looks complete and is quietly missing an invocation's results.

### Report ordering

The report offers two orderings, switchable in the page. **Chronological** follows execution — run 1's
suites, then run 2's — and is the default for a merged report. **Failures first** (FR-019) is the
default for a single run and is unchanged. Chronological order is recovered client-side from the
bracketed indices in each node's path, which are assigned at execution time and survive the
failure-first ordering applied to the embedded trace. Each suite in a merged report carries a
`run n` chip so a row stays attributable in either order.

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
