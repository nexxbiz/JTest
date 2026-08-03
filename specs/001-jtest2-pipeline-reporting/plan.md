# Implementation Plan: JTest 2.0 — Reliable Pipeline Execution & Trustworthy HTML Report

**Branch**: `001-jtest2-pipeline-reporting` | **Date**: 2026-08-03 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-jtest2-pipeline-reporting/spec.md`

## Summary

JTest 2.0 hardens execution and rebuilds reporting around a single **canonical execution
trace**. The engine is reworked so that (a) nothing is ever silently dropped — a crashing or
cancelled suite becomes a first-class failed/aborted node and drives a non-zero, class-specific
exit code; and (b) the trace captures full ancestry (suite → case → dataset → step →
template/loop → iteration → child-step → assertion) with stable ids, ordinals, iteration
indices, timings, and outcomes. Every human-facing report (a new self-contained HTML file,
plus the retained console summary and a trace-projected Markdown) is a read-only projection of
that trace, produced through one centralized value pipeline that HTML-encodes and redacts by
default (including cookie/authorization values). HTTP steps also gain deterministic, per-case
cookie sessions (isolated under parallelism, independent of handler-pool lifetime) and a corrected
response contract — case-insensitive keyed `headers` with multi-valued `set-cookie`, and both
`statusCode` and `status`. A formal, versioned JSON Schema for the JTest language replaces the shallow structural
"validation", with real, located diagnostics that gate CI. Release metadata is single-sourced
and a LICENSE file is added.

Technical approach stays inside the existing two-project layout (`JTest.Core` + `JTest.Cli`),
adding new folders rather than new assemblies, and reuses the json-everything ecosystem already
present (`JsonPath.Net`) by adding `JsonSchema.Net` for schema validation. No web framework:
the HTML report is composed server-side from embedded, inlined CSS/JS templates.

## Technical Context

**Language/Version**: C# 12 on .NET 8.0 (`net8.0`) — unchanged.
**Primary Dependencies**: `JsonPath.Net` (json-everything) already referenced; **add
`JsonSchema.Net`** (same ecosystem) for language-schema validation; `System.Text.Json` for the
canonical trace; `Microsoft.Extensions.DependencyInjection`/`.Http`/`.Hosting`;
`Microsoft.Extensions.FileSystemGlobbing`; `Spectre.Console.Cli` 0.53.1 for the CLI. No new
runtime dependency family; no web/reporting framework.
**Storage**: filesystem only — JSON test-definition files in; canonical trace JSON, HTML report,
and Markdown out. No database.
**Testing**: xUnit 2.4.2 (+ Moq, NSubstitute, coverlet) in `tests/JTest.UnitTests`; add a
golden-file / integration fixture set for traces, reports, exit codes, escaping, and redaction.
**Target Platform**: cross-platform .NET global tool (`jtest`), run headless in CI/CD.
**Project Type**: single solution — core library + CLI tool (Option 1, single project group).
**Performance Goals**: a run of up to ~5,000 trace nodes serializes and renders to a
self-contained HTML file within a few seconds; the HTML remains interactive (client-side
search/filter/collapse) at that scale.
**Constraints**: HTML report MUST be self-contained (zero external requests) and open offline;
output ordering MUST be deterministic; all dynamic values MUST pass through one encode+redact
pipeline; must not regress the existing public `jtest` command surface except where the spec
requires (exit codes, new `report`/trace output).
**Scale/Scope**: typical suites of 10s–100s of cases with nested templates and multi-iteration
loops; reports up to low-thousands of nodes.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

Constitution v1.0.0. Gates below; NON-NEGOTIABLE principles marked (★).

| # | Principle | Gate for this feature | Status |
|---|-----------|-----------------------|--------|
| I | Evidence is canonical, reports are projections | A single `ExecutionTrace` model in `JTest.Core` is the only run result; HTML/Markdown/console are pure projections with no independent state or hidden data. | PASS |
| II ★ | No false-green | Every suite/case/step exception is captured as a node; an `ExitCodeService` maps aggregate outcome → class-specific non-zero code; empty-but-expected discovery → non-zero; `validate` → non-zero on any invalid file. Covered by exit-code tests. | PASS |
| III | Complete, faithful history | Trace nodes carry stable id/path, kind, ordinal, iteration index, timings, outcome, diagnostics, children; loop capture stores every iteration; numbering assigned at execution time. | PASS |
| IV | Secure & redacted by default | One `ReportValuePipeline` performs contextual encoding + value-based redaction for every projected value (incl. Cookie/Set-Cookie/Authorization); env/global dumps opt-in and masked. Escaping/redaction tests required. | PASS |
| V | Formal, versioned language contract | Versioned JSON Schema authored under `JTest.Core/Language/Schema`; validator uses `JsonSchema.Net`; diagnostics carry location; honest counts/labels. | PASS |
| VI | Pipeline-first & deterministic | Self-contained HTML; deterministic node ordering; cancellation/timeout are distinct outcomes; deterministic per-case cookie sessions isolated under parallelism; sequential/parallel equivalence test. | PASS |
| VII ★ | Test-backed correctness | Each correctness/reporting behavior ships with xUnit + golden-file tests: loops, nesting, cancellation, timeout, parallel, exit codes, escaping, redaction. | PASS |
| VIII | Honest, reconciled releases | Version single-sourced via `Directory.Build.props`; add `LICENSE` (MIT); CI checks tag == version. | PASS |

**Result: PASS — no violations.** Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/001-jtest2-pipeline-reporting/
├── plan.md              # This file
├── research.md          # Phase 0 output — decisions & rationale
├── data-model.md        # Phase 1 output — canonical trace entity graph
├── quickstart.md        # Phase 1 output — pipeline usage & exit codes
├── contracts/           # Phase 1 output — CLI, trace schema, language & HTTP-step contracts
│   ├── cli-contract.md
│   ├── execution-trace.schema.json
│   ├── jtest-language-schema.contract.md
│   └── http-step.contract.md
├── checklists/
│   └── requirements.md  # Spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

Existing tracked layout is retained (two projects). New work is added as **folders**, not new
assemblies, to keep the build and packaging simple.

```text
src/
├── JTest.Core/                      # net8.0 library (PackageId JTest.Core)
│   ├── Tracing/                     # NEW — canonical ExecutionTrace model + builder + JSON I/O
│   ├── Execution/                   # REWORK — suite/case/step executors capture all outcomes; per-case session scope
│   ├── Http/                        # NEW — IHttpClientProvider + per-scope CookieContainer, keyed header map, statusCode/status
│   ├── Steps/                       # REWORK — ForLoopStep/WhileStep iteration retention, step ids; HttpStep response contract
│   ├── Language/
│   │   ├── Schema/                  # NEW — versioned JTest language JSON Schema (embedded resource)
│   │   └── Validation/              # NEW — real schema validator + located diagnostics
│   ├── Reporting/
│   │   ├── ReportValuePipeline.cs   # NEW — one encode+redact pipeline for all projections
│   │   ├── Html/                    # NEW — self-contained HTML projector + embedded CSS/JS
│   │   └── Markdown/                # REWORK — projection of the trace (was the de-facto source)
│   ├── Security/                    # NEW/REWORK — SecurityMasker fixed (value + key match, bodies)
│   ├── Models/ Assertions/ Variables/ Templates/ JsonConverters/ ...   # existing
│   └── Exceptions/ Utilities/ TypeDescriptors/                          # existing
└── JTest.Cli/                       # net8.0 global tool `jtest`
    ├── Core/                        # REWORK — reconcile the two AddHttpClient paths onto the scoped cookie provider
    └── Commands/                    # REWORK — run/validate/report exit codes; trace/HTML output

tests/
└── JTest.UnitTests/                 # xUnit
    ├── Tracing/ Execution/ Steps/ Reporting/ Language/ Security/        # NEW/expanded suites
    └── Fixtures/ + golden/                                              # NEW golden traces & reports

# Repository root additions
Directory.Build.props                # NEW — single-sourced <Version>
LICENSE                              # NEW — MIT (NexxBiz)
```

**Structure Decision**: Single solution, two tracked projects (`JTest.Core`, `JTest.Cli`) plus
`tests/JTest.UnitTests` — unchanged. New capabilities land as folders within `JTest.Core`. We
deliberately do **not** reintroduce the separate `JTest.Engine/Evidence/Language/Reporting`
assemblies from the abandoned Program Kit attempt (their leftover `bin/obj` dirs under `src/`
and `src/.program-kit-build/` are stale build output to be removed; see research.md). Rationale:
the spec requires no assembly boundary, and fewer projects keeps versioning, packaging, and the
build gate simple (Principle-agnostic simplicity).

## Complexity Tracking

No constitution violations — no entries.

## Implementation Phases (high level, for /speckit-tasks)

Ordered by the spec's user-story priorities; each phase is independently testable.

1. **Cleanup & foundation** — remove stale net10.0 leftover dirs; add `Directory.Build.props`
   (single-sourced version) and `LICENSE`; add `JsonSchema.Net`.
2. **Canonical trace model (P1 core)** — `Tracing/` model + builder + versioned JSON
   serialization; unit tests for shape/versioning. (FR-009–FR-015)
3. **Execution correctness (US1, US4 — P1/P2)** — rework executors/steps to emit the trace,
   capture crashing/cancelled/timed-out suites, retain every loop iteration, assign step
   ids/ordinals; establish a per-case **execution/session scope**; `ExitCodeService` with
   class-specific codes; parallel==sequential. Tests for false-green, loops, nesting, cancellation,
   timeout, exit codes. (FR-001–FR-008, FR-013–FR-014)
4. **HTTP session & step contract (US7 — P2)** — introduce `Http/IHttpClientProvider` bound to the
   per-case cookie container (deterministic across steps, isolated across cases, independent of
   handler-pool lifetime); reconcile the two `JTest.Cli` `AddHttpClient` paths onto it; emit the
   HTTP response contract (case-insensitive keyed `headers` incl. multi-valued `set-cookie`,
   `statusCode` + `status`). Tests: login→authenticated call, pool-recycle, parallel isolation,
   header/status access. Docs updated. (FR-038–FR-043; SC-013–SC-015)
5. **Report value pipeline & security (US3 — P2)** — `ReportValuePipeline` (encode + redact by
   value & key, bodies included; Cookie/Set-Cookie/Authorization masked); fix `SecurityMasker`;
   opt-in masked env/global dumps. Tests for XSS corpus and secret corpus. (FR-024–FR-028, FR-042)
6. **HTML report projector (US2 — P1)** — self-contained single-file HTML from the trace:
   failure-first, collapsible, searchable, accessible, complete-by-default; embedded CSS/JS;
   golden-file tests + offline (no-network) assertion. Re-express Markdown as a projection.
   (FR-016–FR-023)
7. **CLI surface** — `run` emits HTML and (on request) canonical trace JSON; `report` command to
   render a report from a saved trace; wire exit codes; keep `debug` as an opt-in verbose view.
8. **Formal language schema & honest validation (US5 — P3)** — author versioned JSON Schema;
   `validate` enforces it with located diagnostics, honest counts/labels, non-zero on invalid.
   Apply intentional breaking corrections per FR-033 with a changelog. (FR-029–FR-033)
9. **Release integrity (US6 — P3)** — reconcile version across source/package/tag; README license
   link resolves; CI gate for tag==version, tests, and `jtest run/validate` over fixtures.
   (FR-034–FR-036)

*Full task breakdown is produced by `/speckit-tasks`.*
