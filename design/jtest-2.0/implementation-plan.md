# JTest 2.0 implementation plan

Plan version: 1.0.0
Status: awaiting human approval together with `architecture-design.md`
1.0.0. Implementation authority exists only after the human approves this
exact review set; the `implement-software-plan` capability then executes it
work unit by work unit and stops on material architectural deviation.

Conventions for every work unit below:

- **Depends** lists the units whose outputs must exist first.
- **Allowed edits** is exhaustive; touching paths outside it is a deviation.
- **Verification** must pass before the unit is complete:
  `dotnet build -c Release` and `dotnet test -c Release` are implied for
  every unit from JT2-W020 onward (warnings-as-errors, locked-mode restore),
  plus the listed unit-specific evidence.
- **Stop** conditions halt the flow and report to the human.
- Each unit ends in one reviewable commit (or small commit series) on the
  working branch, message explaining what and why.

Open decisions D1–D4 in the design must be resolved by the human before
JT2-W010 starts; the plan below assumes the recommended options (D1
generated console host, D2 governance-based gate, D3 delete 1.x at W010,
D4 fixed step catalog) and marks every point where a different choice
alters a unit.

## JT2-W010 — Repository reset and engineering baseline

Depends: —
Goal: a clean net10.0 repository skeleton adopting the Program Kit
engineering baseline, consuming the local Program Kit feed, with 1.x source
removed (D3).

Allowed edits: delete `src/`, `tests/`, `docs/`, `scripts/`, `ci-examples/`,
`Dockerfile`, `docker.sh`, `setup.ps1`, `setup.sh`, `INSTALLATION.md`,
`JTest.sln`; create `global.json`, `Directory.Build.props`,
`Directory.Packages.props`, `NuGet.Config`, `.editorconfig`, `LICENSE`,
`JTest.sln`, empty project skeletons for the four source and five test
projects (§3 of the design), `governance/csharp-source-quality-gate.md`
(D2), `tools/prepare-program-kit-feed.ps1`,
`packages/local-feed.manifest.json`, `.gitignore` update, minimal `README.md`
stating rebuild status.

Outputs: building empty solution against the local feed;
`packages/local-feed.manifest.json` pinning Program Kit commit + package
digests.

Verification: `tools/prepare-program-kit-feed.ps1` produces the feed from an
explicit `-ProgramKitRoot`; `dotnet restore --configfile NuGet.Config
--locked-mode` succeeds with sources `local-program-kit` + `nuget.org` only;
solution builds with zero warnings; `LICENSE` present (R-REL-1).

Stop: any Program Kit package fails to pack or restore against net10.0;
feed manifest digest mismatch on re-run (nondeterministic pack).

## JT2-W020 — Language model, JSON Schema, validation, diagnostics

Depends: W010
Goal: `JTest.Language` complete — typed models, embedded
`jtest-suite-2.0.0.schema.json`, three-layer fail-closed validation
(§5.4), stable `JT****` diagnostics, and the language manifest.

Allowed edits: `src/JTest.Language/**`, `tests/JTest.Language.Tests/**`,
`schemas/jtest-suite-2.0.0.schema.json`,
`schemas/jtest-language-manifest-2.0.0.json`, `docs/language/**`.

Outputs: validation API returning typed diagnostics; schema and manifest as
reviewable committed artifacts; diagnostic-code registry doc
(`docs/language/diagnostics.md`, append-only).

Verification: fixture suites — valid documents for every step type and
construct; invalid fixtures asserting exact diagnostic code + JSON pointer
for every `JT00xx–JT05xx` code shipped; schema round-trip (every valid
fixture passes schema; every schema-invalid fixture yields layer-2
diagnostics, never exceptions); unknown-property rejection proven.

Stop: any language question the design does not answer and that changes
document shape (must go back to the human as a design amendment).

## JT2-W030 — Expression engine and context semantics

Depends: W020
Goal: deterministic, fail-closed expression resolution (§5.3) and the
execution-context model (scopes, frames, template visibility, loop
bindings).

Allowed edits: `src/JTest.Engine/Expressions/**`,
`src/JTest.Engine/Contexts/**`, `tests/JTest.Engine.Tests/**`.

Outputs: resolver with position-exact substitution, typed single-token
results, bounded nesting, `${ENV}` handling; frame/scoping implementation
with template read-only env/globals and fresh `ctx`.

Verification: table-driven fixtures for every resolution rule in §5.3,
including: unresolvable path ⇒ `JT02xx` failure (not null); repeated
identical tokens with frame-local differences substituted position-exactly;
invariant-culture stringification; depth-overflow diagnostic; template
scope visibility matrix (params/case/env/globals readable, globals write
rejected).

Stop: JSONPath library capability gap that would change token syntax.

## JT2-W040 — Execution engine, trace contract, canonical result document

Depends: W030
Goal: the run→suite→case→datasetRun→step trace tree (§6.2) with `http`,
`assert`, `wait` steps, assertion operators, capture-time redaction (§9),
and the canonical `result.json` writer + `jtest-result-2.0.0.schema.json`.

Allowed edits: `src/JTest.Engine/**` (excluding areas owned by W030 only by
extension), `src/JTest.Reporting/Canonical/**`,
`schemas/jtest-result-2.0.0.schema.json`, `tests/JTest.Engine.Tests/**`,
`tests/JTest.Reporting.Tests/**`.

Outputs: engine producing complete immutable traces where aggregate
outcomes are functions of children; skipped-step nodes after failure;
exception ⇒ `error` node with `JT9xxx`; redaction applied at capture;
canonical serializer producing byte-stable `result.json`; `runId` per §7.1.

Verification: an in-process fake HTTP handler (no network) drives
fixtures: passing case, failing assertion, engine exception, timeout,
cancellation — each asserting the exact trace shape and that **no outcome
path yields a passing aggregate with a non-passing child** (F1/F4
regression class); redaction fixtures proving secrets absent from result
bytes (actual and expected assertion values, headers, `${ENV}` values);
byte-exact double-serialization fixture (R-DET-1).

Stop: any need to place evidence outside the trace tree (e.g. side
channels) — that is a contract change.

## JT2-W050 — Composite steps: templates, for, while

Depends: W040
Goal: `use`, `for`, `while` with full-fidelity nesting: one
`templateInvocation`/`iteration` node per occurrence, child steps preserved
per iteration, loop delay semantics, while timeout as `timedOut` outcome.

Allowed edits: `src/JTest.Engine/Steps/Composite/**`,
`tests/JTest.Engine.Tests/**`, template fixtures under
`tests/fixtures/**`.

Verification: fixtures for loop with N iterations × M steps asserting
N iteration nodes each with M children and correct `iterationIndex` and
paths (R-TRACE-1/2); early-failure iteration recording partial children +
skipped remainder; `while` timeout mid-iteration; nested template-in-loop
and loop-in-template ancestry; template output mapping and visibility rules
end-to-end.

Stop: trace size pathological cases (e.g. >10k nodes) degrading beyond
acceptance limits — return with measurement, don't silently truncate.

## JT2-W060 — Reporting: writers and static viewer

Depends: W040 (uses W050 fixtures when available)
Goal: catalog writer (`catalog.js`, `result.js`, viewer asset placement),
standalone single-file writer, and the committed pure HTML/CSS/JS viewer
(§7.2–7.4) at the agreed visual standard.

Allowed edits: `src/JTest.Reporting/**`,
`src/JTest.Reporting/Viewer/index.html|viewer.css|viewer.js`,
`tests/JTest.Reporting.Tests/**`.

Outputs: deterministic writers; viewer with catalog view, run view,
failure-first ordering, collapsible trace tree, step-into with
breadcrumbs, assertion/http detail panes, search, hash deep-links,
light/dark themes, expand/collapse-all.

Verification: byte-exact fixtures — same trace ⇒ identical `result.js` and
standalone HTML; same (catalog, entry) ⇒ identical `catalog.js`; hostile
result document fixture rendered inert (R-SEC-2) via a headless DOM check
executed in the test suite; no external URL literals in viewer source
(static scan); manual visual review checkpoint with the human before the
unit closes (explicitly a human review, not an automated gate).

Stop: any requirement that would force a JS build toolchain — that reverses
a human decision.

## JT2-W070 — CLI host, exit codes, report URL and auto-open

Depends: W020, W060
Goal: the `jtest` tool per §8: Open Console document, generated host (D1),
dispatchers for `run`, `validate`, `describe`, frozen exit codes computed
from the result document, always-printed `file://` report URL, best-effort
auto-open with CI suppression.

Allowed edits: `hosting/jtest-open-console.json`, `src/JTest.Cli/**`,
`tests/JTest.Cli.Tests/**`, `tools/generate-cli-host.ps1` (explicit
invocation of the backed Program Kit operation).

Verification: end-to-end CLI tests (in-process + spawned) — exit code
matrix incl. "suite throws ⇒ exit 1 with error node present" and "no files
matched ⇒ exit 2" (R-CLI-1); `--diagnostics json` canonical output;
`describe` byte-equals embedded manifest/schema; URL line printed in all
modes; auto-open path unit-tested behind an opener interface (warning on
failure, exit code unchanged); `--env` first-`=` split and duplicate-key
diagnostics.

Stop (D1): the console host generation contract cannot express part of the
grammar — report the exact gap; do not hand-roll a divergent host under
this plan.

## JT2-W080 — Acceptance evidence and hardening

Depends: W050, W060, W070
Goal: prove the rebuilt whole against the intent and the 1.x findings.

Allowed edits: `tests/JTest.AcceptanceTests/**`, `tests/fixtures/**`,
sample suites under `examples/**`.

Outputs: a realistic example suite family (auth template, dataset matrix,
polling `while`, `for` over items) run against the in-process fake API and,
behind an explicit opt-in flag, a local containerized echo API; recorded
acceptance report attached as fixture evidence.

Verification: one acceptance test per finding F1–F8 demonstrating the 2.0
behavior; parallel `--parallel 4` content-equivalence versus sequential;
determinism re-run (twice, byte-compare canonical outputs); full solution
`dotnet test` green with zero warnings.

Stop: any acceptance test that cannot be expressed without weakening a
contract.

## JT2-W090 — Documentation and repository finishing

Depends: W080
Goal: rewritten documentation set and repository hygiene for the 2.0 line.

Allowed edits: `docs/**`, `README.md`, `ci-examples/**`,
`CHANGELOG.md`, csproj package metadata.

Outputs: language reference (generated sections sourced from the manifest
to prevent drift), authoring guide for humans, authoring guide for agents
(contract-first: schema + manifest + diagnostics), CLI reference, reporting
guide, updated CI examples, changelog describing the rebuild.

Verification: docs build-check (link validity), manifest-sourced sections
regenerated deterministically and diff-clean; README claims match shipped
behavior (no aspirational claims); version metadata single-sourced
(R-REL-1).

Stop: —

## JT2-W100 — Release readiness snapshot (no release)

Depends: W090
Goal: a reviewable statement of release readiness, honestly separated from
releasing (release flows are unavailable in the Program Kit index and out
of scope here).

Allowed edits: `design/jtest-2.0/release-readiness.md`.

Outputs: readiness report — versions, package inventory, verification
evidence digests, known limitations (taint-tracking, extensibility
deferral), and the explicit statement that publishing 2.0.0 requires a
separate human decision and process.

Verification: every claim in the report traces to a committed artifact or
recorded test run.

Stop: always — this is the plan's final stop for human review.

## Dependency graph

```
W010 → W020 → W030 → W040 → W050 ─┐
                        │         ├→ W080 → W090 → W100
                        └→ W060 ──┤
              W020 ──────→ W070 ──┘  (W070 also depends on W060)
```
