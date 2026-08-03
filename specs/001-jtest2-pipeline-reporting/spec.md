# Feature Specification: JTest 2.0 — Reliable Pipeline Execution & Trustworthy HTML Report

**Feature Branch**: `001-jtest2-pipeline-reporting`
**Created**: 2026-08-03
**Status**: Draft
**Input**: User description: "JTest 2.0 — Reliable pipeline execution and a trustworthy, self-contained HTML report. Move JTest into CI/CD pipelines: eliminate all known correctness bugs and emit one human-inspectable HTML report that faithfully and safely explains what ran and what happened. One specification, four pillars: (A) execution correctness / no false-green, (B) canonical execution-trace + safe HTML report, (C) security & redaction, (D) formal language contract + honest release."

## Overview

JTest runs JSON-defined API/integration tests and is published as the `jtest` dotnet global tool. JTest 2.0 is a hardening-and-reporting release that prepares JTest to run as a **CI/CD pipeline gate**. It has two non-negotiable goals: (1) the process outcome must be honest — a run that contains anything wrong must fail the pipeline, and a run that is clean must pass; and (2) every run must produce **one self-contained HTML report** that a human can open offline to understand exactly what executed, in what order, nested to any depth, and why it passed or failed — without leaking secrets and without rendering attacker-controlled markup.

This specification defines observable, testable outcomes only. Implementation choices (data structures, libraries, code organization) are deferred to the planning phase.

### Learning from JTest 1.0 (confirmed defects this release must fix)

- A suite that throws during execution is silently dropped from the results and the process still exits `0` — a crash reads as green.
- `validate` always exits `0`, so it cannot gate CI; its "schema" check is only a shallow structural probe and it reports a valid-file count it never computes.
- Loop steps retain only the **final** iteration's inner steps; early exit leaves stale or empty slots. Iteration history is destroyed before any report is rendered.
- Step numbering is a single flat number: template children all report step `1` and loop children reuse the parent's number, so ancestry and ordering cannot be reconstructed.
- Nested/child results are hidden unless a debug flag is set, so the default report can look bare when nested work failed.
- Assertion, error, name, and description values are embedded into report HTML without escaping (injection/XSS); environment and global variables are dumped verbatim; request-body secret masking never actually fires.
- Release metadata disagrees with itself (source version vs git tag) and the README links a `LICENSE` file that does not exist.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pipeline gate never lies (no false-green) (Priority: P1)

A CI/CD pipeline runs `jtest` over a set of test files as a gate. The pipeline author needs the exit code to be an honest verdict: pass only when everything ran and everything passed; fail whenever anything is wrong — a failing assertion, a case error, a suite that crashes, a definition that is invalid, or a discovery that produced no results when files were expected.

**Why this priority**: A test gate that can report success while hiding a crash is worse than no gate at all — it grants false confidence and lets regressions ship. This is the single most important outcome of the release.

**Independent Test**: Run `jtest` against a corpus that includes a crashing suite, a suite with a failing assertion, a valid all-passing suite, and an empty-but-expected discovery. Assert the process exit code for each case matches the documented contract, independent of any report.

**Acceptance Scenarios**:

1. **Given** a suite whose setup/template load throws, **When** the run executes, **Then** the process exits non-zero and the crashed suite appears in the report as failed with its error and diagnostics.
2. **Given** a run where every discovered case passes, **When** the run completes, **Then** the process exits zero.
3. **Given** a run where at least one case fails, **When** the run completes, **Then** the process exits non-zero.
4. **Given** a discovery that matched files but produced zero results, **When** the run completes, **Then** the process exits non-zero (no results is not success).
5. **Given** a `validate` invocation over a set that includes at least one invalid definition, **When** validation completes, **Then** the process exits non-zero.
6. **Given** a run that executes suites in parallel and one parallel suite throws, **When** the run completes, **Then** that failure is present in the results and the process exits non-zero.

---

### User Story 2 - One trustworthy HTML report to inspect a run (Priority: P1)

An engineer opens the HTML report produced by a pipeline run — on a machine with no internet access — and needs to understand the full story of the run: every suite, case, dataset, step, template expansion, loop iteration, nested child step, and assertion, with expected-vs-actual values, timings, and error detail. Failures must be immediately visible; passing detail must be reachable but not in the way.

**Why this priority**: Delivering a report humans can actually inspect is the explicit purpose of the release; without it, pipeline failures are opaque.

**Independent Test**: Execute a run whose canonical result contains nested templates, multi-iteration loops, and mixed pass/fail outcomes; open the resulting HTML file with networking disabled; verify every node present in the canonical result is represented and navigable, that failures are surfaced first, and that search/filter locates a specific assertion.

**Acceptance Scenarios**:

1. **Given** a completed run, **When** the report is generated, **Then** it is a single self-contained HTML file that renders fully with no external network or asset requests.
2. **Given** a run with nested templates and loops, **When** the report opens, **Then** the complete nested execution is shown by default (no debug flag required to see child/iteration detail).
3. **Given** a run with both failures and passes, **When** the report opens, **Then** failures are surfaced before passing detail and the failing paths are expanded or clearly flagged.
4. **Given** a large run, **When** the user searches or filters, **Then** they can locate a specific case, step, or assertion and drill into its expected/actual values, timing, and diagnostics.
5. **Given** any report, **When** navigated with keyboard only, **Then** all detail is reachable and meets basic accessibility expectations (semantic structure, sufficient contrast).
6. **Given** the same run, **When** the canonical result file and the HTML report are compared, **Then** the report neither adds information absent from the canonical result nor omits any node except through an explicit, user-chosen view filter.

---

### User Story 3 - The report is safe to publish as a pipeline artifact (Priority: P2)

Teams attach the HTML report to pipeline runs and share it via artifact stores and chat. The report must be safe to open and share: no secret values visible by default, and no active markup executing when a test's data happens to contain HTML or script.

**Why this priority**: A report that leaks credentials or executes injected script cannot be shared, which defeats its purpose; but it depends on Story 2 existing first.

**Independent Test**: Run a corpus where responses, assertion values, error messages, names, and request/response bodies contain (a) known secret values and (b) HTML/script payloads; generate the report; verify no secret is visible in the default report and no markup executes or renders as live HTML.

**Acceptance Scenarios**:

1. **Given** a test whose assertion actual/expected, error message, name, or description contains HTML or script, **When** the report is generated, **Then** that content is displayed as inert text and no markup executes.
2. **Given** a secret value present in a header, **When** the report is generated, **Then** the value is redacted by default.
3. **Given** a secret value present inside a request or response body (e.g. a JSON field), **When** the report is generated, **Then** the value is redacted by default.
4. **Given** environment and global variables containing secrets, **When** the report is generated in its default mode, **Then** those dumps are not present; and **When** the user explicitly opts in to include them, **Then** secret values within them remain masked.

---

### User Story 4 - Loops, nesting, cancellation, and parallelism are captured faithfully (Priority: P2)

An engineer debugging a flaky integration flow needs to see every iteration of a loop (not just the last), the true parent/child ancestry of template-expanded steps, and clear, distinct outcomes when a run is cancelled or a step times out.

**Why this priority**: These are the exact places where 1.0 lost or corrupted history; faithful capture is what makes Stories 1 and 2 trustworthy.

**Independent Test**: Execute a suite with a for-loop and a while-loop (each with multiple iterations and an early-exit path), a template that expands nested steps, a step that times out, and a run that is cancelled mid-execution; verify the canonical result contains every iteration with per-iteration outcomes, correct ancestry and ordering, and explicit cancelled/timed-out outcomes.

**Acceptance Scenarios**:

1. **Given** a loop that runs N iterations, **When** the run completes, **Then** the result contains all N iterations, each with its own inner-step outcomes; no iteration overwrites another and no stale/empty slots appear.
2. **Given** a loop that exits early on a failing inner step, **When** the run completes, **Then** the executed iterations are present and complete and the unexecuted remainder is not fabricated.
3. **Given** a template that expands child steps and nested templates, **When** the run completes, **Then** every step has a stable identity, a correct parent, and a correct ordinal so numbering and ancestry are unambiguous at every depth.
4. **Given** a step that exceeds its timeout, **When** the run completes, **Then** that step is recorded with a distinct "timed out" outcome (not a generic failure or a pass).
5. **Given** a run cancelled mid-execution, **When** it stops, **Then** in-flight and not-yet-started work is recorded with a distinct "cancelled" outcome and the process exits non-zero.
6. **Given** the same corpus run sequentially and in parallel, **When** both complete, **Then** the set of result nodes and their outcomes are equivalent.

---

### User Story 5 - Author test definitions with confidence (formal schema & honest validation) (Priority: P3)

A test author (human or agent) writes JTest definitions and needs an authoritative, versioned description of the language and a `validate` command that gives precise, honest, machine-readable diagnostics pinpointing what is wrong and where.

**Why this priority**: Dependable authoring reduces invalid definitions reaching the pipeline; it builds on the corrected exit-code behavior from Story 1.

**Independent Test**: Validate a corpus of valid and deliberately invalid definitions (unknown step type, wrong field type, missing required field, bad reference) against the published schema; verify each invalid file is rejected with a diagnostic identifying the offending location, each valid file passes, and the reported counts are accurate.

**Acceptance Scenarios**:

1. **Given** the release, **When** a consumer looks for the language definition, **Then** an authoritative, versioned machine-readable schema for JTest test definitions is available.
2. **Given** a definition with an unknown step type or a wrongly-typed/missing required field, **When** it is validated, **Then** validation fails with a machine-readable diagnostic that identifies the offending location.
3. **Given** a set of valid definitions, **When** validated, **Then** all pass and the reported "valid" count equals the actual number validated.
4. **Given** any validation output, **When** it labels its checks, **Then** the labels are accurate (a structural-only check is never presented as full schema validation) and no reported count is fabricated.

---

### User Story 6 - Honest, reproducible release (Priority: P3)

A consumer installs `jtest` and needs the reported version, the package metadata, and the git tag to agree, and the declared license to actually be present.

**Why this priority**: Trust and reproducibility of the release itself; lower urgency than runtime correctness but required to ship 2.0 credibly.

**Independent Test**: Inspect the built package, the tool's reported version, and the git tag for the release; verify they match and that a `LICENSE` file exists matching the declared license metadata.

**Acceptance Scenarios**:

1. **Given** the JTest 2.0 release, **When** the version is read from source, package metadata, and the git tag, **Then** all three agree.
2. **Given** the repository, **When** a consumer follows the README's license link, **Then** a `LICENSE` file exists and matches the declared license.
3. **Given** the release commit, **When** the package is rebuilt from it, **Then** the produced version corresponds to the tagged commit.

---

### Edge Cases

- **Crashing suite**: template fails to load, definition fails to deserialize, or setup throws → recorded as a failed suite, non-zero exit.
- **Empty discovery**: no files matched vs files matched but all filtered out vs files matched but zero results produced — each has a defined, documented exit code; "matched but zero results" is a failure.
- **Loop early-exit**: inner step fails and breaks the loop → executed iterations preserved, no fabricated remainder, no stale slots.
- **Zero-iteration loop**: loop whose condition/collection yields no iterations → represented explicitly (not as a missing or failed node).
- **Deeply nested templates**: a template that uses another template that contains a loop → ancestry, ordinals, and identities remain correct and unambiguous at every depth.
- **Cancellation mid-run**: user or pipeline cancels → distinct "cancelled" outcome, non-zero exit, partial-but-honest report.
- **Timeout**: step timeout and while-loop timeout → distinct "timed out" outcome.
- **Secret in body / header / query**: redacted by default wherever it appears in the report.
- **Active markup in data**: assertion values, error text, names, descriptions, and bodies containing HTML/script → rendered inert.
- **Non-text / oversized content**: very large or binary/non-UTF-8 response bodies → represented safely without breaking or bloating the report unusably.
- **Schema-invalid definition**: unknown step type, wrong type, missing required field, dangling reference → rejected with a located diagnostic and non-zero exit.
- **Large run**: thousands of nodes → the report remains usable (searchable, navigable) and self-contained.
- **Unicode / non-ASCII content**: preserved and displayed correctly.
- **Version/license mismatch**: caught before release (treated as a release-blocking inconsistency).

## Requirements *(mandatory)*

### Functional Requirements — Execution correctness & exit codes (Pillar A)

- **FR-001**: The process exit code MUST be deterministic and documented. It MUST be success only when every discovered suite executed to completion and every case passed, and failure otherwise.
- **FR-002**: A suite that throws at any point (discovery, load, setup, execution, teardown) MUST be captured as a failed suite in the results and MUST cause a non-zero exit; it MUST NOT be dropped from the results.
- **FR-003**: A discovery that matched input but produced zero results MUST be treated as a failure (non-zero exit). "No results" MUST NOT be interpreted as success.
- **FR-004**: The `validate` command MUST return non-zero when any file is invalid and zero only when all validated files are valid.
- **FR-005**: Parallel execution MUST report the same complete set of results and outcomes as sequential execution for the same input; no result or exception may be lost in the parallel path.
- **FR-006**: Cancellation MUST be honored promptly and recorded as a distinct "cancelled" outcome; a cancelled run MUST exit non-zero.
- **FR-007**: Timeouts (step-level and loop-level) MUST be honored and recorded as a distinct "timed out" outcome, separate from ordinary pass/fail.
- **FR-008**: The exit-code contract and all outcome states MUST be documented for pipeline authors.

### Functional Requirements — Canonical execution trace (Pillars A & B)

- **FR-009**: Each run MUST produce a single canonical, serializable, machine-readable result (the "execution trace") that is the source of truth for the run.
- **FR-010**: The trace MUST be versioned and MUST record the trace-schema version, the JTest tool version, and start/end timestamps at run, suite, case, and step scope.
- **FR-011**: The trace MUST preserve full ancestry: run → suite → case → dataset → step → template/loop → iteration → child-step → assertion.
- **FR-012**: Every trace node MUST carry a stable identity/path, a kind, an ordinal within its parent, an iteration index where applicable, a duration, an outcome (one of: passed, failed, errored, cancelled, timed-out, skipped), its diagnostics, and its children.
- **FR-013**: Every loop iteration MUST be preserved with its own inner-step results; no iteration may overwrite another, and early exit MUST NOT leave stale or empty result slots.
- **FR-014**: Step identity, ordinal, and ancestry MUST be captured at execution time such that numbering and hierarchy are unambiguous at every nesting depth (template-expanded and loop child steps MUST NOT collide on a shared number).
- **FR-015**: Assertion results MUST record operation, expected value, actual value, outcome, and any error/diagnostic detail, associated with their owning step and iteration.

### Functional Requirements — Reporting projections (Pillar B)

- **FR-016**: Every report format (HTML, and any retained Markdown/console output) MUST be a read-only projection of the canonical trace; a report MUST NOT be the source of truth and MUST NOT add information absent from the trace.
- **FR-017**: A report MUST NOT silently hide information present in the trace; any reduction of detail MUST be an explicit, user-selectable view state, and the complete nested execution MUST be shown by default.
- **FR-018**: The primary report MUST be a single self-contained HTML file with no external network or asset dependencies (all styles/scripts/assets inlined), openable offline.
- **FR-019**: The HTML report MUST be failure-first: failures and errored/cancelled/timed-out nodes MUST be surfaced ahead of passing detail and the failing paths made immediately visible.
- **FR-020**: The HTML report MUST be searchable/filterable and allow drill-down from run → suite → case → dataset → iteration → step → assertion, showing expected vs actual, timings, and diagnostics.
- **FR-021**: The HTML report MUST be accessible: keyboard navigable, semantically structured, and meeting sufficient-contrast expectations.
- **FR-022**: The HTML report MUST clearly present rollups (counts and outcomes) at run, suite, case, and dataset scope.
- **FR-023**: The report MUST remain usable and self-contained for large runs (thousands of nodes) and MUST represent oversized or non-text content safely without breaking rendering.

### Functional Requirements — Security & redaction (Pillar C)

- **FR-024**: All dynamic values placed into any report MUST be encoded appropriately for that format so that no attacker-influenced content can produce active markup or injection (no XSS). Encoding MUST be applied uniformly across every value path, not ad hoc per field.
- **FR-025**: Secrets MUST be redacted by default in every report.
- **FR-026**: Redaction MUST match on secret values (not only on key names) and MUST apply wherever the value appears, including request and response bodies and query strings, not only headers.
- **FR-027**: Environment, global, and variable dumps MUST be excluded from the default report and MUST be available only via explicit opt-in.
- **FR-028**: When variable/environment/global dumps are opted in, secret values within them MUST remain masked.

### Functional Requirements — Formal language contract (Pillar D)

- **FR-029**: The release MUST publish an authoritative, versioned, machine-readable schema for the JTest test-definition language, covering step types and their discriminators, field types, required/optional fields, value constraints, and references.
- **FR-030**: Validation MUST enforce the schema (type, discriminator, constraint, and reference checks), not only shallow structural presence checks.
- **FR-031**: Validation diagnostics MUST be machine-readable and MUST identify the offending location within the definition.
- **FR-032**: Validity reporting MUST be honest: no check may be mislabeled (a structural check MUST NOT be presented as full schema validation), and no reported count may be fabricated or left uncomputed.
- **FR-033**: The compatibility relationship between the JTest 2.0 language and existing (1.0) definitions MUST be explicit and documented.

### Functional Requirements — Release integrity (Pillar D)

- **FR-034**: Version metadata MUST be single-sourced and consistent across source, package metadata, the tool's reported version, and the git tag for the release.
- **FR-035**: A `LICENSE` file MUST exist in the repository and MUST match the declared package license metadata, and the README license reference MUST resolve to it.
- **FR-036**: The JTest 2.0 release MUST be versioned and reproducible, corresponding to a matching tagged commit.

### Functional Requirements — Verification discipline (cross-cutting, per constitution)

- **FR-037**: Each correctness and reporting behavior above MUST be covered by automated tests, including at minimum: loop iteration retention, nested/template ancestry and numbering, cancellation, timeout, parallel-vs-sequential equivalence, exit codes, output escaping, and secret redaction.

### Key Entities

- **Run**: one invocation of JTest over a set of discovered inputs; owns overall outcome, timings, tool/schema versions, and the suites executed.
- **Suite Result**: outcome of one test file/suite, including any load/setup/execution error and its cases; a crashed suite is a first-class failed node.
- **Case Result**: outcome of one test case, across its datasets, with steps and rollup outcome.
- **Dataset Result**: outcome of a case executed against one data row/parameter set.
- **Step Node**: a single step execution with identity, kind, parent, ordinal, iteration index (if any), timings, outcome, diagnostics, children; covers ordinary steps, template expansions, and loop constructs.
- **Iteration**: one pass of a loop, owning its own child step nodes and outcome.
- **Assertion Result**: expected value, actual value, operation, outcome, and diagnostics, owned by a step/iteration.
- **Diagnostic**: an error/warning with message and location, attachable to any node.
- **Redaction Rule**: the policy by which secret values (by key and by value) are masked across all projections.
- **Language Schema**: the authoritative, versioned machine-readable description of the JTest test-definition language.
- **Report**: a read-only projection of the trace into a format (HTML primary; Markdown/console secondary).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Across a regression corpus that includes crashing suites, failing assertions, cancellations, timeouts, and empty-but-expected discovery, 100% of runs containing anything wrong exit non-zero — zero false-greens.
- **SC-002**: 100% of clean, all-passing runs in the corpus exit zero — zero false-reds.
- **SC-003**: `validate` exits non-zero for 100% of schema-invalid definitions and produces zero false rejections on the valid corpus.
- **SC-004**: For every run, 100% of the nodes present in the canonical trace are represented in the HTML report, and the report adds no node absent from the trace.
- **SC-005**: The HTML report renders completely with networking disabled, issuing zero external requests.
- **SC-006**: Across an injection corpus (HTML/script in assertion values, errors, names, descriptions, and bodies), zero instances of active markup execution or live-HTML rendering occur.
- **SC-007**: Across a secret-injection corpus (secrets in headers, bodies, and query strings), zero secret values are visible in the default report.
- **SC-008**: For a loop configured to run N iterations, the trace and report contain exactly N iterations, each with its own inner-step outcomes, for 100% of loop test cases (including early-exit cases, which contain exactly the executed count).
- **SC-009**: Step numbering/ancestry is unambiguous: in a deeply nested template+loop corpus, 100% of step nodes have a unique identity and correct parent/ordinal (zero collisions).
- **SC-010**: Running the same corpus sequentially and in parallel yields equivalent trace node sets and outcomes in 100% of comparisons.
- **SC-011**: An engineer can locate the first failing assertion in a large run (≥1000 nodes) within 30 seconds using the report's failure-first ordering and search/filter.
- **SC-012**: For the released package, the version reported by the tool, the package metadata, and the git tag agree (single reconciled value), and a matching `LICENSE` file is present.

## Assumptions

- **Backward compatibility**: JTest 2.0 formalizes and hardens the *existing* language; existing valid 1.0 test definitions are expected to remain valid under the 2.0 schema. Any unavoidable breaking change is documented under FR-033 rather than introduced silently.
- **HTML is the primary shareable report**; existing console summary output is retained, and Markdown output, if kept, is re-expressed as a projection of the canonical trace rather than removed abruptly. Final disposition of Markdown is a planning decision, not a change to the canonical-trace contract.
- **Report emission is on-demand** via the tool's existing output-selection mechanism (a report path/format is requested), and can be made always-on in pipeline usage; the canonical trace is always produced for a run regardless of which human-facing reports are requested.
- **Target runtime** remains .NET (net8.0) and the `jtest` dotnet global tool distribution; no change of platform or distribution channel is in scope.
- **Secret detection** operates on values configured/known to the run (e.g. declared secrets, credentials, tokens) and on standard secret-like keys; it is not expected to detect arbitrary unknown secrets by content heuristics alone, but MUST cover configured secret values wherever they appear (FR-026).
- **Self-contained HTML** implies all assets inlined; this is acceptable for pipeline artifact sizes for the expected run scales (up to low thousands of nodes). Extreme-scale runs may summarize oversized payloads (FR-023).
- **Program Kit is explicitly out of scope** for JTest 2.0; this release is delivered through the standard Spec-Driven flow only.

## Out of Scope

- Redesigning or extending the JTest test-definition language beyond formalizing and validating what exists.
- New step types, new assertion operators, or new protocol support.
- A hosted/service or GUI reporting dashboard (the deliverable is a single static HTML file).
- Historical trend storage or cross-run aggregation across pipeline builds.
- Any dependence on the abandoned Program Kit workflow.
