# Changelog

All notable changes to JTest and the JTest test-definition language are recorded here.
The language is validated by an authoritative, versioned JSON Schema shipped with the tool
(`jtest validate`), and breaking changes are made deliberately for correctness, clarity, or
security.

## [2.0.0] — unreleased

JTest 2.0 is a hardening + reporting release. It moves JTest into CI/CD pipelines with an honest
exit-code gate and a self-contained, safe HTML report, and formalizes the test-definition language.

### Reliability

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
- **Redacted by default:** `Cookie`/`Set-Cookie`/`Authorization` and declared secret values are
  masked in reports and the trace; all dynamic values are rendered inert (no XSS).

### HTTP

- Response/request **headers are a case-insensitive keyed map**; multi-valued headers (e.g.
  `set-cookie`) expose all values. `$.this.headers['content-type']` now works.
- Response data exposes **`statusCode`** (canonical) and **`status`** (alias).
- **Deterministic per-case cookie sessions:** a login step's cookies are carried to later steps in
  the same test case automatically, isolated between cases, independent of HTTP handler lifetime.

### Language & validation (BREAKING — no external consumers yet)

- `jtest validate` now performs **real JSON Schema validation** with located, machine-readable
  diagnostics, and returns a non-zero exit code when any file is invalid. The previous check was a
  shallow structural probe mislabeled as "schema" validation, and it always exited `0`.
- **Unknown step types and structurally invalid definitions are now rejected.**
- The `while` step now carries an explicit `type: "while"` identifier (previously relied on a
  class-name convention).
- JSONPath is pinned to **RFC 9535** (JsonPath.Net); filter selectors use `?@.expr`. Filter and
  multi-match resolution is guaranteed in `save`, assertions, and interpolation.
- A JSONPath that matches nothing is reported as a distinct diagnostic rather than silently
  coerced to `null`.

### Release

- Version is single-sourced across source/package/tag; the `LICENSE` file is present and matches
  the declared MIT metadata.
