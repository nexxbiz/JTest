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

## `jtest run`

```
jtest run <paths...> [options]
```

| Option | Description |
|--------|-------------|
| `--report <file>` | Write a self-contained HTML report. |
| `--trace <file>` | Write the canonical execution-trace JSON. |
| `-p, --parallel <n>` | Run suites in parallel (results are equivalent to sequential). |
| `-e, --env <k=v>` | Set an environment variable (repeatable). |
| `--env-file <file>` | Load environment variables from a file. |
| `--globals-file <file>` | Load global variables from a file. |
| `-c, --categories <list>` | Only run the given comma-separated categories. |
| `-o, --output <dir>` | Output directory for the console/markdown report. |
| `--skip-output` | Do not write the default report file. |

The canonical trace is always built in-memory; `--trace`/`--report` persist projections of it.

## `jtest validate`

```
jtest validate <paths...>
```

Validates each file against the JTest language schema and prints located diagnostics. Exits `0` when
all files are valid, `3` when any file is invalid. Use it as a fast pre-gate in CI.

## Other commands

- `jtest debug <paths...>` — like `run`, with extra developer detail in the console output.
- `jtest create` / `jtest export` — scaffolding/export helpers.
