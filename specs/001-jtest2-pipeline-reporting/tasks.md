# Tasks: JTest 2.0 — Reliable Pipeline Execution & Trustworthy HTML Report

**Input**: Design documents from `specs/001-jtest2-pipeline-reporting/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: REQUIRED. Constitution Principle VII (Test-Backed Correctness) is NON-NEGOTIABLE and
FR-037 mandates coverage for loops, nesting, cancellation, timeout, parallel, exit codes, escaping,
redaction, HTTP session/contract, JSONPath resolution, and doc-example validation. Test tasks are
therefore included in every story and are release-blocking.

**Organization**: Tasks are grouped by user story (spec.md priorities) for independent
implementation and testing. Target framework: C#/.NET 8.0; test framework: xUnit.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1–US7 map to spec.md user stories; Setup/Foundational/Docs/Polish carry no story label
- All paths are repository-relative

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Clean the tree and establish build/version/test scaffolding.

- [X] T001 Remove stale build cruft from the abandoned attempt: `src/.program-kit-build/`, and the `bin/obj`-only dirs `src/JTest.Engine/`, `src/JTest.Evidence/`, `src/JTest.Language/`, `src/JTest.Reporting/`, and `src/JTest.Cli/obj/Generated/` (verify none are git-tracked before deleting)
- [X] T002 Add root `Directory.Build.props` with a single-sourced `<Version>2.0.0</Version>` applied to all projects
- [X] T003 [P] Remove per-project `<PackageVersion>` from `src/JTest.Core/JTest.Core.csproj` and confirm `src/JTest.Cli/JTest.Cli.csproj` inherits the shared version
- [X] T004 [P] Add `LICENSE` (MIT, NexxBiz) at repo root and confirm the `README.md` license link resolves to it
- [X] T005 [P] Add `JsonSchema.Net` PackageReference to `src/JTest.Core/JTest.Core.csproj`
- [X] T006 [P] Configure embedded-resource plumbing in `src/JTest.Core/JTest.Core.csproj` for the language schema and HTML/CSS/JS report assets
- [X] T007 [P] Create test folders in `tests/JTest.UnitTests/`: `Tracing/`, `Execution/`, `Reporting/`, `Security/`, `Http/`, `Language/`, `Fixtures/`, `golden/`
- [X] T008 [P] Implement a golden-file test helper (normalize + compare JSON/HTML) in `tests/JTest.UnitTests/Fixtures/GoldenFile.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The canonical trace, exit-code service, and value pipeline that every story projects
from. **⚠️ No user story can begin until this phase is complete.**

- [X] T009 [P] Define `Outcome` enum + aggregation rules (`errored>timedOut>cancelled>failed>passed`, skipped rule) in `src/JTest.Core/Tracing/Outcome.cs`
- [X] T010 [P] Define `Rollup` value object in `src/JTest.Core/Tracing/Rollup.cs`
- [X] T011 [P] Define `Diagnostic` record (severity, message, location, exceptionType, stackTrace) in `src/JTest.Core/Tracing/Diagnostic.cs`
- [X] T012 Define trace node records (`ExecutionTrace`, `SuiteResult`, `CaseResult`, `DatasetResult`, `StepNode`, `Iteration`, `AssertionResult`, `HttpExchange`, `HeaderMap`) with id/path/kind/ordinal/iteration/timings/children in `src/JTest.Core/Tracing/` (depends on T009–T011, matches data-model.md)
- [X] T013 Implement `TraceBuilder` (stable ids/paths, ordinals, timings, counts aggregation) in `src/JTest.Core/Tracing/TraceBuilder.cs` (depends on T012)
- [X] T014 Implement canonical JSON serialization (System.Text.Json, `traceSchemaVersion`+`toolVersion`, stable property names, deterministic ordering) in `src/JTest.Core/Tracing/TraceJson.cs` (depends on T012)
- [X] T015 [P] Contract test: a built trace serializes and validates against `specs/001-jtest2-pipeline-reporting/contracts/execution-trace.schema.json` in `tests/JTest.UnitTests/Tracing/TraceSchemaTests.cs`
- [X] T016 Implement `ExitCodeService` (Outcome→code; 0/1/2/3/4 with precedence `2>3>4>1`) in `src/JTest.Core/Execution/ExitCodeService.cs` (depends on T009)
- [X] T017 [P] Unit test `ExitCodeService` mapping + precedence in `tests/JTest.UnitTests/Execution/ExitCodeServiceTests.cs`
- [X] T018 Implement `ReportValuePipeline` (redact-by-value+key, then contextual encode; HTML + Markdown encoders) in `src/JTest.Core/Reporting/ReportValuePipeline.cs`
- [X] T019 Rework `SecurityMasker` to register/replace secret **values** (declared + secret-like keys) across headers, bodies, and query strings in `src/JTest.Core/Security/SecurityMasker.cs` (depends on T018)
- [X] T020 [P] Unit tests for encode + value-based redaction (incl. Cookie/Set-Cookie/Authorization, JSON body secrets) in `tests/JTest.UnitTests/Security/RedactionTests.cs`

**Checkpoint**: Trace model, exit codes, and redaction pipeline exist and are tested.

---

## Phase 3: User Story 1 — No false-green pipeline gate (Priority: P1) 🎯 MVP

**Goal**: The process outcome is honest — non-zero whenever anything is wrong; empty results are not success.

**Independent Test**: Run a corpus {crash, fail, pass, empty-expected} and assert exit codes {2,1,0,2}; run `validate` over an invalid corpus and assert exit 3.

### Tests for User Story 1

- [X] T021 [P] [US1] Integration test: a suite that throws → captured as `errored` node + process exit 2 in `tests/JTest.UnitTests/Execution/FalseGreenTests.cs`
- [X] T022 [P] [US1] Integration test: empty-but-expected discovery → exit 2; all-pass → 0; any case failure → 1 in `tests/JTest.UnitTests/Execution/ExitCodeIntegrationTests.cs`
- [X] T023 [P] [US1] Test: `validate` over an invalid corpus → exit 3 with honest counts in `tests/JTest.UnitTests/Execution/ValidateExitTests.cs`
- [X] T024 [P] [US1] Test: unresolved JSONPath → distinct "matched nothing" diagnostic (vs matched-null), no silent null in `tests/JTest.UnitTests/Execution/UnresolvedPathTests.cs`

### Implementation for User Story 1

- [X] T025 [US1] Rework `JTestSuiteExecutor` to catch suite/case-level exceptions and record them as `errored` `SuiteResult`/`CaseResult` nodes (never drop; include in results) in `src/JTest.Core/Execution/JTestSuiteExecutor.cs`
- [X] T026 [US1] Treat matched-but-zero-result discovery as failure (`--fail-on-empty` default true) in the executor / `src/JTest.Cli/Commands/RunCommand.cs`
- [X] T027 [US1] Wire `RunCommand` exit code through `ExitCodeService` (replace `results.All(...)` logic) in `src/JTest.Cli/Commands/RunCommand.cs`
- [X] T028 [US1] Fix `ValidateCommand` to return non-zero on any invalid file and report honest counts in `src/JTest.Cli/Commands/ValidateCommand.cs`
- [X] T029 [US1] Emit a distinct diagnostic when a JSONPath matches nothing, distinguishable from a matched `null`, in `src/JTest.Core/Utilities/VariableInterpolator.cs` and `src/JTest.Core/Steps/StepProcessor.cs` (FR-049)
- [X] T089 [US1] `-e/--env` reaches `$.env` (FR-053): bind the repeatable option to `string[]` (`Spectre.Console.Cli` never binds an `IEnumerable<string>`, so every value was dropped — issue #74), split on the first `=` only, reject a keyless entry as a usage error, and merge `--env-file` first with `-e` laid over it so the command line wins instead of throwing on a duplicate key. Files: `src/JTest.Cli/Settings/RunCommandSettings.cs`, `contracts/cli-contract.md`; regression tests driving the real parser in `tests/JTest.UnitTests/Cli/EnvironmentVariableOptionTests.cs`

**Checkpoint**: A crash/empty/invalid run can never exit 0. MVP deliverable.

---

## Phase 4: User Story 2 — One trustworthy self-contained HTML report (Priority: P1)

**Goal**: A single offline HTML file that faithfully shows the whole run, failure-first.

**Independent Test**: From a trace fixture with nested templates/loops and mixed outcomes, generate HTML with networking disabled; assert every trace node is present and a specific assertion is findable.

### Tests for User Story 2

- [X] T030 [P] [US2] Golden-file test: trace fixture → HTML; assert 100% of trace nodes represented in `tests/JTest.UnitTests/Reporting/HtmlReportGoldenTests.cs`
- [X] T031 [P] [US2] Offline test: generated HTML references no external URLs (regex/asset scan) in `tests/JTest.UnitTests/Reporting/SelfContainedTests.cs`
- [X] T032 [P] [US2] Accessibility/search test: WCAG 2.1 AA checks — semantic landmarks/headings, visible focus, AA text-contrast (≥4.5:1) in light and dark, all detail reachable by keyboard, search locates a node in `tests/JTest.UnitTests/Reporting/HtmlAccessibilityTests.cs`
- [X] T033 [P] [US2] Test: in a ≥1000-node report, failure-first ordering + search surface the first failing assertion within the SC-011 target (assert failing nodes precede passing detail and are directly locatable) in `tests/JTest.UnitTests/Reporting/FailureFirstNavigationTests.cs` (covers SC-011)
- [X] T034 [P] [US2] Test: oversized body is truncated with indicator + recorded original size; binary/non-UTF-8 body is summarized, not emitted raw in `tests/JTest.UnitTests/Reporting/OversizedContentTests.cs` (FR-023)

### Implementation for User Story 2

- [X] T035 [US2] Implement `HtmlReportGenerator` projecting a trace into one self-contained file (inlined assets, trace as inert JSON island) in `src/JTest.Core/Reporting/Html/HtmlReportGenerator.cs`
- [X] T036 [P] [US2] Author embedded CSS (failure-first, collapsible tree, light/dark, WCAG 2.1 AA contrast, visible focus) in `src/JTest.Core/Reporting/Html/report.css`
- [X] T037 [P] [US2] Author embedded JS (collapse, search/filter, keyboard nav; build DOM via `textContent` only) in `src/JTest.Core/Reporting/Html/report.js`
- [X] T038 [US2] Failure-first ordering, rollups, and drill-down (run→suite→case→dataset→iteration→step→assertion) in the projector (depends on T035)
- [X] T039 [US2] Implement safe large/binary content handling in the projector and trace: truncate request/response bodies over a configurable threshold (default 256 KB) with a truncation + original-size indicator; summarize binary/non-UTF-8 content as content-type + byte size (never raw) in `src/JTest.Core/Reporting/Html/HtmlReportGenerator.cs` and `src/JTest.Core/Tracing/TraceBuilder.cs` (FR-023)
- [X] T040 [US2] Re-implement the Markdown writer as a projection of the trace in `src/JTest.Core/Reporting/Markdown/MarkdownReportGenerator.cs` (retire the source-of-truth writer; restore/remove `tests/JTest.UnitTests/ResultsToMarkdownConverterTests.cs`)
- [X] T041 [US2] Register HTML/Markdown/JSON output generators in DI (both `src/JTest.Cli/DI/DependencyRegistration.cs` and `DependencyRegistrationHelper.cs`)
- [X] T085 [US2] Assertion clarity: carry the asserted `subject` (original actual expression) and optional `description` through the trace and show them in the report so a passing assertion is self-explanatory (FR-015/FR-050). Files: `src/JTest.Core/Assertions/AssertionResult.cs`, `AssertionOperationBase.cs`, `src/JTest.Core/Tracing/TraceNodes.cs`, `src/JTest.Core/Execution/ExecutionTraceAssembler.cs`, `src/JTest.Core/Reporting/Html/report.js`, trace contract `contracts/execution-trace.schema.json`; tests in `tests/JTest.UnitTests/Tracing/TraceSchemaTests.cs` and `Reporting/HtmlReportTests.cs`
- [X] T086 [US2] JSON body viewer: render request/response bodies in a collapsible, pretty-printed JSON box with a copy control, self-contained and inert (FR-051). Files: `src/JTest.Core/Reporting/Html/report.js`, `report.css`; test in `tests/JTest.UnitTests/Reporting/HtmlReportTests.cs`
- [X] T087 [US2] Report layout: elide a case's lone default/unparameterized dataset (render its steps directly under the case; keep multi/parameterized datasets) and replace per-level borders with a boxed suite/case + thin gradient indentation-guide rail for nested nodes (FR-017). Projection-only, no evidence hidden. Files: `src/JTest.Core/Reporting/Html/report.js`, `report.css`
- [X] T088 [US2] Retire the legacy per-suite Markdown writer and fix default outputs (FR-052): `run` writes `artifacts/report.html` + `artifacts/trace.json` by default (never a per-suite Markdown dump in the working folder); `-f/--output-format markdown` writes a clean `report.md` projection instead of HTML; explicit `--report`/`--trace` override and `--skip-output` suppresses only the defaults. The result processor is now console-summary-only and the legacy `src/JTest.Core/Output/` writer set (`MarkdownOutputGenerator`, `MarkdownTestCaseResultWriter`, `IOutputGenerator`, `ITestCaseResultWriter`, `HttpStepResultDataWriter`) is **deleted**. Files: `src/JTest.Cli/Commands/RunCommand.cs`, `Settings/RunCommandSettings.cs`, `src/JTest.Core/Execution/IJTestSuiteExecutionResultProcessor.cs` + `JTestSuiteExecutionResultProcessor.cs`, `src/JTest.Cli/DI/DependencyRegistration.cs` + `DependencyRegistrationHelper.cs`, `src/JTest.Core/Reporting/Markdown/MarkdownReportGenerator.cs`, `contracts/cli-contract.md`; removed `src/JTest.Core/Output/**`

**Checkpoint**: `run` produces a complete, offline, failure-first HTML report — with self-explanatory assertions and a JSON body viewer.

---

## Phase 5: User Story 3 — Report safe to publish (redaction + escaping) (Priority: P2)

**Goal**: No secrets and no active markup in the default report/trace.

**Independent Test**: Run a corpus with HTML/script payloads and secret values in assertions/errors/names/headers/bodies/query; assert inert rendering and zero visible secrets.

### Tests for User Story 3

- [X] T042 [P] [US3] XSS corpus test: HTML/script in assertion actual/expected, error, name, description, body → inert in HTML in `tests/JTest.UnitTests/Reporting/XssEscapingTests.cs`
- [X] T043 [P] [US3] Secret corpus test: secrets in headers, JSON body, and query string → redacted by default in report and trace in `tests/JTest.UnitTests/Security/SecretLeakTests.cs`

### Implementation for User Story 3

- [X] T044 [US3] Route every dynamic value in the HTML and Markdown projectors through `ReportValuePipeline` (no ad-hoc escaping) in `src/JTest.Core/Reporting/Html/HtmlReportGenerator.cs` and `Markdown/MarkdownReportGenerator.cs`
- [X] T045 [US3] Ensure the persisted canonical trace has declared secrets masked before serialization in `src/JTest.Core/Tracing/TraceBuilder.cs`
- [X] T046 [US3] Implement opt-in, masked env/global/variable dump (`--include-variables`) in report + trace; excluded by default in `src/JTest.Core/Reporting/` and `src/JTest.Cli/Commands/RunCommand.cs`

**Checkpoint**: The report is safe to attach as a public pipeline artifact.

---

## Phase 6: User Story 4 — Faithful loops, nesting, cancellation, parallelism (Priority: P2)

**Goal**: Every iteration, correct ancestry/numbering, and distinct cancelled/timed-out outcomes.

**Independent Test**: Execute for/while loops (multi-iteration + early-exit), nested templates, a timeout, and a cancellation; assert full iteration retention, unique ids/ordinals, distinct outcomes, and parallel==sequential.

### Tests for User Story 4

- [X] T047 [P] [US4] Test: N-iteration loop retains N iterations; early-exit keeps exact executed count; no stale/null slots in `tests/JTest.UnitTests/Steps/LoopRetentionTests.cs`
- [X] T048 [P] [US4] Test: deep template+loop nesting yields unique ids and correct parent/ordinal (zero collisions) in `tests/JTest.UnitTests/Steps/AncestryTests.cs`
- [X] T049 [P] [US4] Test: step/loop timeout → `timedOut`; cancellation mid-run → `cancelled`; both exit 4 in `tests/JTest.UnitTests/Execution/CancellationTimeoutTests.cs`
- [X] T050 [P] [US4] Test: same corpus sequential vs parallel → equivalent node set/outcomes in `tests/JTest.UnitTests/Execution/ParallelEquivalenceTests.cs`

### Implementation for User Story 4

- [X] T051 [US4] Rework `ForLoopStep` to emit an `Iteration` node per pass with its own inner steps (size by iterations, not step count) in `src/JTest.Core/Steps/ForLoopStep.cs`
- [X] T052 [US4] Rework `WhileStep` likewise and add the missing `[TypeIdentifier("while")]` in `src/JTest.Core/Steps/WhileStep.cs`
- [X] T053 [US4] Assign stable step id/ordinal/path at execution time (replace the flat `StepNumber`) in `src/JTest.Core/Steps/StepProcessor.cs` and `src/JTest.Core/Execution/TestExecutionContext.cs`
- [X] T054 [US4] Capture template (`UseStep`) child ancestry with correct parent/ordinal in `src/JTest.Core/Steps/UseStep.cs`
- [X] T055 [US4] Honor the `CancellationToken` and step/loop/run timeouts, recording `cancelled`/`timedOut` outcomes across executors and steps in `src/JTest.Core/Execution/` and `src/JTest.Core/Steps/`
- [X] T056 [US4] Rework parallel execution to merge complete subtrees without loss (no dropped throwing suites) in `src/JTest.Core/Execution/JTestSuiteExecutor.cs`

**Checkpoint**: The trace is a faithful, complete history under all control-flow shapes.

---

## Phase 7: User Story 7 — Deterministic authenticated HTTP flows (Priority: P2)

**Goal**: Cookie sessions persist across steps within a case, isolated across cases, independent of handler-pool lifetime; corrected response contract.

**Independent Test**: login→authenticated GET passes with no manual Cookie header across pool recycles; two parallel cases don't share cookies; `statusCode`/`status`/keyed headers resolve.

### Tests for User Story 7

- [X] T057 [P] [US7] Test: login→authenticated call succeeds w/o manual `Cookie`, including after forced handler-pool recycle in `tests/JTest.UnitTests/Http/CookieSessionTests.cs`
- [X] T058 [P] [US7] Test: two parallel cases as different users never share cookies in `tests/JTest.UnitTests/Http/CookieIsolationTests.cs`
- [X] T059 [P] [US7] Test: `$.this.headers['content-type']` case-insensitive, multi-valued `set-cookie` array, `statusCode`+`status` both resolve in `tests/JTest.UnitTests/Steps/HttpResponseContractTests.cs`

### Implementation for User Story 7

- [X] T060 [US7] Introduce `Http/IHttpClientProvider` + `HttpClientProvider` bound to a per-scope `CookieContainer` (deterministic, pool-lifetime-independent) in `src/JTest.Core/Http/`
- [X] T061 [US7] Establish a per-case execution/session scope and pass the provider to `HttpStep` (replace reflection-injected raw `HttpClient`) in `src/JTest.Core/Execution/` and `src/JTest.Core/TypeDescriptors/TypeDescriptorRegistry.cs`
- [X] T062 [US7] Reconcile both `AddHttpClient` registrations onto the provider (host + Spectre containers) in `src/JTest.Cli/Core/JTestApplication.cs`
- [X] T063 [US7] Emit a case-insensitive keyed header map with multi-valued support in `HttpStep.CreateResponseData`/`GetResponseHeaders` in `src/JTest.Core/Steps/HttpStep.cs`
- [X] T064 [US7] Add `statusCode` (canonical) + `status` (alias) to response data and trace `HttpExchange` in `src/JTest.Core/Steps/HttpStep.cs` and `src/JTest.Core/Tracing/`
- [X] T065 [US7] Redact `Cookie`/`Set-Cookie`/`Authorization` via the pipeline in HTTP exchange projection (FR-042)
- [X] T084 [US7] Template-invoked steps share the case cookie jar: the isolated child scope created by `UseStep.CreateIsolatedTemplateContext` MUST inherit the caller's `CookieContainer` (variables stay isolated) so a login inside a `use` template establishes the case session (FR-038/FR-039). Fixes the dropped-session/401 regression found dogfooding `2.0.0-preview.9`. Files: `src/JTest.Core/Execution/TestExecutionContext.cs` (`Cookies { get; init; }`), `src/JTest.Core/Steps/UseStep.cs` (`new TestExecutionContext { Cookies = parentContext.Cookies }`); regression + variable-isolation test in `tests/JTest.UnitTests/Http/HttpCookieSessionTests.cs` (SC-013)

**Checkpoint**: Cookie-based auth flows (e.g. Elsa) work deterministically and safely — including logins performed inside a `use` template.

---

## Phase 8: User Story 5 — Formal language schema & honest validation (Priority: P3)

**Goal**: A versioned JSON Schema with real, located diagnostics; guaranteed JSONPath filter support.

**Independent Test**: Validate valid/invalid corpora → correct pass/fail with located diagnostics and honest counts; run a JSONPath filter corpus in save/assert/interpolation.

### Tests for User Story 5

- [X] T066 [P] [US5] Test: valid corpus passes, invalid corpus (unknown type, wrong type, missing required, bad reference) fails with located diagnostics + honest counts in `tests/JTest.UnitTests/Language/SchemaValidationTests.cs`
- [X] T067 [P] [US5] Test: JSONPath filter + multi-match corpus resolves in save/assert/interpolation using the pinned dialect in `tests/JTest.UnitTests/Utilities/JsonPathFilterTests.cs`

### Implementation for User Story 5

- [X] T068 [US5] Author the versioned JTest language JSON Schema (draft 2020-12; step discriminators, types, constraints, references) as an embedded resource in `src/JTest.Core/Language/Schema/jtest-language-1.0.0.schema.json` (per `contracts/jtest-language-schema.contract.md`)
- [X] T069 [US5] Implement the schema validator using `JsonSchema.Net`, emitting machine-readable located diagnostics (JSON Pointer + ruleId) in `src/JTest.Core/Language/Validation/SchemaValidator.cs`
- [X] T070 [US5] Replace the shallow `JTestSuiteValidator` checks with real schema validation; honest labels and counts (fix the never-incremented valid count) in `src/JTest.Core/JTestSuiteValidator.cs`
- [X] T071 [US5] Pin and document the JSONPath dialect (`JsonPath.Net`, RFC 9535) and guarantee filter + multi-match resolution across save/assert/interpolation in `src/JTest.Core/Utilities/VariableInterpolator.cs`
- [X] T072 [US5] Apply intentional breaking corrections and record them in `CHANGELOG.md` (while-step type id, canonical assertion operator names, `additionalProperties:false`, typed durations)

**Checkpoint**: `validate` is a real CI gate; the language has an authoritative contract.

---

## Phase 9: User Story 6 — Honest, reproducible release (Priority: P3)

**Goal**: Version single-sourced across source/package/tag; license present; CI-gated.

**Independent Test**: Inspect package version, tool version, and git tag for agreement; confirm LICENSE exists and README link resolves.

### Tests for User Story 6

- [X] T073 [P] [US6] Test/gate asserting version consistency (source == package == tag) and LICENSE presence + resolvable README link in `tests/JTest.UnitTests/Release/ReleaseMetadataTests.cs`

### Implementation for User Story 6

- [X] T074 [US6] Author the CI workflow (build, test, `jtest validate` + `jtest run` over fixtures, tag==version gate) in `.github/workflows/ci.yml`
- [X] T075 [US6] Confirm reproducible pack from the tagged commit and single-sourced version wiring in `Directory.Build.props` and CI

**Checkpoint**: JTest 2.0 can be tagged and published honestly.

---

## Phase 10: Documentation Rewrite (Final — FR-044/FR-045)

**Purpose**: Regenerate `docs/` from the implemented system. Runs last so it reflects what was built.
Legacy docs are output, never a source of truth.

- [X] T076 Delete legacy `docs/` content and re-author it from the implemented 2.0 system — language (from the shipped schema), HTTP step/session contract (`statusCode`/`status`, keyed headers, cookies), exit-code contract, canonical trace, reporting, and redaction — in `docs/`
- [X] T077 Add a CI check that validates every test-definition example embedded in `docs/` against the shipped language schema in `.github/workflows/ci.yml`
- [X] T078 [P] Rewrite `README.md` to the 2.0 system (commands, exit codes, report, license link)
- [X] T079 Verify zero references to removed/legacy 1.0 contract behavior remain across `docs/` and `README.md`

**Checkpoint**: Docs describe the truth of the shipped system (SC-016).

---

## Phase 11: Polish & Cross-Cutting Concerns

- [X] T080 [P] Run `specs/001-jtest2-pipeline-reporting/quickstart.md` end-to-end against sample suites
- [X] T081 [P] Performance check: a ~5,000-node run serializes + renders HTML within ≤ 3 seconds and stays interactive (`tests/JTest.UnitTests/Reporting/LargeRunPerfTests.cs`)
- [X] T082 Security review of report output (no injection/leak paths) across all projections
- [X] T083 Final Constitution compliance re-check (all 8 gates) and release notes

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Ph1)**: no dependencies.
- **Foundational (Ph2)**: depends on Setup; **blocks all user stories**.
- **User stories (Ph3–Ph9)**: all depend on Foundational. Recommended order by priority:
  US1 → US2 → (US3, US4, US7) → (US5, US6). US3 depends on the pipeline (Ph2) and projectors (US2).
  US2's full fidelity benefits from US4, but is independently testable via trace fixtures.
- **Docs (Ph10)**: depends on the language schema (US5) and the implemented behaviors it documents — runs last.
- **Polish (Ph11)**: depends on all desired stories.

### Story independence notes

- US1 is the MVP and is testable on the in-memory trace + exit codes alone.
- US2 is testable from hand-authored trace fixtures (no live HTTP needed).
- US7 requires the per-case session scope introduced in US4/Foundational execution rework (T061 depends on T053/T054 scope work); sequence US4 before US7 if staffed serially.
- US5 and US6 are independent of the reporting stories.

### Parallel opportunities

- Ph1: T003–T008 in parallel.
- Ph2: T009–T011 parallel; T015/T017/T020 parallel with their siblings.
- Once Ph2 is done, US1/US2/US5/US6 can proceed in parallel by different developers; US3 after US2's projectors exist; US7 after the US4 scope work.
- All `[P]` test tasks within a story run in parallel.

---

## Parallel Example: User Story 1

```bash
# Tests first (parallel):
Task: "Integration test crashing suite → exit 2 (FalseGreenTests.cs)"
Task: "Integration test empty/pass/fail exit codes (ExitCodeIntegrationTests.cs)"
Task: "validate invalid corpus → exit 3 (ValidateExitTests.cs)"
Task: "unresolved-path diagnostic vs matched-null (UnresolvedPathTests.cs)"
# Then implementation T025–T029.
```

---

## Implementation Strategy

### MVP first (US1)

1. Ph1 Setup → 2. Ph2 Foundational → 3. Ph3 US1 → **STOP & validate**: the gate can never
   false-green. This alone unblocks safe pipeline use.

### Incremental delivery

US1 (honest gate) → US2 (HTML report) → US3 (safe to publish) → US4 (faithful history) →
US7 (auth flows) → US5 (schema/validate) → US6 (release) → Docs → Polish. Each story is a
demoable increment that does not break earlier ones.

---

## Notes

- Tests are required (Principle VII); write per-story tests before/with implementation and ensure they fail first.
- `[P]` = different files, no dependency on an incomplete task.
- Commit after each task or logical group; keep the branch pushed.
- Total: 89 tasks — Setup 8, Foundational 12, US1 10, US2 16, US3 5, US4 10, US7 10, US5 7, US6 3, Docs 4, Polish 4.
- T084 was added post-implementation to capture a defect found while dogfooding `2.0.0-preview.9` (per-case cookie session dropped across a `use` template boundary); the fix and its regression test are folded into US7.
- T085–T088 were added post-implementation from dogfooding feedback: assertions now surface subject/description, response bodies render in a collapsible JSON viewer, the redundant single default dataset level is elided, nested detail uses an indentation-guide rail, and the legacy per-suite Markdown dump is retired in favour of tidy `artifacts/` defaults (HTML+trace, or a clean Markdown projection). Folded into US2.
- T089 was added post-implementation for issue #74 (reported against `2.0.0-preview.13`): `-e/--env` was silently ignored, so a pipeline could not retarget a suite's `env`. Folded into US1.
