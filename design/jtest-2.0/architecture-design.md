# JTest 2.0 architecture design

Design version: 1.0.0
Status: awaiting human approval. This document grants no implementation
authority and records no approval.
Repository: JTest (`design/jtest-2-review` branch).
Companion documents: `findings-1.x.md` (verified evidence),
`implementation-plan.md` (bounded work units), `review-manifest.json`
(exact digests of this review set).

## 1. Intent

Rebuild JTest from scratch as **JTest 2.0** while keeping its intent alive:
a declarative JSON language for end-to-end API integration tests
(arrange, act, assert) that both humans and AI agents can author, execute
through a CLI, and inspect through truthful, beautiful reports.

The rebuild applies Program Kit engineering rules — deterministic generation,
stable diagnostics, pinned dependencies, fail-closed validation — and consumes
locally built Program Kit NuGet packages through a local feed. It resolves
every verified 1.x finding (F1–F8) by construction rather than by patching.

Human decisions already recorded during intake and design conversation:

- JTest 2.0 is a ground-up rebuild in this repository; version 1.x has no
  external consumers to migrate.
- JTest stays an **opt-in** tool; Program Kit adoption of JTest as a default
  endpoint-testing provider is a separate follow-up design (out of scope).
- No migration tooling from 1.x (out of scope).
- Program Kit is consumed as locally built NuGet packages via a local feed —
  no project references into the Program Kit checkout.
- The HTML report viewer is **pure static HTML/CSS/JS** (no React, no build
  toolchain, no CDN), committed as reviewed source, with collapsible
  drill-down into steps, templates, and loop iterations.
- Report writing is **purely deterministic**.
- The CLI always prints the report location as a ctrl-clickable `file://`
  URL and by default attempts to open it, downgrading to a warning on
  failure.
- Human sessions get a persistent **catalog** reports folder the viewer
  reads (refresh to see new runs); automated pipelines get an explicit
  output path with a standalone single-file report.

## 2. Non-goals

- No 1.x compatibility layer, importer, or migration assistant.
- No Program Kit capability, provider wrapper, hook, or tool binding is
  created by this design.
- No release, publication, or feed-transport behavior; the Program Kit
  release flows are `unavailable` in its capability index and nothing here
  routes into them.
- No test-generation AI features inside JTest itself; agents author tests
  using the language contract and manifest, outside this repository's
  runtime.
- The viewer never becomes a server, watcher, or live dashboard; it is a
  static projection of evidence on disk.

## 3. System identity and product structure

| Identity | Kind | Purpose |
| --- | --- | --- |
| `JTest.Language` | library / NuGet `JTest.Language` | The versioned JTest language: typed suite/case/step/template models, the published JSON Schema, exhaustive fail-closed validation, stable machine-readable diagnostics, and the agent-facing language manifest. No execution behavior. |
| `JTest.Engine` | library / NuGet `JTest.Engine` | Deterministic execution of validated suites: context and scope semantics, expression resolution, step execution (http, wait, assert, use, for, while), assertion operators, cancellation/timeout behavior, and production of the execution trace. |
| `JTest.Reporting` | library / NuGet `JTest.Reporting` | The canonical result document (evidence), capture-time redaction, the deterministic report writers (catalog mode and standalone mode), and the embedded static viewer assets. |
| `JTest.Cli` | executable / dotnet tool `jtest` | The command-line host: command grammar, exit codes, console output, report-URL printing and best-effort open. Composition only — no test semantics live here. |
| `reports viewer` | committed static assets (`src/JTest.Reporting/Viewer/`) | `index.html`, `viewer.css`, `viewer.js` — hand-authored, reviewed source; rendered data is loaded via script files the writer emits. |

Test projects mirror source projects one-to-one
(`tests/JTest.Language.Tests`, `tests/JTest.Engine.Tests`,
`tests/JTest.Reporting.Tests`, `tests/JTest.Cli.Tests`) plus
`tests/JTest.AcceptanceTests` for end-to-end fixtures.

Ownership boundary: `JTest.Language` owns *what a test means*;
`JTest.Engine` owns *what happened*; `JTest.Reporting` owns *how what
happened is evidenced and shown*; `JTest.Cli` owns *invocation and exit
semantics*. No layer reaches upward.

## 4. Program Kit consumption

### 4.1 Engineering baseline (adopted rules)

- Target framework `net10.0`, C# 14, .NET SDK pinned by `global.json` to
  `10.0.302` with `rollForward: disable` — required for binary compatibility
  with Program Kit packages (their `Directory.Build.props` pins the same
  profile).
- `Directory.Build.props` at the JTest root adopts the Program Kit posture:
  `Deterministic`, `DeterministicSourcePaths`, `TreatWarningsAsErrors` (all
  four variants), `AnalysisLevel latest-recommended`, `NuGetAudit` (mode
  `all`, level `low`), `RestorePackagesWithLockFile`, locked-mode restore in
  CI, `GenerateDocumentationFile`, central package management via
  `Directory.Packages.props` with every version pinned exactly.
- Source-quality rules from the Program Kit C# source gate
  (`governance/csharp-source-quality-gate.md`, policy 1.10.0) are adopted as
  JTest governance: one named type per file, namespace mirrors folder path,
  no behavior invoked on freshly constructed receivers, uncontracted helpers
  static, behavioral collaborators behind narrow interfaces with constructor
  injection, explicit composition roots. See open decision D2 for the
  enforcement mechanism.

### 4.2 Local package feed

The Program Kit `CSharpGate` analyzer is `IsPackable=false` and Program Kit
itself is unreleased, so JTest consumes **locally built** Program Kit packages:

- `packages/local-feed/` (git-ignored) holds `Orbyss.ProgramKit.*` `.nupkg`
  files produced from the sibling Program Kit checkout with its own
  `build/ProgramKit.Pack.proj` (`dotnet msbuild build/ProgramKit.Pack.proj
  /t:Pack /p:ProgramKitPackageProject=<csproj>
  /p:PackageOutputPath=<JTest>/packages/local-feed`).
- `tools/prepare-program-kit-feed.ps1` (repository-owned, explicit
  `-ProgramKitRoot` parameter, no discovery) packs the exact package list
  and records each produced package's SHA-256 in
  `packages/local-feed.manifest.json` (committed). The manifest pins the
  Program Kit commit the feed was built from.
- `NuGet.Config` at the JTest root clears inherited sources and declares
  exactly two: `local-program-kit` (the folder feed) and `nuget.org`, with
  `packageSourceMapping` sending `Orbyss.ProgramKit.*` to the local feed and
  everything else to nuget.org — the same isolation pattern Program Kit's
  own consumer proof uses (`build/Invoke-IsolatedConsumerProof.ps1`).

### 4.3 Consumed packages

| Package | Used by | Used for |
| --- | --- | --- |
| `Orbyss.ProgramKit.Serialization.JSON` | Language, Engine, Reporting | Model-first System.Text.Json serialization, canonicalization (stable member order, invariant formatting) for the canonical result document and digests. |
| `Orbyss.ProgramKit.Artifacts` | Reporting | Revision/digest primitives (`sha256:` content digests) used in result documents and the catalog. |
| `Orbyss.ProgramKit.DotNet` + `Orbyss.ProgramKit.CommandLine` | development time only | Backed `program-kit dotnet generate-host console` operation generating the CLI host from a typed Open Console document (see §8 and D1). Generated applications do not reference the generator at runtime. |

Anything further (Modularity, Tasks) is deliberately **not** consumed in
2.0.0; JTest's extension surface (D4) does not yet justify it. This keeps
the runtime dependency footprint reviewable.

## 5. The JTest language 2.0 (contract)

### 5.1 Identity and versioning

- Every suite document declares `"$schema"`/`"jtest": "2.0"` (exact
  discriminator: top-level required property `"jtest"` with value `"2.0"`).
- The authoritative contract is a **versioned JSON Schema**,
  `schemas/jtest-suite-2.0.0.schema.json`, published inside the
  `JTest.Language` package and embedded in the CLI.
- A machine-readable **language manifest**
  (`schemas/jtest-language-manifest-2.0.0.json`) enumerates every step type,
  assertion operator, expression form, scope, and constraint with
  descriptions and examples — the agent-facing description of the language.
  `jtest describe` emits it (see §8).
- Compatibility policy: within 2.x, additions are backward compatible
  (documents valid under 2.0 stay valid); removals or semantic changes
  require a major language version. Schema files are immutable once
  released; a new version is a new file.

### 5.2 Document shape (kept from 1.x, formalized)

Suite: `jtest` (required), `info` (name/description), `using` (template file
paths), `env`, `globals`, `secrets` (see §9), `tests[]`.
Case: `name` (required), `description`, `steps[]` (required), `datasets[]`
(each: `name`, `case` object).
Template file: `components.templates[]`, each `name`, `description`,
`params` (typed: `type`, `required`, `default`, `description`), `steps[]`,
`output` map.

Step catalog (discriminator `type`):

| type | Purpose | Key properties |
| --- | --- | --- |
| `http` | Perform one HTTP request | `method`, `url`, `headers`, `query`, exactly one of `body`/`file`/`formFiles`; result exposes `request`, `response` (status, headers, parsed body, raw body, timing) |
| `assert` | Evaluate assertions only | `assert[]` |
| `wait` | Delay | `ms` |
| `use` | Invoke a template | `template`, `with` map |
| `for` | Iterate over items | `items` (array or expression), `as`/`indexAs` (explicit names, defaults `item`/`index`), `steps[]`, optional `delayMs` |
| `while` | Poll until condition | `condition` (assertion object), `timeoutMs` (required), `delayMs`, `steps[]` |

Common step properties: `id` (named result), `name`, `description`,
`save` map, `assert[]`. Assertion operators keep the 1.x set (equals,
notEquals, contains, notContains, exists, notExists, greaterThan,
lessThan, greaterOrEqual, lessOrEqual, between, in, matches, startsWith,
endsWith, length, empty, notEmpty, type) with formalized operand typing and
invariant-culture comparison rules recorded per operator in the schema and
manifest.

### 5.3 Expressions and scopes (formalized semantics)

- Two token forms only: `{{$.<jsonpath>}}` resolved against the execution
  context, and `${ENV_NAME}` resolved against process environment variables
  at suite load.
- Scopes: `$.env` (suite/CLI-provided, immutable during a run), `$.globals`
  (suite-scoped, mutable, persists across cases and dataset runs in file
  order), `$.case` (current dataset row, read-only), `$.ctx` (current
  execution frame scratch space), `$.this` (previous step result in the
  current frame), and `$.<stepId>` (result of the identified step in the
  current frame). Scope names are reserved; `save` targets must address
  `$.ctx.*` or `$.globals.*` explicitly (fail-closed otherwise).
- **Fail-closed resolution (R-LANG-3):** a token whose path resolves to
  nothing is an error with a stable diagnostic, failing the step — never a
  silent `null`. An explicit `default` is expressible via assertion/step
  design, not by silent coercion.
- A string that is exactly one token resolves to the typed value; a string
  containing token(s) among other text resolves each token position-exactly
  (no global text replacement) and stringifies values with invariant
  culture.
- Nesting depth is bounded (8) with a specific diagnostic on overflow.
- **Template visibility (R-LANG-4):** inside a template: `params`
  (from `with` + defaults), `case`, and read-only `env` and `globals` are
  visible; the template gets a fresh `ctx`; a template exports values only
  through its declared `output` map. Writes to `$.globals` from template
  scope are rejected by validation.
- Loop frames bind `as`/`indexAs` names in a child frame; shadowing an
  existing name is a validation warning diagnostic.

### 5.4 Validation and diagnostics (R-LANG-1, R-LANG-2)

Validation is exhaustive and fail-closed, in three deterministic layers:

1. JSON syntax;
2. exact JSON Schema conformance (unknown properties rejected);
3. semantic rules the schema cannot express: template references resolve,
   `with` satisfies declared params, save-target scopes legal, step `id`
   uniqueness per frame, expression syntax well-formed, reserved-name and
   shadowing rules.

Every diagnostic is a typed record: stable code (`JT****`, e.g. `JT0101`
unknown step type), severity, message, source file, JSON pointer, and
optional hint. Code ranges: `JT00xx` document/syntax, `JT01xx` structure,
`JT02xx` expressions, `JT03xx` templates, `JT04xx` datasets, `JT05xx`
assertions, `JT9xxx` internal. Codes are append-only; meanings never change.
`--diagnostics json` emits them as a canonical JSON array (agent-friendly).

## 6. Execution model and trace contract (R-TRACE-1, R-TRACE-2)

### 6.1 Execution semantics

- Discovery: explicit file patterns (globbing), deterministic ordering
  (ordinal path sort). Suites run in that order; cases in document order;
  dataset runs in dataset order.
- Every run executes under a `CancellationToken` with an overall
  `--timeout` and per-http-step `timeoutMs`; cancellation and timeout
  produce `cancelled`/`timedOut` outcomes, never lost results.
- Parallel suite execution (`--parallel N`) is Task-based (no
  sync-over-async), with per-suite isolated contexts and buffered per-suite
  console sections; trace and evidence are identical in content to a
  sequential run of the same suites (ordering in the run document stays
  discovery order).
- Failure behavior: a failed or errored step fails its case; remaining
  steps in the case are recorded as `skipped` nodes (visible, not
  invented); remaining cases still run. An engine exception becomes an
  `error` outcome node with diagnostic `JT9xxx` — never a swallowed
  console-only message.

### 6.2 Trace contract

Execution produces a single immutable tree per run. Every node carries:

- `path` — stable execution path, e.g.
  `suites/0/cases/2/datasets/1/steps/3/iterations/4/steps/0`;
- `kind` — `run | suite | case | datasetRun | step | templateInvocation |
  iteration | assertion`;
- `ordinal` (1-based among siblings), `iterationIndex` where applicable;
- identity: step `type`, `id`, `name`, template name, dataset name;
- `outcome` — `passed | failed | error | skipped | cancelled | timedOut`;
- timing: `startUtc`, `durationMs` (evidence recorded by the engine clock —
  part of the data, never re-read by writers);
- `diagnostics[]` (typed, same record shape as validation diagnostics);
- kind-specific evidence (http request/response snapshot after redaction,
  assertion actual/expected/operator, saved-value summaries, loop
  completion counts);
- `children[]`.

Loops emit one `iteration` node per iteration with the iterated item
(redacted as applicable) and full child steps — nothing is overwritten.
Aggregate outcomes are pure functions of children (a node cannot pass with a
failed child), which structurally removes the F1/F4 class of defects.

## 7. Evidence and reporting

### 7.1 Canonical result document (the evidence)

- One `result.json` per run conforming to
  `schemas/jtest-result-2.0.0.schema.json`: run metadata (tool version,
  language version, start time, arguments summary, environment name),
  aggregate counts, and the full trace tree.
- Serialized canonically via `Orbyss.ProgramKit.Serialization.JSON`
  (stable member order, invariant formatting, LF, UTF-8 without BOM), so
  identical evidence yields identical bytes (**R-DET-1**).
- `runId` = `<startUtc as yyyyMMddTHHmmssfffZ>-<first 8 hex of
  sha256(canonical trace bytes)>` — collision-resistant and derived only
  from evidence.
- The result document is the single source of truth. Every rendering is a
  projection; nothing appears in a report that is not in the document
  (**R-REPORT-1**, **R-SEC-3** — there is no debug/non-debug divergence;
  verbosity is a viewer concern).

### 7.2 Reports folder (human catalog mode — default)

Default location `./.jtest/reports/` (override `--report-dir`):

```
.jtest/reports/
  index.html          # committed viewer, copied if absent or version-older
  viewer.css
  viewer.js
  catalog.js          # window.__JTEST_CATALOG__ = {...}
  runs/<runId>/result.json
  runs/<runId>/result.js    # window.__JTEST_RUN__ = {...}
```

A static page opened via `file://` cannot list directories or `fetch()`
sibling files, so the writer maintains `catalog.js` (run index: runId, suite
names, outcome, counts, startUtc, durationMs) and a `result.js` script
wrapper per run; the viewer loads them with `<script>` tags, which work over
`file://`. The catalog update is deterministic: prior catalog + new entry →
exact output; ordering is by `startUtc` descending then `runId` (no clock
reads in the writer). Keep the browser tab open, rerun, press refresh — the
new run is at the top.

### 7.3 Standalone mode (pipelines)

`--report standalone --report-out <path>` writes exactly two artifacts:
`<path>/index.html` — one self-contained file with viewer CSS/JS and the
run data inlined — and `<path>/result.json` beside it for downstream
automation. No catalog, no auto-open.

### 7.4 Viewer (pure HTML/CSS/JS)

- Committed, hand-authored `index.html` + `viewer.css` + `viewer.js`; no
  framework, no build step, no external requests of any kind (works
  offline; CSP-friendly).
- Catalog view: chronological run list with outcome, counts, duration;
  failure-first sort toggle; text filter.
- Run view: aggregate header; failure-first ordering; collapsible trace
  tree with explicit step-into for template invocations and loop
  iterations (breadcrumb trail while descended); assertion tables;
  http request/response detail panes; diagnostics with codes; expand-all /
  collapse-all; deep-linkable node addresses via URL hash (`#path=...`).
- Rendering is DOM-API based (`textContent`, attribute setters); no data
  string is ever concatenated into markup (**R-SEC-2**). A fixture test
  renders a hostile result document (script tags, event handlers, broken
  unicode) and asserts inert output.
- Visual standard: polished typography, spacing, motion on expand/collapse,
  light and dark themes via `prefers-color-scheme` — quality bar reviewed
  as part of acceptance, not an afterthought.

### 7.5 Report URL and auto-open (decided behavior)

Every `run` invocation, in every mode, prints the absolute report location
as a `file:///...` URL on its own console line (ctrl-clickable). By default
in interactive sessions the CLI attempts to open it with the OS default
opener; failure logs a warning only — never an error, never a changed exit
code. `--open`/`--no-open` force the behavior; auto-open is suppressed when
output is redirected or a CI environment is detected (`CI` variable), where
the printed URL remains.

## 8. CLI contract

Commands (final surface — `create`, `export`, and `debug` from 1.x are
removed; F4/R-CLI-2, and debug-mode divergence is eliminated by design):

```
jtest run <patterns...> [--env-file <file>] [--env k=v]... [--globals-file <file>]
          [--report-dir <dir> | --report standalone --report-out <dir>]
          [--parallel <n>] [--timeout <ms>] [--open|--no-open]
          [--diagnostics text|json]
jtest validate <patterns...> [--diagnostics text|json]
jtest describe [--schema suite|result] [--output <file|->]
```

- `describe` emits the language manifest (default) or an exact embedded
  schema — the agent-facing contract, versioned with the tool.
- Exit codes (frozen): `0` all discovered suites passed; `1` at least one
  case failed, errored, timed out, or was cancelled; `2` usage, input,
  discovery, or validation failure (including "no files matched");
  `3` unexpected internal failure. There is **no path to exit 0 without a
  complete passing trace for every discovered suite** (R-CLI-1); the exit
  code is computed from the canonical result document itself.
- `--env` values split on the first `=` only; duplicate keys are a `JT`
  diagnostic, not an exception (F8).
- The CLI grammar is defined as a typed Open Console document
  (`hosting/jtest-open-console.json`) and the host is generated with the
  backed `program-kit dotnet generate-host console` operation; JTest
  implements the operation dispatchers (see open decision D1).
- Packaged as dotnet tool `jtest` (`PackAsTool`), version 2.0.0 line.

## 9. Security model (R-SEC-1..3)

- **Capture-time structural redaction.** Sensitivity is declared, not
  guessed: suite-level `secrets: ["$.env.token", ...]` path list plus CLI
  `--secret-env NAME`... entries; additionally all values resolved from
  `${ENV_NAME}` process variables and a fixed credential header set
  (`Authorization`, `Proxy-Authorization`, `Cookie`, `Set-Cookie`,
  `X-Api-Key`) are sensitive by default. Sensitive values are replaced by
  `"«redacted:sha256/8»"` markers **when evidence is captured** — redacted
  data never enters the trace, so no projection can leak it. Assertion
  evidence redacts actual **and** expected values (F6).
- Documented limitation (2.0.0): values derived from secrets (e.g.
  a substring saved to `ctx`) are not taint-tracked; the language manifest
  states this and recommends asserting on secrets via `exists`/`matches`
  rather than copying them.
- The viewer performs no network requests and executes no data-derived
  code; hostile-content fixture required (§7.4).
- Reports and result documents are safe to attach to CI artifacts by
  default.

## 10. Determinism rules (R-DET-1)

- Writers are pure: canonical result bytes are a function of the trace;
  catalog bytes are a function of (prior catalog, new entry); standalone
  HTML bytes are a function of (viewer assets, result document). Byte-exact
  re-render fixtures enforce all three.
- No writer reads clocks, environment, culture, RNG, or filesystem state
  beyond declared inputs; all times in evidence come from the engine at
  execution moments.
- All serialization invariant-culture; all file output UTF-8 (no BOM), LF.
- Engine execution is inherently nondeterministic (network, wall clock) —
  determinism claims apply to validation, generation, and rendering, per
  the Program Kit boundary.

## 11. Versioning, releases, licensing (R-REL-1)

- Single source of version truth: `Directory.Build.props` (`Version`
   `2.0.0-alpha.1` during rebuild). All packages and the tool share it.
- Language schema (2.0.0), result schema (2.0.0), and tool version are
  distinct identities; the result document records all three.
- `LICENSE` (MIT) added at the repository root; package metadata references
  it consistently.
- Publishing/releasing 2.0.0 is a separate later activity; the Program Kit
  release-cycle flows are unavailable, so release qualification will be a
  human-run process defined then — this design deliberately does not invent
  one.

## 12. Open decisions for the human (material alternatives)

- **D1 — CLI host: generated Open Console host (recommended) vs
  hand-authored host.** Recommended: define the grammar as a typed Open
  Console document and generate the host with the backed Program Kit
  operation, proving real Program Kit consumption and freezing
  grammar/exit codes as reviewable artifacts. Cost: contract-first
  authoring is stricter; if a generation-contract gap emerges mid-build the
  affected work unit stops and reports (no silent hand-rolled fallback).
  Alternative: hand-authored System.CommandLine host — simpler, but the
  CLI contract lives only in code.
- **D2 — Source-gate enforcement.** The Program Kit `CSharpGate` analyzer
  is not packable, so it cannot come from the local feed. Recommended:
  adopt the gate rules as JTest governance
  (`governance/csharp-source-quality-gate.md` referencing the Program Kit
  policy id/version) enforced by `.editorconfig` + built-in analyzers +
  review, and record the delta honestly. Alternative: add Program Kit as a
  dev-time git submodule solely for the analyzer project reference —
  stronger enforcement, but reintroduces the checkout coupling the human
  asked to avoid.
- **D3 — 1.x source disposition.** Recommended: work unit JT2-W010 deletes
  the 1.x `src/`, `tests/v*`, and stale docs in one reviewable commit (git
  history preserves everything; the rebuild starts clean). Alternative:
  keep 1.x alongside until 2.0 reaches acceptance — safer-feeling but
  guarantees a long period of two sources of truth.
- **D4 — Step extensibility surface.** 2.0.0 ships the fixed step catalog
  (§5.2) with the engine's internal step registry designed for extension
  but **not** exposed publicly. Public extensibility (custom step packages)
  is deferred until a concrete consumer exists. Confirm or widen.

Everything else in this document is a reversible detail resolved by the
design; objections in review are welcome and will be reconciled explicitly.

## 13. Traceability

| Requirement | Origin | Design answer | Plan unit |
| --- | --- | --- | --- |
| R-TRACE-1 | F1 | §6.2 iteration nodes | JT2-W050 |
| R-TRACE-2 | F2 | §6.2 paths/ancestry | JT2-W040 |
| R-REPORT-1 | F3 | §7.1 single evidence, no debug split | JT2-W040/W060 |
| R-CLI-1 | F4 | §8 exit codes from evidence | JT2-W070 |
| R-CLI-2 | F4 | §8 removed stub commands | JT2-W070 |
| R-LANG-1 | F5 | §5.1/§5.4 schema + fail-closed validation | JT2-W020 |
| R-LANG-2 | F5 | §5.4 typed diagnostics | JT2-W020 |
| R-LANG-3 | F8 | §5.3 fail-closed expressions | JT2-W030 |
| R-LANG-4 | F8 | §5.3 template visibility | JT2-W030/W050 |
| R-SEC-1 | F6 | §9 capture-time redaction | JT2-W040 |
| R-SEC-2 | F6 | §7.4 DOM-safe viewer | JT2-W060 |
| R-SEC-3 | F6 | §7.1 no debug divergence | JT2-W040 |
| R-DET-1 | F8 | §10 pure writers | JT2-W060 |
| R-REL-1 | F7 | §11 version/license alignment | JT2-W010/W090 |
