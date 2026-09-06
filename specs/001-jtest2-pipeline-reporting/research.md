# Phase 0 Research: JTest 2.0

All Technical Context unknowns are resolved below. Format: Decision / Rationale / Alternatives.

## R1. Canonical trace serialization & versioning

- **Decision**: Model the trace as plain C# records in `JTest.Core/Tracing`, serialized with
  `System.Text.Json` using explicit, stable property names and a top-level
  `traceSchemaVersion` (semver string) plus `toolVersion`. Outcome is a string enum
  (`passed|failed|errored|cancelled|timedOut|skipped`). Node ordering is deterministic
  (declaration order; loop iterations by index).
- **Rationale**: `System.Text.Json` is already in the runtime, is fast, and gives full control
  over names/versioning. Records give value semantics for golden-file comparison. A versioned
  root is required by FR-010 and the constitution.
- **Alternatives**: Newtonsoft.Json (extra dependency, not needed); embedding presentation in the
  model (violates Principle I); an int outcome enum (less legible in the JSON artifact).

## R2. JSON Schema for the JTest language

- **Decision**: Author a versioned JSON Schema (draft 2020-12) for the test-definition language,
  ship it as an **embedded resource** under `JTest.Core/Language/Schema`, and validate with
  **`JsonSchema.Net`** (json-everything), the sibling of the already-referenced `JsonPath.Net`.
  Step polymorphism handled via a discriminator (`type`) using `oneOf` + `if/then`. Validation
  emits located diagnostics (JSON Pointer path + message + rule id).
- **Rationale**: Reuses an ecosystem already in the dependency tree (no new family), supports
  2020-12 discriminated unions, and exposes evaluation results with instance locations for
  FR-031. Embedding guarantees the schema ships with the tool and matches its version (FR-029).
- **Alternatives**: Hand-rolled validation (the 1.0 mistake — FR-032 forbids mislabeling it);
  NJsonSchema (heavier, Newtonsoft-based); external schema file only (drift risk vs tool version).
- **Note**: Per FR-033 (clean break allowed), the schema is authored to the *correct* intended
  language, fixing 1.0 flaws (e.g. `WhileStep` missing its type identifier); breaking changes are
  logged in a `CHANGELOG`.

## R3. Self-contained HTML report

- **Decision**: Compose the HTML server-side in `JTest.Core/Reporting/Html` from a small set of
  **embedded resources** (one CSS file, one vanilla-JS file, one HTML shell) inlined into a single
  output file. The trace is embedded as an inert JSON island (`<script type="application/json">`)
  and rendered by the inlined JS; all text is HTML-encoded before embedding. Features: failure-first
  ordering, collapsible tree, client-side search/filter, keyboard navigation, light/dark via CSS,
  and a summary/rollup header.
- **Rationale**: Guarantees zero external requests (FR-018, SC-005) and offline use; vanilla JS
  avoids a build toolchain and supply-chain surface; embedding the trace makes the report a literal
  projection (Principle I) and enables client-side search at the target scale.
- **Alternatives**: A JS framework/CDN (violates self-contained + adds build step); pure static
  server-rendered HTML with no JS (fails interactive search/collapse at scale); Markdown-embedded
  HTML (the 1.0 anti-pattern that caused the XSS/escaping defects).
- **Escaping**: The JSON island is serialized with HTML-sensitive characters escaped (`<`, `>`, `&`,
  `U+2028/2029`) so no `</script>` breakout is possible; DOM built via `textContent`, never
  `innerHTML`, for dynamic values.
- **Assertion & body legibility (added post-implementation)**: the projector shows each assertion's
  subject (the original actual expression, e.g. the JSONPath) and its optional `description` so a
  passing assertion is self-explanatory instead of a bare resolved value; and it renders
  request/response bodies in a collapsible, pretty-printed JSON viewer with a copy control. Both are
  client-side affordances in the inlined vanilla JS — no external assets, values still built via
  `textContent`, and copy uses the Clipboard API with a hidden-`textarea` fallback for `file://`
  contexts (FR-050/FR-051). The trace gains `subject`/`description` on the assertion node (contract
  updated in `contracts/execution-trace.schema.json`); reports remain pure projections (Principle I).
- **Report layout calm (added post-implementation)**: only suites and cases render as boxes; nested
  nodes drop their per-level borders and instead hang off a single thin, downward-fading gradient rail
  aligned under the disclosure arrow (a JSON-tree feel that stays readable when deeply nested). A case's
  single default/unparameterized dataset is elided in the projection (its steps render directly under the
  case) — no evidence is hidden and the trace still carries the dataset node, so this stays within FR-017.

## R4. One encode + redact value pipeline

- **Decision**: A single `ReportValuePipeline` in `JTest.Core/Reporting` is the only path any
  dynamic value takes into any projection. It (1) resolves redaction (value-match against declared
  secrets + values under secret-like keys, applied to headers, bodies, query strings) and (2)
  applies format-appropriate encoding at the projection boundary (HTML projector encodes;
  Markdown projector escapes). Redaction happens before encoding.
- **Rationale**: FR-024 requires uniform, non-ad-hoc encoding; FR-025–FR-028 require value-based
  redaction everywhere. Centralizing removes the 1.0 inconsistency (some writers encoded, most
  didn't) and makes both properties testable in one place.
- **Alternatives**: Per-writer escaping (the 1.0 bug source); redaction only at HTTP layer (misses
  assertion/error/name paths); key-name-only masking (the ineffective 1.0 `SecurityMasker`).

## R5. Fixing `SecurityMasker` (Pillar C)

- **Decision**: Rework masking to register secret **values** (from declared secrets and from values
  found under secret-like keys anywhere in the payload) and replace every occurrence of those
  values in projected strings — including JSON bodies and query strings — with a fixed mask token.
  Keep the secret-like key list but use it to *discover values to mask*, not as the only trigger.
- **Rationale**: Directly fixes the confirmed defect where `RegisterForMasking("requestBody", ...)`
  never matched because the key `requestBody` isn't secret-like, so bodies were printed in full.
- **Alternatives**: Content-entropy heuristics (rejected in clarify — false positives); regex-only
  detection of known token shapes (kept as an optional future add, not required).

## R6. Exit-code contract

- **Decision**: `ExitCodeService` maps the aggregate run outcome to documented codes:
  `0` success; `1` test/assertion failures; `2` execution/suite error (crash, load/deserialize,
  setup failure, empty-but-expected discovery); `3` validation failure (`validate` command / schema
  errors); `4` aborted (cancelled or timed out). Precedence when multiple classes occur:
  `2 > 3 > 4 > 1` (an execution error outranks a mere test failure). Codes documented in quickstart
  and README.
- **Rationale**: FR-008 requires distinct documented codes and a documented precedence; chosen
  ordering surfaces the most serious "the tool couldn't trust the run" conditions first.
- **Alternatives**: Single non-zero (rejected in clarify); two-tier (rejected in clarify);
  Unix-style 130 for cancellation (avoided for cross-platform determinism — a small stable code set
  is clearer for CI branching).

## R7. Cancellation & timeout model

- **Decision**: Thread one `CancellationToken` from the CLI (linked to `Ctrl+C`/`SIGINT` and to an
  optional overall run timeout) through executors and steps. On cancellation, in-flight nodes are
  closed with outcome `cancelled` and not-yet-started nodes recorded as `cancelled`; step/loop
  timeouts produce `timedOut`. Both are distinct from `failed`/`errored` and drive exit code `4`.
- **Rationale**: FR-006/FR-007 require first-class, distinct outcomes; the CLI already uses
  `Microsoft.Extensions.Hosting`, which provides console-lifetime cancellation.
- **Alternatives**: Treating cancellation as failure (loses the distinction the spec requires);
  thread aborts (unsafe/deprecated).

## R8. Parallel vs sequential equivalence

- **Decision**: Parallel execution builds each suite's subtree independently and merges completed
  subtrees into the trace under a lock/concurrent collection; failures and exceptions in any branch
  are captured as nodes (never swallowed). A property test runs the same corpus both ways and
  asserts equal node sets and outcomes (ordering normalized by stable id).
- **Rationale**: FR-005 + SC-010. Fixes the 1.0 parallel path that incremented a never-read
  failure counter and dropped throwing suites from the bag.
- **Alternatives**: Serialize-only (removes a feature); shared mutable trace without isolation
  (race conditions).

## R9. Version single-sourcing & release integrity

- **Decision**: Add root `Directory.Build.props` defining `<Version>` (and deriving
  `PackageVersion`) for all projects; remove the per-project `PackageVersion`. Add a `LICENSE` file
  (MIT, NexxBiz) matching `<PackageLicenseExpression>` and the README link. CI adds a gate asserting
  the git tag equals the built version.
- **Rationale**: FR-034–FR-036; fixes the confirmed 1.0.0-vs-v1.0.3 drift, the version-less
  `JTest.Cli.csproj`, and the dead README license link.
- **Alternatives**: MinVer/GitVersion (adds tooling; deferrable — a props file is sufficient now);
  leaving CI to inject version at pack time only (the current fragile approach).

## R10. Disposition of existing outputs & leftover cruft

- **Decision**: Retain the console summary and keep a Markdown report, but re-implement Markdown as
  a projection of the canonical trace (not a de-facto source). HTML becomes the primary artifact.
  Remove stale untracked build output from the abandoned attempt: `src/.program-kit-build/` and the
  `bin/obj`-only dirs `src/JTest.{Engine,Evidence,Language,Reporting}/` and
  `src/JTest.Cli/obj/Generated/` (Program Kit source-generator output). The disabled
  `ResultsToMarkdownConverterTests.cs` (currently `<Compile Remove>`) is either restored against the
  new projection or deleted.
- **Rationale**: Aligns all outputs with Principle I; keeps a familiar text report for PR diffs; the
  leftover dirs are net10.0 build output that will confuse `dotnet build`/discovery.
- **Alternatives**: Drop Markdown entirely (unnecessary churn for users relying on it); keep leftover
  dirs (build noise, potential name collisions).
- **Output defaults & legacy-writer retirement (corrected post-implementation)**: dogfooding showed the
  legacy per-suite Markdown writer was still wired and, because `--output-format` defaulted to markdown
  and `--skip-output` defaulted false, it dumped a timestamped HTML-table `.md` into the working folder
  on every run. Decision: the result processor is reduced to the **console summary only**; all file
  output is the trace projection written by the run command. Default `run` writes `artifacts/report.html`
  + `artifacts/trace.json`; `-f markdown` writes `report.md` (the `MarkdownReportGenerator` projection)
  instead of HTML; explicit `--report`/`--trace` override. The legacy `src/JTest.Core/Output/` writer set
  is **removed** (FR-052). This finally delivers R10's "Markdown is a projection, not a de-facto source."

## R11. Testing strategy

- **Decision**: xUnit unit tests per concern plus golden-file tests: a corpus of definitions →
  expected canonical trace JSON and expected HTML (normalized). Dedicated corpora for false-green,
  loop retention, nesting/ancestry, cancellation, timeout, parallel-equivalence, XSS injection, and
  secret redaction. An integration test invokes the CLI and asserts exit codes.
- **Rationale**: Principle VII (NON-NEGOTIABLE) + SC-001…SC-012 are all mechanically checkable this
  way; golden files pin the projection so regressions are caught.
- **Alternatives**: Manual verification (non-repeatable); snapshot-only without exit-code integration
  tests (misses the primary false-green property).

## R12. Deterministic, isolated HTTP cookie handling

- **Context (verified on current source)**: `HttpStep` receives its `HttpClient` by reflection
  injection — `TypeDescriptorRegistry.GetArguments` calls `serviceProvider.GetService(HttpClient)`
  (`src/JTest.Core/TypeDescriptors/TypeDescriptorRegistry.cs:112`). `JTest.Cli` calls
  `services.AddHttpClient()` in **two separate** service collections: the host
  (`JTestApplication.cs:46`) and Spectre's own `ServiceCollection`
  (`JTestApplication.cs:79`). Cookie persistence today is accidental — it only works while the
  `IHttpClientFactory` pooled primary handler (default `UseCookies=true`, per-handler
  `CookieContainer`) is reused, and it is lost when the handler pool recycles.
- **Critical assessment of the reported fix**: the suggested `services.AddSingleton<CookieContainer>()`
  + `ConfigurePrimaryHttpMessageHandler` is the correct *shape* but wrong for JTest 2.0 as written:
  1. A process-wide **singleton** cookie jar cross-contaminates sessions across cases — it breaks
     test isolation and our parallel==sequential requirement (FR-005/FR-039). Sessions must be
     **scoped**, not global.
  2. With `IHttpClientFactory`, the `ConfigurePrimaryHttpMessageHandler` factory receives the
     **root** provider and the handler is **pooled/shared across DI scopes**, so a naively "scoped"
     `CookieContainer` will not flow through the pooled handler. Scope isolation cannot be achieved
     by DI lifetime alone on top of the factory's default pooling.
  3. Registering a singleton in each of the two separate service collections yields **two different
     instances** — not actually shared.
- **Decision**: Introduce a JTest-owned HTTP client abstraction (a small `IHttpClientProvider` in
  `JTest.Core/Http`) that hands each step a client bound to the **current execution scope's**
  `CookieContainer` (scope = test case by default; configurable to per-run). Achieve determinism by
  either (a) constructing the client's primary handler with an explicit per-scope `CookieContainer`
  and `PooledConnectionLifetime`/no-cookie-pooling so the jar is scope-owned, or (b) disabling
  handler cookie management (`UseCookies=false`) and having JTest apply/collect cookies against the
  per-scope container itself. The execution layer creates one scope per case and passes its cookie
  container down to every HTTP step in that case. Both `JTest.Cli` registration paths are reconciled
  to use this provider; no path falls back to an unmanaged default client.
- **Rationale**: Satisfies FR-038/FR-039/FR-043 and keeps FR-005 (parallel equivalence): cookies
  persist across steps in a case, are isolated across cases, and do not depend on handler-pool
  lifetime. Owning the client also gives one place to enforce timeouts (R7) and to feed
  request/response into the redaction pipeline (R4).
- **Alternatives**: Process-wide singleton jar (rejected — isolation/parallel break); relying on
  factory defaults (the current accidental behavior); per-step new `HttpClient` with its own jar
  (breaks cross-step persistence).
- **Redaction tie-in**: `Cookie`, `Set-Cookie`, and `Authorization` are treated as secret-like keys
  by the value pipeline (R4/R5) so session credentials are masked in the report and trace (FR-042).
  The existing `HttpStepResultDataWriter.cs:169` already lists these as sensitive — that intent is
  carried into the centralized pipeline.

## R13. HTTP headers as a case-insensitive keyed map

- **Context (verified against code)**: `HttpStep.GetResponseHeaders`
  (`src/JTest.Core/Steps/HttpStep.cs:326`) returns `object[]` of `{name, value}`, and request
  headers are built the same way (`HttpStep.cs:270-273`). (The legacy docs happen to assume keyed
  access, but that is corroboration only — the keyed-map shape is chosen on its own design merits,
  and the docs are regenerated to match; see R15.)
- **Decision**: Emit `headers` in the step's response/request data as a **case-insensitive keyed
  map** (`Dictionary<string,object?>` with `StringComparer.OrdinalIgnoreCase`). Single-valued
  headers map to a string; multi-valued headers (notably `set-cookie`) map to an **array of
  strings** so every value is addressable. This is the shape consumed by JSONPath (`$.this.headers`)
  and mirrored in the canonical trace's `HttpExchange` header maps.
- **Rationale**: Makes the documented access pattern actually work (FR-040); ordinal-ignore-case
  matches HTTP header semantics; arrays preserve `Set-Cookie` fidelity needed for R12.
- **Alternatives**: Keep the array-of-`{name,value}` shape (docs stay broken); comma-join
  multi-values (loses individual `Set-Cookie` entries — bad for cookie inspection). The legacy array
  form is dropped (clean break, FR-033) rather than dual-emitted, to avoid an ambiguous contract.

## R14. `status` vs `statusCode`

- **Context (verified against code/tests, NOT docs)**: response data emits
  `["status"] = (int)response.StatusCode` (`HttpStep.cs:295`); unit tests assert `$.this.status`
  (`tests/JTest.UnitTests/Steps/HttpStepTests.cs`, `ExampleUsageTests.cs`). The legacy docs also
  reference `statusCode`, but docs are legacy output and are explicitly NOT a source of truth here
  (see R15).
- **Decision**: Expose **both** keys, with `statusCode` as the canonical name and `status` as a
  retained alias; both are the integer HTTP status and both are covered by tests.
- **Rationale (design merit, not docs)**: `statusCode` is the more descriptive/conventional public
  name (mirrors `System.Net.HttpStatusCode`); `status` is retained so the existing green unit tests
  keep meaning and as a short ergonomic alias. The rewritten docs (final phase) describe
  `statusCode` as canonical — the docs follow this decision, they do not drive it. Dual keys are
  deliberate and documented, so honesty (Principle V) holds.
- **Alternatives**: `statusCode`-only + rewrite the existing tests (breaks a currently-green
  contract for little gain); `status`-only (less descriptive canonical name).

## R15. Documentation is a downstream projection, not a source of truth

- **Decision**: The `docs/` folder is treated as **legacy 1.0 output**, NOT authoritative input to
  this plan. No design decision may be justified by the current docs. As the **final phase** of the
  plan, `docs/` is fully rewritten from the implemented JTest 2.0 system (canonical trace, exit
  codes, HTTP contract, language schema, redaction) — the same fresh authoring applied to the JSON
  schemas. Every example/definition in the rewritten docs MUST validate against the shipped
  versioned JTest language schema and is CI-checked.
- **Rationale**: The current docs contain stale assertions/conditions/examples describing 1.0
  behavior we are deliberately leaving behind (clean break, FR-033). Letting them influence the
  design would re-import the very flaws we are removing. Docs must describe the system as built —
  this is Principle I generalized: reports and docs *project* truth, they do not *define* it.
- **Alternatives**: Incrementally patch docs (leaves legacy contamination and drift); keep docs as
  a design reference (rejected — exactly the contamination this guards against).

## R16. JSONPath filters & multi-match in `save` (dialect pinned)

- **Context (verified against code)**: `save` values resolve through
  `VariableInterpolator.ResolveVariableTokens` → `ResolveJsonPath` → `ExecuteJsonPath`, which calls
  `JsonPath.Parse(path).Evaluate(node)` (`VariableInterpolator.cs:287-308`) — the **same** evaluator
  used by assertions and interpolation. Multi-match already returns an array
  (`VariableInterpolator.cs:301-307`); `GetSaveValue` routes string save values through the
  interpolator (`StepProcessor.cs:264`). So filter/array support is largely a matter of *guaranteeing
  and testing* existing behavior, not building it.
- **Decision**: Treat JSONPath filter expressions and multi-match arrays as a **guaranteed,
  tested capability** in `save`, assertions, and interpolation. **Pin** the JSONPath evaluator
  (`JsonPath.Net`) version and **document the exact dialect** — it implements **RFC 9535**, whose
  filter selector is `$[?@.active==true]`, *not* the Goessner `$[?(@.active==true)]` the finding
  assumed. The language schema/docs describe the real accepted syntax; a filter corpus is added to
  the test suite.
- **Rationale**: Delivers the "array-filter in save" capability the user wants with high confidence
  and no dialect ambiguity; aligns with the formal-contract pillar (FR-046/FR-047).
- **Alternatives**: Promise Goessner `?()` verbatim (would be false — the pinned library may reject
  it); implement a custom filter layer (unnecessary; reuse the evaluator).

## R17. Unresolved-path diagnostics; case-sensitivity is a non-goal

- **Context (verified)**: `ResolveJsonPath` catches `JsonPathValueNotFoundException` and returns
  **`null`** (`VariableInterpolator.cs:277-280`); zero matches throw that exception
  (`VariableInterpolator.cs:292-293`). So a path that matches nothing (e.g. a camelCase mismatch)
  is silently coerced to `null` in both `save` and assertion values — confusing at best, and a
  false-green risk at worst (Principle II).
- **Decision**: Do **not** add case-insensitive matching (JSONPath is case-sensitive by spec;
  case-folding invites ambiguous/duplicate matches). Instead, when a path matches nothing, emit a
  **distinct diagnostic** ("path matched nothing" with the path + location) attached to the owning
  trace node, and distinguish it from a path that matched an actual `null`. Provide a strict option
  (fail on unresolved) and, at minimum, always make the no-match visible in the report so casing
  mismatches are obvious.
- **Rationale**: Fixes the true pain behind the camelCase note without a non-standard footgun;
  supports Principle II (no false-green) and the faithful-diagnostics goal (FR-048/FR-049). The
  user's "live check" of the API's real casing is a test-authoring step, not a JTest feature.
- **Alternatives**: Case-insensitive path matching (ambiguous, non-standard — rejected); keep
  silent-null (the current confusing behavior — rejected).
