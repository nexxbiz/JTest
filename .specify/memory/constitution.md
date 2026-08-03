<!--
Sync Impact Report
==================
Version change: (template / unversioned) → 1.0.0
Bump rationale: Initial ratification of the JTest 2.0 constitution (first concrete,
  non-template version). MAJOR baseline per semantic-versioning governance policy.

Principles defined (8):
  I.   Evidence Is Canonical, Reports Are Projections
  II.  No False-Green (NON-NEGOTIABLE)
  III. Complete, Faithful Execution History
  IV.  Secure And Redacted By Default
  V.   Formal, Versioned Language Contract
  VI.  Pipeline-First And Deterministic
  VII. Test-Backed Correctness (NON-NEGOTIABLE)
  VIII.Honest, Reconciled Releases

Added sections:
  - Security & Reporting Requirements
  - Development Workflow & Quality Gates
  - Governance

Removed sections: none (template placeholders fully replaced).

Templates requiring updates:
  ✅ .specify/templates/plan-template.md   — Constitution Check gate aligns (principle-agnostic gate retained; no edit required)
  ✅ .specify/templates/spec-template.md   — mandatory sections compatible with principles (no edit required)
  ✅ .specify/templates/tasks-template.md  — task categories cover testing/versioning discipline (no edit required)

Follow-up TODOs: none. RATIFICATION_DATE set to first adoption date 2026-08-03.
-->

# JTest Constitution

<!--
JTest is a .NET (net8.0) tool that executes JSON-defined API/integration tests. It is
published as a dotnet global tool (`jtest`) and runs both interactively and headless in
CI/CD pipelines. This constitution governs JTest 2.0 — a hardening and reporting release
that learns from confirmed defects in the 1.0 line.
-->

## Core Principles

### I. Evidence Is Canonical, Reports Are Projections

A single serializable execution-trace contract (canonical JSON) MUST be the source of truth
for every run. Every human-facing report — HTML, Markdown, console — MUST be a read-only,
deterministic PROJECTION of that trace. A projection MUST NOT be able to add information the
trace does not carry, and MUST NOT hide information the trace does carry except through an
explicitly documented, opt-in view filter.

Rationale: 1.0 embedded presentation logic in the writers, so different outputs disagreed and
nested evidence was reachable only in one format. One canonical trace makes every consumer
(humans, pipelines, agents) see the same truth and makes reports independently verifiable.

### II. No False-Green (NON-NEGOTIABLE)

Any unhandled error, swallowed or suite-level exception, crashed or dropped suite, or failed
validation MUST produce a non-zero process exit code AND appear in the report. Empty, partial,
or missing results MUST NEVER be interpreted as success — "no results" is a failure, not a
pass. Exit codes MUST be deterministic and documented.

Rationale: In 1.0 a suite that threw was silently dropped from the results set, and
`results.All(...)` over the remaining (or empty) set returned exit 0 — a crash read as green.
For a pipeline gate this is the most dangerous possible defect.

### III. Complete, Faithful Execution History

The trace MUST preserve the full ancestry: suite → case → dataset → step → template/loop →
iteration → child-step → assertion. Every node MUST carry a stable execution path/id, a kind,
an ordinal within its parent, an iteration index where applicable, a duration, an outcome, its
diagnostics, and its children. No loop iteration and no nested result may be overwritten,
dropped, or hidden by default.

Rationale: 1.0 sized loop result arrays to the step count rather than iterations × steps, so
only the final iteration survived (with stale/null slots on early exit), and a single flat
step number collided across nesting levels. Truthful numbering and history cannot be
reconstructed after the fact — they must be captured at execution time.

### IV. Secure And Redacted By Default

Reports MUST be safe to publish as pipeline artifacts. All dynamic values MUST be contextually
escaped for their output format (no injection / XSS). Secrets MUST be redacted by default.
Environment, global, and variable dumps MUST be opt-in and masked even when enabled. Masking
and escaping MUST be verified by automated tests.

Rationale: 1.0 injected assertion/error/name values into embedded HTML without escaping,
dumped environment and global variables verbatim in debug reports, and its request-body masking
never fired because it keyed only on header-style names. A report is worthless as a shared
artifact if opening it leaks secrets or executes markup.

### V. Formal, Versioned Language Contract

The JTest test-definition language MUST have an authoritative, versioned JSON Schema and
machine-readable diagnostics. Validation MUST be real (type, discriminator, constraint, and
reference checks — not shallow structural probes) and MUST be able to gate CI with a non-zero
exit on failure. Reporting validity MUST be honest: a tool MUST NOT label a shallow check as
"schema validation" or report counts it does not compute. Compatibility rules between language
versions MUST be explicit.

Rationale: 1.0 called a hand-rolled structural check "JTEST schema" validation, never
incremented its own valid-file counter, and `validate` always exited 0. Agents and humans need
an exact, machine-readable contract to author tests dependably.

### VI. Pipeline-First And Deterministic

JTest MUST be designed to run headless. Sequential and parallel execution MUST be
deterministic in outcome and reporting. Cancellation and timeouts MUST be honored and recorded
as first-class outcomes. The primary shareable report MUST be a self-contained single-file HTML
artifact with no external network or asset dependencies, suitable for CI inspection offline.

Rationale: The delivery goal of 2.0 is running in pipelines and handing humans one HTML file
that fully explains what ran and what happened. That is only trustworthy if execution is
deterministic and the artifact is complete on its own.

### VII. Test-Backed Correctness (NON-NEGOTIABLE)

Every correctness fix and every reporting behavior MUST ship with automated tests. The
following areas MUST have explicit coverage: loop iteration retention, nested/template
ancestry and numbering, cancellation, timeout, parallel execution, process exit codes, output
escaping, and secret redaction. A regression in any of these areas is release-blocking.

Rationale: The 1.0 defects were untested — loops, nesting, exit codes, escaping, and masking
had no assertions guarding them. Fixes without tests will silently regress under a pipeline
workload.

### VIII. Honest, Reconciled Releases

Release metadata MUST be single-sourced and mutually consistent across source, package, and
git tag. The license file MUST exist and match declared package metadata. Releases MUST be
versioned and reproducible, and published version numbers MUST correspond to a matching tagged
commit.

Rationale: 1.0 shipped csproj version 1.0.0 while git was tagged v1.0.3, one project declared
no version at all, and the README linked a LICENSE file that did not exist. Consumers cannot
trust or reproduce a release whose own metadata disagrees with itself.

## Security & Reporting Requirements

- The canonical trace schema MUST be versioned; every emitted trace MUST record the schema
  version, the JTest tool version, and start/end timestamps for suite, case, and step nodes.
- HTML reports MUST be self-contained (inline CSS/JS/assets, no external fetches), failure-first
  (failures surfaced before passing detail), collapsible, searchable/filterable, and accessible
  (keyboard navigable, sufficient contrast, semantic structure).
- HTML reports MUST render the complete nested execution by default; hiding detail is a
  user-chosen view state, never the default and never silent.
- Redaction MUST operate on values (not only on key names) and MUST apply to every projection,
  including request/response bodies, not just headers.
- No dynamic value may reach any output without format-appropriate encoding. Encoding helpers
  MUST be applied uniformly; ad-hoc per-writer escaping is prohibited.

## Development Workflow & Quality Gates

- Work proceeds through the Spec-Driven flow: constitution → specify → (clarify) → plan →
  tasks → implement. Plans MUST include a Constitution Check and MUST NOT proceed while any
  principle is violated without a recorded, justified exception.
- CI MUST fail the build on: any failing test, any non-zero exit from `jtest run` or
  `jtest validate` over the project's own fixtures, a schema-invalid test definition, or a
  version/license metadata inconsistency.
- Reporting and execution-correctness changes MUST include tests as defined in Principle VII
  before merge.
- Any new output format MUST be implemented as a projection of the canonical trace
  (Principle I) and MUST pass escaping/redaction tests (Principle IV) before it is registered.

## Governance

This constitution supersedes other development practices for JTest. Amendments MUST be proposed
in writing, justified against the principles above, reviewed, and recorded with a version bump
and dated entry in this file's Sync Impact Report.

Versioning policy (semantic):
- MAJOR: backward-incompatible governance change, or removal/redefinition of a principle.
- MINOR: a new principle or section, or materially expanded guidance.
- PATCH: clarifications, wording, or non-semantic refinements.

Compliance: all plans and pull requests MUST verify compliance with these principles. The two
NON-NEGOTIABLE principles (II — No False-Green, VII — Test-Backed Correctness) are hard gates:
a change that weakens either MUST be rejected or accompanied by an explicit, time-boxed,
documented waiver. Complexity that appears to conflict with a principle MUST be justified in the
plan's Complexity Tracking section or removed.

**Version**: 1.0.0 | **Ratified**: 2026-08-03 | **Last Amended**: 2026-08-03
