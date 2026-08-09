# Changelog

All notable changes to JTest and the JTest test-definition language are recorded here.
The language is validated by an authoritative, versioned JSON Schema shipped with the tool
(`jtest validate`), and breaking changes are made deliberately for correctness, clarity, or
security.

## [2.0.0] — unreleased

JTest 2.0 is a hardening + reporting release. It moves JTest into CI/CD pipelines with an honest
exit-code gate and a self-contained, safe HTML report, and formalizes the test-definition language.

### Reliability

- **An unresolvable `{{...}}` token in a step field now fails the step instead of silently becoming
  an empty string.** This was a false-positive generator, not a cosmetic issue: a suite that mints a
  per-run unique resource name (e.g. an HTTP route `greet-{{$.random.uuid}}`) instead sent the
  constant `greet-`, so every run targeted the *same* server-side resource. A run could pass while
  being served by a previous run's artifact, with every assertion green — ordinary assertions cannot
  detect this. The rule now covers URLs, methods, headers, query values, body values, file paths, and
  template arguments; assertion and `save` operands were already covered.
- **`$.now` and `$.random` exist.** They were documented as built-in variables but had no
  implementation, so `{{$.now.iso}}` and `{{$.random.uuid}}` expanded to nothing — the direct cause of
  the failure above. See the language reference for the fields and for when to prefer `$.run`.
- **No false-green.** A suite that crashes, an empty-but-expected discovery, an invalid definition,
  a timeout, or a cancellation now produces a non-zero, class-specific exit code and is visible in
  the report. Exit codes: `0` success, `1` test failures, `2` execution error, `3` validation error,
  `4` aborted (cancelled/timed-out). The exit code is derived from the canonical execution trace.
- **Loop iterations are preserved.** `for`/`while` retain every iteration with its own steps
  (previously only the final iteration survived).
- **Cancellation and timeouts** are honored and recorded as distinct outcomes.

### Reporting

- New **self-contained HTML report** (`jtest run --report <file>`): offline, failure-first,
  searchable, keyboard-navigable (WCAG 2.1 AA), rendering the complete nested execution.
- New **canonical execution-trace JSON** (`jtest run --trace <file>`): the versioned source of
  truth every report projects from.
- **Clearer assertions:** each assertion now records and shows its *subject* (the original asserted
  expression, e.g. the JSONPath) and an optional `description`, alongside operation/expected/actual —
  so a passing check reads as what it verified, not just a bare resolved value.
- **JSON body viewer:** request/response bodies render in a collapsible, pretty-printed JSON box with
  a copy button (self-contained, values inert; oversized/binary bodies still follow the truncation rules).
- **Calmer report layout:** a case's single default (unparameterized) dataset is no longer shown as a
  separate level — its steps render directly under the case (data-driven cases still show each dataset) —
  and nested detail uses a thin, downward-fading indentation guide rail instead of a hard border at every
  level, so deep trees stay readable.
- **No more stray Markdown dumps.** `jtest run` no longer writes a timestamped per-suite Markdown file
  (HTML-table soup) into the working folder. By default it writes just `artifacts/report.html` +
  `artifacts/trace.json`; `-f markdown` writes a clean `artifacts/report.md` (a projection of the trace)
  instead of the HTML. Explicit `--report`/`--trace` paths still win. The legacy Markdown writer has been removed.
- **Redacted by default:** `Cookie`/`Set-Cookie`/`Authorization` and declared secret values are
  masked in reports and the trace; all dynamic values are rendered inert (no XSS).

### CLI

- **`-e/--env` works again.** Values passed on the command line were silently dropped and never
  reached `$.env`, so a suite kept whatever its own `env` block declared and a pipeline could not
  retarget it (#74). `-e key=value` is repeatable and now overrides both the suite `env` block and
  `--env-file` (precedence: suite `env` → `--env-file` → `-e`). Only the first `=` separates key
  from value, so values may contain `=`; a key supplied by both `--env-file` and `-e` no longer
  aborts the run; and a malformed entry (`-e baseUrl`) is reported as a usage error instead of
  being ignored.

### HTTP

- Response/request **header keys are normalized to lower case**, so a header is addressable
  regardless of the casing the server sent: `$.this.headers['content-type']` works whether the
  response said `Content-Type` or `content-type`. The map was previously built with a
  case-insensitive comparer, but that comparer does not survive the serialization to JSON that
  precedes JSONPath evaluation, and RFC 9535 name selectors are case-sensitive — so only the
  server's exact casing resolved, and any other casing silently produced an empty string.
  Multi-valued headers (e.g. `set-cookie`) expose all values.
- Response data exposes **`statusCode`** (canonical) and **`status`** (alias).
- **Deterministic per-case cookie sessions:** a login step's cookies are carried to later steps in
  the same test case automatically, isolated between cases, independent of HTTP handler lifetime.

### Language & validation (BREAKING — no external consumers yet)

- `jtest validate` now performs **real JSON Schema validation** with located, machine-readable
  diagnostics, and returns a non-zero exit code when any file is invalid. The previous check was a
  shallow structural probe mislabeled as "schema" validation, and it always exited `0`.
- **Unknown step types and structurally invalid definitions are now rejected.**
- **`jtest validate` no longer green-lights a file `jtest run` cannot load.** The schema describes
  shape, but the runner also binds the definition to its configuration types, and the two could
  disagree — a numeric query value (`{ "query": { "take": 1 } }`) validated clean and then failed
  deserialization at run time. Validation now performs the same binding a run does and reports any
  failure as a diagnostic, so a file that validates will load.
- **Scalar query and header values are accepted.** `{ "take": 1 }` means `take=1`; a query string and
  a header carry text, so a number or boolean is used as its textual form. Only values with no
  unambiguous text form (objects, arrays) are rejected, and now by validation, naming the key.
- **Request `headers` accept the object-map shape** — `{ "X-Tenant": "acme" }` — alongside the
  existing `[{ "name": ..., "value": ... }]` array. Only the array form parsed before, so the natural
  shape (and the one mirroring the response contract) failed at load on a file validate had passed.
- **`jtest validate` now rejects an unknown assertion operator**, with its JSON Pointer location and
  the list of supported operators — previously a typo like `"op": "isEqual"` validated clean and only
  surfaced at run time. Operators are resolved against the runtime's own registry, so validation
  cannot be stricter than execution: case variants such as `notEquals` remain valid.
- **A suite that cannot be loaded no longer aborts the run.** Every discovered file was deserialized
  eagerly, before execution and outside the executor's per-suite boundary, so one malformed
  definition (bad JSON, unknown step type, unknown operator) took down the whole invocation: the
  other suites never ran, no trace or report was written, and the process exited `127` — outside the
  documented exit-code contract. A load failure is now captured as an errored suite: the remaining
  suites still execute, artifacts are still written, the failure is named in the trace and report,
  and the run exits `2` (execution error) as specified.
- An unknown operator encountered at run time now reports the supported operators instead of
  `Type with identifier 'x' is not registered`.
- The `while` step now carries an explicit `type: "while"` identifier (previously relied on a
  class-name convention).
- JSONPath is pinned to **RFC 9535** (JsonPath.Net); filter selectors use `?@.expr`. Filter and
  multi-match resolution is guaranteed in `save`, assertions, and interpolation.
- A JSONPath that matches nothing is reported as a distinct diagnostic rather than silently
  coerced to `null` — now at the point of use, not only in the resolver. An assertion whose
  actual/expected contains an unresolved path fails naming that path (and carries it in the trace as
  an assertion-level diagnostic) instead of comparing a blank value; a `save` from an unresolved path
  records a warning on the step. `exists`/`notexists` are exempt, since for them "matched nothing" is
  the answer being tested. The diagnostic hints at the common JavaScript-isms — `.length`, `.count`,
  `.size` do not exist in RFC 9535 JSONPath — and otherwise at casing.
- **`in` accepts scalar actual values.** `{ "op": "in", "actualValue": "{{$.this.statusCode}}",
  "expectedValue": [200, 201] }` was rejected with "expects a collection or string, but got integer":
  the cardinality guard checked the wrong operand. `in` asks whether a *scalar* actual is one of the
  *expected* values, so it is the expectedValue that must be a collection — which is now what is
  validated, with a clearer error when it is not.
- **`for` accepts an empty item list**, running zero iterations instead of failing validation. The
  natural "clean up whatever is left over" loop has the zero-items case as its normal state.
- **New `$.now` / `$.random` built-ins** — `$.now.iso|date|time|epoch|epochMs` and
  `$.random.uuid|id`, evaluated at each reference. A suite that defines those roots itself keeps its
  own values, and an unknown field under them is an error naming the available fields.
- **New `$.run` variables** — `$.run.id` (short unique token), `$.run.uuid`, `$.run.timestamp`,
  `$.run.epoch`, `$.run.epochMs`. A suite that creates a resource with globally-unique server-side
  identity (e.g. an HTTP route) can now generate a fresh value per run instead of passing once and
  conflicting forever. Values are stable for the whole run, so a create step and a later fetch step
  agree without an intervening `save`, and they are recorded in the trace under `run` so a failed run
  stays reproducible.

### Release

- Version is single-sourced across source/package/tag; the `LICENSE` file is present and matches
  the declared MIT metadata.
