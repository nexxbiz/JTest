# jtest CLI reference

The `jtest` host is generated from a typed Open Console document
([hosting/inputs/open-console.json](../hosting/inputs/open-console.json))
through the backed Program Kit console generation, so the grammar below is
locked to a reviewable artifact. `jtest --help` and `jtest --complete` are
generated from the same document.

## Exit codes (frozen)

| code | meaning |
| --- | --- |
| 0 | Every discovered suite produced a complete passing trace. |
| 1 | At least one case failed, errored, timed out, or was cancelled. |
| 2 | Usage, input, discovery, or validation failure. |
| 3 | Unexpected internal failure. |

The run exit code is computed from the canonical evidence itself: there is
no path to exit 0 without a complete passing trace for every discovered
suite.

## jtest run

```bash
jtest run "tests/**/*.suite.json" --env baseUrl=https://api.example.test
```

Arguments: one to sixty-four glob patterns (a leading `!` excludes).

| option | meaning |
| --- | --- |
| `--env-file <file>` | JSON object merged into `env` (CLI wins over the suite). |
| `--env, -e k=v` | One env value; repeatable; splits on the first `=`. |
| `--globals-file <file>` | JSON object merged into `globals`. |
| `--secret-env <name>` | Marks a CLI env entry sensitive; repeatable. |
| `--report catalog\|standalone` | Report mode (default `catalog`). |
| `--report-dir <dir>` | Catalog directory (default `.jtest/reports`). |
| `--report-out <dir>` | Standalone output directory. |
| `--parallel, -p <n>` | Maximum suites executed concurrently. |
| `--timeout <ms>` | Overall run timeout. |
| `--open` / `--no-open` | Force or suppress opening the report page. |
| `--diagnostics text\|json` | Diagnostic output format. |

Every run prints the report location as a clickable `file:///` URL and the
canonical evidence path. In interactive sessions the report opens
automatically; failure to open is a warning only and never changes the
exit code. Auto-open is suppressed when `CI` is set or output is
redirected.

## jtest validate

```bash
jtest validate "tests/**/*.suite.json" --diagnostics json
```

Validates without executing. Exit 0 when every file is valid, otherwise 2,
with every finding reported as a stable `JT****` diagnostic.

## jtest describe

```bash
jtest describe                       # the agent-facing language manifest
jtest describe --schema suite       # published suite JSON Schema
jtest describe --schema templates   # published template-file JSON Schema
jtest describe --schema result      # published result-document JSON Schema
jtest describe --schema suite --output jtest-suite.schema.json
```

Emits the exact contract artifacts embedded in the running tool.
