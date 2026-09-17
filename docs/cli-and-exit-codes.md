# CLI & exit codes

## Exit codes

The process exit code is deterministic and is derived from the canonical execution trace. It is the
gate a pipeline reads.

| Code | Class | Meaning |
|------|-------|---------|
| `0` | success | Everything discovered ran and passed (or validated clean). |
| `1` | test failure | At least one assertion/case failed. |
| `2` | execution error | A suite crashed or failed to load, or discovery matched input but produced zero results. |
| `3` | validation error | A definition failed schema validation. |
| `4` | aborted | The run was cancelled or timed out. |

When more than one class occurs in a run, the reported code follows the precedence
`2 > 3 > 4 > 1`. "No results" is never success.

A suite that fails to load (malformed JSON, an unknown step type or assertion operator) is recorded
as an errored suite rather than aborting the run: the remaining suites still execute, the report and
trace are still written, and the failure is named in both.

## `jtest run`

```
jtest run <paths...> [options]
```

| Option | Description |
|--------|-------------|
| `--report <file>` | Write a self-contained HTML report. |
| `--trace <file>` | Write the canonical execution-trace JSON. |
| `-p, --parallel <n>` | Run suites in parallel (results are equivalent to sequential). |
| `-e, --env <k=v>` | Set an environment variable (repeatable); overrides the suite's own `env`. |
| `--env-file <file>` | Load environment variables from a JSON file; also overrides the suite's `env`. |
| `--globals-file <file>` | Load global variables from a file. |
| `-c, --categories <list>` | Only run the given comma-separated categories. |
| `-o, --output <dir>` | Output directory for the console/markdown report. |
| `--skip-output` | Do not write the default report file. |

The canonical trace is always built in-memory; `--trace`/`--report` persist projections of it.

### Environment variables

Precedence, lowest to highest: the suite's own `env` block → `--env-file` → `-e/--env`. So a
pipeline can point a suite at a dynamically allocated address without rewriting the file:

```bash
jtest run tests/api.suite.json -e "baseUrl=http://localhost:$PORT" -e "apiKey=$API_KEY"
```

Only the first `=` separates key from value, so values may contain `=` (connection strings, base64).
A key given twice takes the last occurrence; an entry with no key (`-e baseUrl`) is a usage error.

## `jtest validate`

```
jtest validate <paths...>
```

Validates each file against the JTest language schema and prints located diagnostics. Exits `0` when
all files are valid, `3` when any file is invalid. Use it as a fast pre-gate in CI.

It also resolves every assertion operator, so a typo such as `"op": "isEqual"` is reported here —
with its location and the supported operators — instead of surfacing later as a suite that fails to
load.

`validate` never green-lights a file `run` cannot load: after the schema passes, the definition is
bound exactly as a run would bind it, and any failure is reported as a diagnostic. A file that
validates clean will load.

## `jtest report`

```
jtest report --trace <file> [--trace <file>...] -o <file> [--merged-trace <file>] [--format html|markdown]
```

Renders a report from execution traces that are already on disk. It runs no tests; it is a pure
projection of the trace, exactly like the report `run` writes.

| Option | Description |
|--------|-------------|
| `--trace <file>` | A trace to render. Repeat the option to merge several runs; inputs are merged in the order given. |
| `-o, --output <file>` | Where to write the report (`--report` is accepted as an alias). |
| `--merged-trace <file>` | Also write the merged canonical trace JSON. |
| `-f, --format <html\|markdown>` | Report format. Defaults to HTML, or Markdown if the output path ends in `.md`. |

Exits `0` when the report is written, `2` when a trace cannot be read or the inputs cannot be
merged. The exit code reports whether the report was produced — the catalog's own result is the
merged trace's `outcome`/`exitCode`, printed in the summary line and shown in the report.

### Why merge

A suite catalog that cannot be one invocation — for example when the system under test has to be
restarted between two groups of suites — produces one trace and one report per invocation. A CI
artifact then holds several reports to download and read separately, and no single answer to "did
the catalog pass". `jtest report` collapses them into one.

With a single `--trace` it simply re-renders that trace, unchanged.

### Merge semantics

The merged root is not a trace of one run, so each field is defined explicitly:

| Field | Merged value |
|-------|--------------|
| `traceSchemaVersion` | Must be identical across inputs. A mismatch is refused, not reconciled. |
| `startedAt` / `endedAt` | Earliest start, latest end — the true outer window. |
| `durationMs` | **The sum of the inputs' durations**, not `endedAt - startedAt`. |
| `counts` | Summed element-wise. |
| `outcome` | The most severe input outcome: `errored` > `timedOut` > `cancelled` > `failed` > `passed`; `skipped` only when every input was skipped. |
| `exitCode` | The most severe input exit code, under the same `2 > 3 > 4 > 1` precedence a single run uses. |
| `suites` | Concatenated in input order, with every id and path namespaced by input. |
| `toolVersion` | The shared version, or every distinct one plus a warning diagnostic. |
| `merge` | A block describing the merge itself (below). Present only on a merged trace. |
| `run` / `environment` | Moved off the root onto `merge.sources[n]`, one set per run. |

Three of these deserve their reasons:

**`durationMs` is a sum.** The wall-clock span `endedAt - startedAt` silently includes the gaps
between invocations — an engine restart, a queued CI step — so a 74-second catalog would be reported
as minutes of testing. The two numbers look identical, so the merged trace separates them
structurally in `merge`:

```jsonc
"merge": {
  "durationSemantics": "sumOfSources",
  "testDurationMs": 453.9,      // time actually spent running tests — equals the root durationMs
  "elapsedMs": 33196.5,         // first run started to last run ended
  "betweenRunsMs": 32742.6,     // restarts and waiting — not testing
  "sources": [ { "index": 0, "idPrefix": "input[0]", "source": "groupA.trace.json",
                 "startedAt": "…", "durationMs": 236.3, "outcome": "passed", "exitCode": 0,
                 "suiteCount": 5, "counts": { }, "run": { }, "environment": { } },
               { "index": 1, "gapBeforeMs": 32742.6, } ]
}
```

The report renders this as a **Merged from N runs** panel — three labelled figures and a row per run,
with the idle time shown between the rows — so nobody has to read a sentence to learn what a number
means. A pipeline reads the same fields directly.

**Suite ids are namespaced by input.** Ids are positional within a run, so the first suite of every
invocation is `suite[0]` and a plain concatenation would always put two rows claiming the same
identity in one report. Each input's nodes are therefore re-rooted under `input[0]`, `input[1]`, …
(`suite[0]` becomes `input[0]/suite[0]`, and its cases, datasets, steps, iterations and assertions
follow). Refusing the collision instead would reject every real merge, since a consumer cannot give
a positional id a different value. The namespace is applied uniformly, not only on collision, so a
node's identity does not depend on what it was merged with; the root diagnostics map each
`input[n]` back to its file.

**`run` and `environment` move onto their run.** They are per-invocation dictionaries, and
presenting one invocation's `$.run` values as the whole catalog's would be false — but dropping them
would lose evidence a suite needs to be reproducible. Each run keeps its own under
`merge.sources[n]`, and the report shows them as a collapsible block per run.

### Ordering

The report can show results two ways, switchable in the page:

- **Chronological** — the order things actually ran: run 1's suites, then run 2's. This is the
  default for a merged report, where the sequence is the point.
- **Failures first** — worst outcome first. The default for a single run, unchanged.

Suites in a merged report carry a `run 1` / `run 2` chip, so a row is attributable at a glance in
either order.

### Schema version

A merged trace declares `traceSchemaVersion` **1.1.0**, the version that adds the optional `merge`
block; an ordinary `jtest run` trace is unchanged at 1.0.0. Inputs to a merge must share a **major**
version — a differing minor is additive and merges cleanly, which is what lets a merged trace be
re-merged with a later run; a differing major is a different document shape and is refused.

## Other commands

- `jtest debug <paths...>` — like `run`, with extra developer detail in the console output.
- `jtest create` / `jtest export` — scaffolding/export helpers.
