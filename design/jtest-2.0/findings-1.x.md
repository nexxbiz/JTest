# JTest 1.x verified findings register

Version: 1.0.0
Status: evidence input to the JTest 2.0 design; not a change proposal by itself.
Grounding: every finding below was re-verified against the current bytes of
this repository (branch `design/jtest-2-review`, content-identical to `main`
at commit `79d58ba`) during the design session on 2026-07-27.

Each finding lists the requirement it creates for JTest 2.0. Requirement IDs
are referenced by `architecture-design.md`.

## F1 — Loop steps lose all iterations except the last

`src/JTest.Core/Steps/ForLoopStep.cs:47` sizes `innerStepResults` to the step
count (not `iterations x steps`) and line 61 overwrites slot `i` on every
iteration. `src/JTest.Core/Steps/WhileStep.cs:34` and line 54 do the same. An
early `break` (failure or timeout) can leave later slots `null`. Only the last
iteration of each inner step can ever be reported; the true execution history
is destroyed before any renderer runs.

Creates requirement **R-TRACE-1** (preserve every iteration as a distinct
trace node).

## F2 — Nested steps cannot be truthfully numbered or attributed

`StepProcessedResult` takes its number from `context.StepNumber`
(`src/JTest.Core/Steps/StepProcessor.cs:219`), which is set only per top-level
step by `src/JTest.Core/Execution/JTestCaseExecutor.cs:99`. Template children
(`UseStep.cs:68`) and loop children execute against the same context, so every
nested result inherits its parent's number. There is no ancestry, ordinal, or
iteration identity anywhere in the result model; no renderer can reconstruct
truthful numbering.

Creates requirement **R-TRACE-2** (stable execution path and ancestry for
every node).

## F3 — Nested results are hidden unless debug mode is active

`src/JTest.Core/Output/Markdown/MarkdownTestCaseResultWriter.cs:117` renders
`InnerResults` only when `isDebug` is true. A normal run of a template-heavy
suite reports steps whose inner evidence is silently omitted.

Creates requirement **R-REPORT-1** (full nested execution always present in
evidence and report).

## F4 — Suite exceptions can produce a false-green exit

`src/JTest.Core/Execution/JTestSuiteExecutor.cs:32-37` catches any exception
from a suite run, writes it to the console, and adds **no result** for that
file. `src/JTest.Cli/Commands/RunCommand.cs:55` computes the exit code as
`results.All(x => x.CasesFailed == 0)` — a file that crashed contributes no
failed case and cannot fail the run. The parallel path is worse:
`JTestSuiteExecutor.cs:57-80` uses sync-over-async (`.Result`), drops results
on exception, and only increments a counter that nothing reads.

Additionally, `src/JTest.Cli/Commands/ExportCommand.cs` is a stub that prints
"Export completed successfully." and returns exit code 0 without exporting
anything.

Creates requirements **R-CLI-1** (no execution path may terminate with exit 0
unless every discovered suite produced a complete, passing trace) and
**R-CLI-2** (no command may report success for unimplemented behavior).

## F5 — "Schema" validation is shallow and its summary lies

`src/JTest.Core/JTestSuiteValidator.cs:95-113` checks only that `version`,
`tests`, `name`, and `steps` exist, then prints "Valid JTEST schema"
(line 52). `validFiles` (line 24) is never incremented, so the summary always
reports `Valid files: 0`. There is no authoritative JSON Schema, no property
or type validation, no step-type validation, and no machine-readable
diagnostics — inadequate for dependable agent authoring.

Creates requirements **R-LANG-1** (versioned JSON Schema + exhaustive
fail-closed validation) and **R-LANG-2** (machine-readable diagnostics with
stable codes and JSON-pointer locations).

## F6 — Reporting is unsafe: secrets and unescaped injection

- `SecurityMasker` (`src/JTest.Core/Debugging/SecurityMasker.cs`) masks by
  key-name heuristics and post-hoc string replacement over rendered text —
  values reached through paths not named like secrets leak.
- The assertion table masks only the actual value
  (`MarkdownTestCaseResultWriter.cs:151-152`); the expected value is
  hardcoded unmasked.
- Assertion values, descriptions, and error messages are written into raw
  embedded HTML (`MarkdownTestCaseResultWriter.cs:161,182-184,214`) without
  escaping — response-controlled content can inject markup/script into
  reports.

Creates requirements **R-SEC-1** (capture-time structural redaction),
**R-SEC-2** (no data ever concatenated into markup; DOM-safe rendering), and
**R-SEC-3** (no report may require debug mode to be truthful).

## F7 — Release metadata is inconsistent

`src/JTest.Core/JTest.Core.csproj` pins `PackageVersion` 1.0.0 while the
repository ships `tests/v1.0.3` verification suites and the README documents
tagged releases; both csproj files declare `PackageLicenseExpression` MIT and
the README links a `LICENSE` file that does not exist in the repository.

Creates requirement **R-REL-1** (single-source version, present license,
consistent package metadata).

## F8 — Additional load-bearing observations (not from the original intake)

- `JTestCaseExecutor.DeepCloneVariable`
  (`src/JTest.Core/Execution/JTestCaseExecutor.cs:230-239`) serializes a value
  to JSON and then returns the **original reference**; the "deep clone" for
  non-dictionary reference types is a no-op, so dataset isolation is not what
  the comments claim.
- Expression resolution (`src/JTest.Core/Utilities/VariableInterpolator.cs`)
  silently converts unresolvable paths to `null`/empty (line 277-280) and
  performs string `Replace` of token text (lines 108, 257), which rewrites
  repeated identical tokens even when a later occurrence should differ.
  Creates **R-LANG-3** (fail-closed expression resolution with deterministic,
  position-exact substitution).
- Template contexts (`UseStep.CreateIsolatedTemplateContext`,
  `src/JTest.Core/Steps/UseStep.cs:117-145`) copy only `case` and `with`
  parameters, so `{{$.env.*}}` and `{{$.globals.*}}` inside a template resolve
  to `null` silently — undocumented and surprising. Creates **R-LANG-4**
  (explicit, documented context visibility rules).
- `--env key=value` parsing (`RunCommandSettings.cs:112-115`) splits on every
  `=`, breaking values that contain `=`; duplicate env keys across file and
  flags throw an unhandled exception (`.Add`, line 122).
- Report file names embed a wall-clock timestamp at write time
  (`JTestSuiteExecutionResultProcessor.cs:91`), so the writer is inherently
  nondeterministic. Creates **R-DET-1** (deterministic writer: identical
  evidence in, byte-identical artifacts out).
