# Changelog

## 2.0.0-alpha.1 (in progress)

JTest 2.0 is a ground-up rebuild on Program Kit engineering rules
(deterministic generation, stable diagnostics, pinned dependencies,
fail-closed validation), replacing version 1.x, which had no external
consumers. The intent is unchanged: declarative JSON end-to-end API tests
that humans and AI agents author, run, and inspect.

### Language

- Formal versioned contract: `"jtest": "2.0"` discriminator, published
  JSON Schemas (suite, templates, result), and an agent-facing language
  manifest, all embedded in the tool (`jtest describe`).
- Fail-closed everything: closed document shapes, three-layer validation,
  stable append-only `JT****` diagnostics with JSON pointers, unresolvable
  expressions fail the step instead of becoming null.
- Formalized semantics: scope visibility (templates read env/globals,
  export only via `output`), `$.this` transparency of assert/wait,
  explicit save targets, no token nesting, resolved values never
  re-interpreted (closes the 1.x expression-injection channel).

### Execution and evidence

- Truthful execution trace: every suite, case, dataset run, step, template
  invocation, loop iteration, and assertion is a node with a stable path,
  ordinal, outcome, timing, diagnostics, and redacted evidence. Aggregate
  outcomes are pure functions of children — the 1.x false-green and
  lost-iteration defects are impossible by construction.
- Canonical result document (RFC 8785 canonical bytes, digest-derived
  runId) as the single source of truth for every report.
- Capture-time structural redaction: declared secrets, `${NAME}`
  substitutions, and credential headers never enter the trace; assertion
  evidence redacts actual and expected operands.

### Reports

- Static catalog viewer (hand-authored HTML/CSS/JS, no build toolchain, no
  external requests): chronological run catalog, failure-first collapsible
  trace tree, step-into with breadcrumbs, hash deep links, light/dark.
- Deterministic writers: identical evidence yields byte-identical
  artifacts. Standalone single-file mode for pipelines.

### CLI

- The host is generated from a typed Open Console document through the
  backed Program Kit console generation; the grammar and exit map are
  reviewable artifacts.
- Frozen exit codes computed from evidence: 0 pass, 1 test failures,
  2 usage/input/validation, 3 internal.
- The report URL is always printed as a clickable `file:///` link;
  interactive sessions auto-open it (failure is only a warning).
- Removed from 1.x: `debug` (the trace is always complete), `create`, and
  the stub `export` command that falsely reported success.

### Breaking

Everything — JTest 2.0 documents declare `"jtest": "2.0"` and 1.x
documents are not accepted. There is no migration tooling by decision.
