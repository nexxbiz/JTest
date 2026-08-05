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

## Clarifications

### Session 2026-08-03

- Q: What process exit-code scheme should the pipeline gate use? → A: Distinct, documented codes per failure class — `0` = success, with separate non-zero codes for test/assertion failures, execution/suite errors (crashes), validation failures, and aborted (cancelled or timed-out) runs.
- Q: Is the canonical JSON execution trace a user-facing artifact or internal-only? → A: Always produced internally; emittable to a file on request via a selectable output option (and configurable to always emit in pipeline usage). HTML remains a projection of it.
- Q: How strictly must 1.0 test-definition compatibility be preserved? → A: Clean break allowed. No external consumers exist yet, so breaking changes to the language and its schema are permitted where they improve correctness, clarity, or security; changes are documented but no migration path is guaranteed.
- Q: How are secrets identified for redaction? → A: Redact values explicitly declared/registered as secret AND values under known secret-like keys, matched by value wherever they appear (headers, request/response bodies, query strings).

### Session 2026-08-03 (HTTP hardening finding)

- Q: How should HTTP session state (cookies) persist across steps and isolate across cases? → A: A cookie jar is shared across steps within one execution scope (a test case by default) and isolated between cases/runs; persistence MUST hold regardless of HttpClient handler-pool lifetime and MUST NOT cross-contaminate under parallel execution. A process-wide singleton cookie container is explicitly rejected (it breaks parallel isolation, FR-005).
- Q: How are HTTP response/request headers exposed to tests? → A: As a case-insensitive keyed map (e.g. `$.this.headers['content-type']`); multi-valued headers such as `set-cookie` expose all values (array).
- Q: `status` vs `statusCode` in HTTP response data? → A: Expose both — `statusCode` is the canonical name (chosen on convention/clarity), `status` is a retained alias (existing tests use it); both resolve to the integer HTTP status and are covered by tests.
- Q: Are the `docs/` a source of truth for JTest 2.0? → A: No. `docs/` is legacy 1.0 output; it MUST NOT drive design or be cited as authority. It is fully rewritten from the implemented JTest 2.0 system as the final phase of the plan (FR-044/FR-045).

### Session 2026-08-03 (JSONPath save/resolution finding)

- Q: Does JTest support JSONPath filter expressions (e.g. array filters) in `save`? → A: Yes — save values resolve through the same JSONPath evaluator as assertions/interpolation, and multi-match paths yield an array. 2.0 guarantees filter + multi-match support in `save`, assertions, and interpolation, and pins/documents the exact supported JSONPath dialect. (The precise accepted filter syntax must be verified against the pinned library and documented — do NOT assume Goessner `?()` works verbatim; the pinned library follows RFC 9535, e.g. `$[?@.active==true]`.)
- Q: Should JSONPath property matching be case-insensitive to tolerate camelCase differences? → A: No — matching stays case-sensitive (standard; avoids ambiguous/duplicate matches). Instead, a path that matches nothing MUST surface a distinct, visible diagnostic (not a silent `null`), so casing mismatches are obvious. Verifying an API's actual casing remains the test author's responsibility.

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
5. **Given** any report, **When** navigated with keyboard only, **Then** all detail is reachable and the report meets WCAG 2.1 AA (semantic structure, visible focus, text contrast ≥ 4.5:1).
6. **Given** the same run, **When** the canonical result file and the HTML report are compared, **Then** the report neither adds information absent from the canonical result nor omits any node except through an explicit, user-chosen view filter.
7. **Given** an assertion (passing or failing) and an HTTP step with a JSON body, **When** the report is opened, **Then** the assertion shows what was asserted (its subject expression, operation, expected/actual, and any description) and the body is shown in a collapsible, pretty-printed JSON viewer with a copy button.
8. **Given** a case with a single default (unparameterized) dataset, **When** the report opens, **Then** the redundant dataset level is not shown and the case's steps appear directly under it; a data-driven case with multiple datasets still shows each dataset. Nested detail is indented under a light guide rail rather than a full border at every level.

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

### User Story 7 - Authenticated multi-step HTTP flows work deterministically (Priority: P2)

A tester writes a suite that logs in (the server sets an HttpOnly session cookie) and then calls authenticated endpoints in later steps, expecting the session to be carried automatically — and expecting cases that run in parallel not to leak sessions into one another.

**Why this priority**: Cookie-based authentication is a common real-world flow (e.g. an Elsa server whose `POST /_elsa/identity/login` sets an HttpOnly cookie). Today it works only by accident (the pooled HTTP handler defaults to cookie handling), so it is non-deterministic and unsafe under parallelism.

**Independent Test**: Run a two-step suite (login → authenticated GET) and assert it passes with no manual `Cookie` header, repeated across HTTP handler-pool lifetimes; and run two cases that authenticate as different identities in parallel and assert neither observes the other's cookies.

**Acceptance Scenarios**:

1. **Given** a login step that receives a `Set-Cookie`, **When** a later step in the same case calls a protected endpoint, **Then** the request carries the cookie automatically and succeeds — with no manually specified `Cookie` header.
2. **Given** the same suite run repeatedly, **When** the underlying HTTP handler pool recycles, **Then** the outcome is unchanged (deterministic).
3. **Given** two cases that each log in as a different user run in parallel, **When** they execute, **Then** neither case observes the other's cookies.
4. **Given** a response carrying `Set-Cookie` (possibly multiple), **When** a step reads `$.this.headers['set-cookie']`, **Then** all cookie values are available.
5. **Given** any response, **When** a step reads `$.this.statusCode` or `$.this.status`, **Then** both return the integer status; and `$.this.headers['content-type']` resolves case-insensitively.

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
- **Opaque assertion**: an assertion that only carries a resolved actual value (e.g. `exists`) still shows its subject expression, so the reader can tell what was checked without opening the source suite.
- **JSON body in the report**: a request/response body is pretty-printed and individually collapsible with a copy action; oversized/binary bodies remain governed by the FR-023 truncation/summary rules.
- **Redundant default dataset**: a case with exactly one implicit default dataset renders without the extra dataset level (steps shown directly under the case); no step, assertion, or diagnostic is lost, and the canonical trace still records the dataset node.
- **Deeply nested detail**: nested nodes use a light indentation guide rail rather than a full border at each level, keeping deep trees readable.
- **Non-text / oversized content**: very large or binary/non-UTF-8 response bodies → represented safely without breaking or bloating the report unusably.
- **Schema-invalid definition**: unknown step type, wrong type, missing required field, dangling reference → rejected with a located diagnostic and non-zero exit.
- **Large run**: thousands of nodes → the report remains usable (searchable, navigable) and self-contained.
- **Unicode / non-ASCII content**: preserved and displayed correctly.
- **Version/license mismatch**: caught before release (treated as a release-blocking inconsistency).
- **Cookie session across steps**: a login step sets an HttpOnly cookie → a later step in the same case is authenticated automatically; a different case running in parallel is not.
- **HTTP handler pool recycles mid-run**: cookie/session behavior is unchanged (no dependence on pooled-handler defaults).
- **Multi-valued `Set-Cookie`**: a response with multiple cookies exposes all values via `headers['set-cookie']`.
- **Case-insensitive header lookup**: `headers['Content-Type']` and `headers['content-type']` resolve to the same value.
- **Cookie/authorization in the report**: `Cookie`, `Set-Cookie`, and `Authorization` values are redacted by default in reports and the trace.
- **JSONPath filter in save**: `save: { ids: "{{$.items[?<filter>].id}}" }` saves the array of all matching values; a filter matching exactly one saves the single value.
- **JSONPath filter matches nothing**: saving/asserting a filter (or path) with zero matches yields a distinct "matched nothing" diagnostic — not a silent `null`.
- **Property casing mismatch**: a path like `version.id` against a response serializing `version.Id` matches nothing and is reported as such (JTest does not case-fold); the author fixes the path or the API casing.
- **Path matches an actual null**: distinguished in the diagnostic from a path that matched nothing.

## Requirements *(mandatory)*

### Functional Requirements — Execution correctness & exit codes (Pillar A)

- **FR-001**: The process exit code MUST be deterministic and documented. It MUST be success only when every discovered suite executed to completion and every case passed, and failure otherwise.
- **FR-002**: A suite that throws at any point (discovery, load, setup, execution, teardown) MUST be captured as a failed suite in the results and MUST cause a non-zero exit; it MUST NOT be dropped from the results.
- **FR-003**: A discovery that matched input but produced zero results MUST be treated as a failure (non-zero exit). "No results" MUST NOT be interpreted as success.
- **FR-004**: The `validate` command MUST return non-zero when any file is invalid and zero only when all validated files are valid.
- **FR-005**: Parallel execution MUST report the same complete set of results and outcomes as sequential execution for the same input; no result or exception may be lost in the parallel path.
- **FR-006**: Cancellation MUST be honored promptly and recorded as a distinct "cancelled" outcome; a cancelled run MUST exit non-zero.
- **FR-007**: Timeouts (step-level and loop-level) MUST be honored and recorded as a distinct "timed out" outcome, separate from ordinary pass/fail.
- **FR-008**: The exit-code contract MUST use distinct, documented codes per failure class: `0` = success; a distinct non-zero code for test/assertion failures; a distinct non-zero code for execution/suite errors (crashes); a distinct non-zero code for validation failures; and a distinct non-zero code for aborted (cancelled or timed-out) runs. When a single run exhibits multiple classes, the precedence used to select the reported code MUST be documented. All codes and outcome states MUST be documented for pipeline authors.

### Functional Requirements — Canonical execution trace (Pillars A & B)

- **FR-009**: Each run MUST produce a single canonical, serializable, machine-readable result (the "execution trace") that is the source of truth for the run. The trace MUST always be produced internally and MUST be emittable to a file on request via a selectable output option (with a stable, versioned schema); pipeline usage MAY be configured to always emit it.
- **FR-010**: The trace MUST be versioned and MUST record the trace-schema version, the JTest tool version, and start/end timestamps at run, suite, case, and step scope.
- **FR-011**: The trace MUST preserve full ancestry: run → suite → case → dataset → step → template/loop → iteration → child-step → assertion.
- **FR-012**: Every trace node MUST carry a stable identity/path, a kind, an ordinal within its parent, an iteration index where applicable, a duration, an outcome (one of: passed, failed, errored, cancelled, timed-out, skipped), its diagnostics, and its children.
- **FR-013**: Every loop iteration MUST be preserved with its own inner-step results; no iteration may overwrite another, and early exit MUST NOT leave stale or empty result slots.
- **FR-014**: Step identity, ordinal, and ancestry MUST be captured at execution time such that numbering and hierarchy are unambiguous at every nesting depth (template-expanded and loop child steps MUST NOT collide on a shared number).
- **FR-015**: Assertion results MUST record operation, the asserted subject (the original actual expression, e.g. the JSONPath being evaluated), expected value, actual value, an optional human description (from the assertion's `description`), outcome, and any error/diagnostic detail, associated with their owning step and iteration.

### Functional Requirements — Reporting projections (Pillar B)

- **FR-016**: Every report format (HTML, and any retained Markdown/console output) MUST be a read-only projection of the canonical trace; a report MUST NOT be the source of truth and MUST NOT add information absent from the trace.
- **FR-017**: A report MUST NOT silently hide information present in the trace; any reduction of detail MUST be an explicit, user-selectable view state, and the complete nested execution MUST be shown by default. A purely structural node that carries no information of its own — specifically a case's single implicit/default dataset (default label, no parameters) — MAY be flattened in the projection by rendering its steps directly under the case; this hides no data (every step, assertion, and diagnostic is still shown) and MUST NOT be applied when a case has multiple, named, or parameterized datasets.
- **FR-018**: The primary report MUST be a single self-contained HTML file with no external network or asset dependencies (all styles/scripts/assets inlined), openable offline.
- **FR-019**: The HTML report MUST be failure-first: failures and errored/cancelled/timed-out nodes MUST be surfaced ahead of passing detail and the failing paths made immediately visible.
- **FR-020**: The HTML report MUST be searchable/filterable and allow drill-down from run → suite → case → dataset → iteration → step → assertion, showing expected vs actual, timings, and diagnostics.
- **FR-021**: The HTML report MUST meet WCAG 2.1 AA: keyboard navigable with a visible focus indicator, semantically structured (landmarks/headings), and meeting AA text-contrast ratios (≥ 4.5:1 for normal text, ≥ 3:1 for large text) in both light and dark themes.
- **FR-022**: The HTML report MUST clearly present rollups (counts and outcomes) at run, suite, case, and dataset scope.
- **FR-023**: The report MUST remain usable and self-contained for large runs (thousands of nodes). Request/response bodies larger than a configurable threshold (default 256 KB) MUST be truncated in the report with a clear indicator of truncation and original size; binary or non-UTF-8 content MUST be represented by a summary (content type + byte size), never emitted as raw bytes that break rendering. Truncation affects the projection only; the canonical trace records the original size and a truncation flag.

### Functional Requirements — Security & redaction (Pillar C)

- **FR-024**: All dynamic values placed into any report MUST be encoded appropriately for that format so that no attacker-influenced content can produce active markup or injection (no XSS). Encoding MUST be applied uniformly across every value path, not ad hoc per field.
- **FR-025**: Secrets MUST be redacted by default in every report.
- **FR-026**: Secret identification MUST cover both (a) values explicitly declared or registered as secret and (b) values appearing under known secret-like keys (e.g. token, password, secret, key, credential, authorization, bearer). Identified secrets MUST be matched by value and redacted wherever the value appears — including request and response bodies and query strings — not only by key name and not only in headers.
- **FR-027**: Environment, global, and variable dumps MUST be excluded from the default report and MUST be available only via explicit opt-in.
- **FR-028**: When variable/environment/global dumps are opted in, secret values within them MUST remain masked.

### Functional Requirements — Formal language contract (Pillar D)

- **FR-029**: The release MUST publish an authoritative, versioned, machine-readable schema for the JTest test-definition language, covering step types and their discriminators, field types, required/optional fields, value constraints, and references.
- **FR-030**: Validation MUST enforce the schema (type, discriminator, constraint, and reference checks), not only shallow structural presence checks.
- **FR-031**: Validation diagnostics MUST be machine-readable and MUST identify the offending location within the definition.
- **FR-032**: Validity reporting MUST be honest: no check may be mislabeled (a structural check MUST NOT be presented as full schema validation), and no reported count may be fabricated or left uncomputed.
- **FR-033**: JTest 2.0 MAY introduce breaking changes to the test-definition language and its schema where they improve correctness, clarity, or security. Backward compatibility with 1.0 definitions is NOT required (no external consumers exist yet). Every such breaking change MUST be recorded in a changelog; a guaranteed migration path is NOT required.

### Functional Requirements — Release integrity (Pillar D)

- **FR-034**: Version metadata MUST be single-sourced and consistent across source, package metadata, the tool's reported version, and the git tag for the release.
- **FR-035**: A `LICENSE` file MUST exist in the repository and MUST match the declared package license metadata, and the README license reference MUST resolve to it.
- **FR-036**: The JTest 2.0 release MUST be versioned and reproducible, corresponding to a matching tagged commit.

### Functional Requirements — Verification discipline (cross-cutting, per constitution)

- **FR-037**: Each correctness and reporting behavior above MUST be covered by automated tests, including at minimum: loop iteration retention, nested/template ancestry and numbering, cancellation, timeout, parallel-vs-sequential equivalence, exit codes, output escaping, secret redaction, the HTTP session/cookie and response-contract behaviors (FR-038–FR-043), JSONPath filter/multi-match resolution and unresolved-path diagnostics (FR-046–FR-049), and validation of all documentation examples against the shipped language schema (FR-045).

### Functional Requirements — HTTP step contract & session handling

- **FR-038**: HTTP cookie state MUST persist deterministically across steps within one execution scope (a test case by default) via an explicit, JTest-managed cookie container, independent of the HTTP client's handler-pool lifetime. Relying on `IHttpClientFactory` default cookie behavior is not acceptable.
- **FR-039**: Cookie state MUST be isolated between test cases (and between runs); under parallel execution no case may observe another case's cookies. (Consistency with FR-005.)
- **FR-040**: HTTP response data MUST expose headers as a case-insensitive keyed map addressable by name (e.g. `headers['content-type']`); multi-valued headers (e.g. `set-cookie`) MUST expose all values. Request data SHOULD expose headers the same way.
- **FR-041**: HTTP response data MUST expose the status code under a documented canonical key `statusCode` with a retained alias `status`; both MUST resolve to the integer HTTP status.
- **FR-042**: `Cookie`, `Set-Cookie`, and `Authorization` values MUST be redacted by default in every report and in the persisted trace (a specialization of FR-025/FR-026).
- **FR-043**: Every code path that constructs the HTTP client MUST use the shared, scope-isolated cookie container. (Today `JTest.Cli` registers the HTTP client in two separate service collections — host and CLI; both MUST be reconciled, and no path may fall back to an unmanaged default client.)

### Functional Requirements — Documentation (rewritten from implemented truth)

- **FR-044**: The `docs/` folder MUST be fully rewritten to describe only the JTest 2.0 system as designed and implemented in this feature (canonical trace, exit-code contract, HTTP step/session contract, language schema, redaction/security). All legacy 1.0 assertions, conditions, response shapes, and examples MUST be removed; no document may describe behavior the shipped system does not have. Legacy `docs/` MUST NOT be treated as a source of truth during design or implementation.
- **FR-045**: Every test-definition example and snippet embedded in the rewritten docs MUST validate against the shipped, versioned JTest language schema, and this MUST be enforced in CI so docs cannot drift from the schema.

### Functional Requirements — Value resolution (JSONPath) contract

- **FR-046**: `save`, assertion values, and variable interpolation MUST support JSONPath filter expressions and MUST return/save all matches (an array) when a path matches multiple nodes, using a single, consistent JSONPath evaluator across all three.
- **FR-047**: The supported JSONPath dialect/version MUST be pinned and documented (including the exact filter syntax that is valid), and its filter and multi-match behavior MUST be covered by tests. Documentation MUST NOT promise a syntax the pinned evaluator does not accept.
- **FR-048**: JSONPath property matching MUST remain case-sensitive; JTest MUST NOT silently perform case-insensitive matching (explicit non-goal, to avoid ambiguous or duplicate matches).
- **FR-049**: A JSONPath that matches nothing MUST be recorded as a distinct, visible diagnostic in the trace and report at its point of use (a `save` source or an assertion `actual`/`expected`), distinguishable from a path that matches an actual `null`. An unresolved path MUST NOT be silently coerced to `null` in a way that can mask a failure or produce a misleading pass.

### Functional Requirements — Reporting clarity & ergonomics (added post-implementation)

- **FR-050**: The HTML report MUST make each assertion self-explanatory: it MUST show the asserted subject (the original actual expression, e.g. the JSONPath), the operation, expected and actual values where applicable, and the assertion's optional human `description`. A reader MUST be able to tell WHAT was asserted from a passing assertion, not only its resolved actual value.
- **FR-051**: The HTML report MUST render request/response bodies in a dedicated viewer that pretty-prints JSON (indented), is individually collapsible/expandable, and provides a one-click copy of the body text. This is a projection-layer affordance only: it MUST remain self-contained (no external assets, FR-018) and safe (values rendered inert, FR-024), and MUST NOT override the oversized/binary handling of FR-023.
- **FR-052**: `jtest run` MUST have predictable, tidy file output. By default (no explicit `--report`/`--trace`, and not `--skip-output`) it writes exactly two artifacts into the output directory (default `artifacts/`): the self-contained HTML report (`report.html`) and the canonical trace (`trace.json`). `-f/--output-format markdown` writes a clean Markdown projection (`report.md`) in place of the HTML. JTest MUST NOT write any other report file — in particular it MUST NOT dump a per-suite (timestamped) Markdown file into the suite or working folder. Explicit `--report`/`--trace` paths override the defaults and are always written (even under `--skip-output`). The Markdown report MUST be the trace projection (`MarkdownReportGenerator`); the legacy per-case HTML-table Markdown writer (`src/JTest.Core/Output/`) is removed (R10).

### Key Entities

- **Run**: one invocation of JTest over a set of discovered inputs; owns overall outcome, timings, tool/schema versions, and the suites executed.
- **Suite Result**: outcome of one test file/suite, including any load/setup/execution error and its cases; a crashed suite is a first-class failed node.
- **Case Result**: outcome of one test case, across its datasets, with steps and rollup outcome.
- **Dataset Result**: outcome of a case executed against one data row/parameter set.
- **Step Node**: a single step execution with identity, kind, parent, ordinal, iteration index (if any), timings, outcome, diagnostics, children; covers ordinary steps, template expansions, and loop constructs.
- **Iteration**: one pass of a loop, owning its own child step nodes and outcome.
- **Assertion Result**: operation, asserted subject (the original actual expression), expected value, actual value, optional human description, outcome, and diagnostics, owned by a step/iteration.
- **Diagnostic**: an error/warning with message and location, attachable to any node.
- **Redaction Rule**: the policy by which secret values (by key and by value) are masked across all projections.
- **Language Schema**: the authoritative, versioned machine-readable description of the JTest test-definition language.
- **Report**: a read-only projection of the trace into a format (HTML primary; Markdown/console secondary).
- **HTTP Exchange**: the request/response captured for an HTTP step — method, URL, a case-insensitive keyed header map for request/response headers (multi-valued headers like `set-cookie` expose all values), bodies, and status (`statusCode`/`status`) — with cookie/authorization values redacted.
- **Execution Scope** (also called the session scope): the per-case boundary that owns a cookie container; persists cookies across that scope's steps and is isolated from other scopes.

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
- **SC-013**: A two-step authenticated suite (login → protected endpoint) passes with no manually specified `Cookie` header in 100% of runs, across repeated executions and forced HTTP handler-pool recycling.
- **SC-014**: Two cases authenticating as different identities run in parallel never observe each other's cookies (0 cross-contamination) — and the run's node set/outcomes match the sequential run (consistent with SC-010).
- **SC-015**: For every HTTP response, `statusCode`, `status`, and case-insensitive `headers[...]` access (including multi-valued `set-cookie`) resolve correctly in 100% of contract tests, and `Cookie`/`Set-Cookie`/`Authorization` values are redacted by default (0 leaks).
- **SC-016**: After the documentation rewrite, 100% of test-definition examples in `docs/` validate against the shipped language schema (CI-enforced), and zero references to removed/legacy contract behavior remain.
- **SC-017**: A documented JSONPath filter corpus (single-match, multi-match, and filter selectors) resolves correctly in `save`, assertions, and interpolation in 100% of cases; multi-match filters save the full array.
- **SC-018**: An assertion or `save` referencing a path that matches nothing (e.g. a camelCase mismatch) produces a distinct "path matched nothing" diagnostic in the report in 100% of such cases, with zero occurrences of a silent `null` masking the failure.
- **SC-019**: For 100% of assertions in the report, the asserted subject (and description when provided) is displayed alongside operation/expected/actual, so no assertion is shown as a bare resolved value.
- **SC-020**: For 100% of HTTP steps whose body is JSON, the report renders the body as indented JSON in a collapsible viewer with a working copy control, with zero external asset requests.
- **SC-021**: For 100% of cases that have exactly one default (unparameterized) dataset, the report shows no separate dataset node while still rendering every step of that case; cases with multiple or parameterized datasets show each dataset. No node in the canonical trace is dropped by this projection choice (consistent with SC-004).

## Assumptions

- **Backward compatibility is not required**: there are no external consumers of JTest 1.0 yet, so JTest 2.0 is free to correct flaws in the test-definition language and its schema even where that breaks 1.0 definitions. Breaking changes are made deliberately for correctness/clarity/security and recorded in a changelog (FR-033), not to expand feature scope (see Out of Scope).
- **HTML is the primary shareable report**; existing console summary output is retained, and Markdown output, if kept, is re-expressed as a projection of the canonical trace rather than removed abruptly. Final disposition of Markdown is a planning decision, not a change to the canonical-trace contract.
- **Report emission is on-demand** via the tool's existing output-selection mechanism (a report path/format is requested), and can be made always-on in pipeline usage; the canonical trace is always produced for a run regardless of which human-facing reports are requested.
- **Target runtime** remains .NET (net8.0) and the `jtest` dotnet global tool distribution; no change of platform or distribution channel is in scope.
- **Secret detection** is deterministic: it redacts values declared/registered as secret plus values under standard secret-like keys, matched by value wherever they appear (FR-026). It deliberately does NOT attempt content-heuristic detection of undeclared secrets (e.g. entropy/shape guessing), to avoid false positives; undeclared secrets under non-obvious keys are the author's responsibility to declare.
- **Self-contained HTML** implies all assets inlined; this is acceptable for pipeline artifact sizes for the expected run scales (up to low thousands of nodes). Extreme-scale runs may summarize oversized payloads (FR-023).
- **Program Kit is explicitly out of scope** for JTest 2.0; this release is delivered through the standard Spec-Driven flow only.
- **The existing `docs/` folder is legacy 1.0 output, not a source of truth.** It describes behavior and contracts (assertions, conditions, response shapes) we are intentionally leaving behind. It is regenerated from the implemented system as the final phase (FR-044/FR-045) and must not be cited to justify any 2.0 design decision.

## Out of Scope

- Expanding the language's feature surface: no new step types, new assertion operators, or new protocol support. (Correcting or breaking *existing* definitions/schema for the better is allowed per FR-033; adding new capabilities is not.)
- A hosted/service or GUI reporting dashboard (the deliverable is a single static HTML file).
- Historical trend storage or cross-run aggregation across pipeline builds.
- Any dependence on the abandoned Program Kit workflow.
