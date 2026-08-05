# Phase 1 Data Model: Canonical Execution Trace

The **ExecutionTrace** is the single source of truth for a run (Principle I). All reports project
from it. Types live in `JTest.Core/Tracing`. Names below are logical; JSON property names are the
serialized contract (see `contracts/execution-trace.schema.json`).

## Enums

### Outcome
`passed | failed | errored | cancelled | timedOut | skipped`
- **passed**: node and all required children succeeded.
- **failed**: an assertion/expected condition did not hold.
- **errored**: an exception/crash prevented normal evaluation (load, deserialize, setup, infra).
- **cancelled**: stopped by cancellation before completing.
- **timedOut**: exceeded a configured step/loop/run timeout.
- **skipped**: intentionally not executed (e.g. downstream of an early loop exit or a skip flag).

Aggregation rule (parent outcome from children): `errored` > `timedOut` > `cancelled` > `failed`
> `passed`, with `skipped` ignored unless all children are skipped (then `skipped`).

### NodeKind
`run | suite | case | dataset | step | template | loop | iteration | assertion`

## Entities

### ExecutionTrace (root)
| Field | Type | Notes |
|-------|------|-------|
| traceSchemaVersion | string (semver) | Contract version of this trace format (FR-010). |
| toolVersion | string | JTest tool version that produced it (FR-010). |
| startedAt / endedAt | timestamp (ISO-8601 UTC) | Run window (FR-010). |
| durationMs | number | Convenience total. |
| outcome | Outcome | Aggregate over suites. |
| exitCode | int | Final process exit code (FR-008) — recorded for auditability. |
| counts | Rollup | Aggregate counts (see Rollup). |
| suites | SuiteResult[] | Children. |
| diagnostics | Diagnostic[] | Run-level errors (e.g. discovery/config failures). |
| environment | RedactedMap? | Present only when opted in; values masked (FR-027/28). |

### Rollup (value object, present on run/suite/case/dataset)
`total, passed, failed, errored, cancelled, timedOut, skipped` (all int).

### SuiteResult
| Field | Type | Notes |
|-------|------|-------|
| id / path | string | Stable id; `path` is the hierarchical execution path (FR-012). |
| kind | NodeKind = suite | |
| filePath | string | Source test file (redaction n/a; path only). |
| name / description | string | Encoded on projection. |
| startedAt / endedAt / durationMs | | |
| outcome | Outcome | A crashing suite is `errored` here — never dropped (FR-002). |
| counts | Rollup | |
| cases | CaseResult[] | |
| diagnostics | Diagnostic[] | Load/setup/teardown errors captured here (FR-002). |

### CaseResult
| Field | Type | Notes |
|-------|------|-------|
| id / path | string | |
| kind | NodeKind = case | |
| name | string | |
| datasets | DatasetResult[] | A case with no data-driven rows has one implicit dataset. |
| outcome / counts / timings | | |
| diagnostics | Diagnostic[] | |

### DatasetResult
| Field | Type | Notes |
|-------|------|-------|
| id / path | string | |
| kind | NodeKind = dataset | |
| label | string | Dataset name / row key. |
| parameters | RedactedMap | The data row (redacted). |
| steps | StepNode[] | Root steps for this dataset. |
| outcome / counts / timings | | |

### StepNode (covers step, template, loop; recursive)
| Field | Type | Notes |
|-------|------|-------|
| id / path | string | Stable, unique at every depth (FR-014) — fixes the flat step-number collision. |
| kind | NodeKind | `step` \| `template` \| `loop`. |
| stepType | string | Language type discriminator (e.g. `http`, `assert`, `use`, `for`, `while`, `wait`). |
| ordinal | int | 1-based position within its parent (FR-012). |
| name / description | string | Encoded on projection. |
| startedAt / endedAt / durationMs | | |
| outcome | Outcome | |
| request / response | HttpExchange? | For HTTP steps; bodies redacted+encoded via the pipeline. |
| assertions | AssertionResult[] | |
| contextChanges | ContextChanges? | Added/modified variables (redacted). |
| children | StepNode[] | Template-expanded child steps (their `parentId` = this node). |
| iterations | Iteration[] | Present when `kind = loop` (FR-013) — every iteration retained. |
| diagnostics | Diagnostic[] | Step error detail. |

### Iteration (child of a loop StepNode)
| Field | Type | Notes |
|-------|------|-------|
| id / path | string | |
| kind | NodeKind = iteration | |
| index | int | 0-based iteration number (FR-012). |
| startedAt / endedAt / durationMs | | |
| outcome | Outcome | |
| steps | StepNode[] | This iteration's own inner steps — never overwritten (FR-013). |
| diagnostics | Diagnostic[] | |

### AssertionResult
| Field | Type | Notes |
|-------|------|-------|
| id | string | |
| kind | NodeKind = assertion | |
| operation | string | e.g. `equals`, `contains`, `greaterThan`. |
| subject | RedactedValue? | The original actual expression being asserted (e.g. the JSONPath), so the report shows WHAT was checked, not only the resolved value. Redacted + encoded on projection. |
| expected / actual | RedactedValue | Redacted + encoded on projection (fixes 1.0 XSS). |
| description | string? | Optional human label of the check, from the assertion's `description` (encoded). |
| outcome | Outcome | `passed` or `failed`. |
| message | string? | Failure/diagnostic text (encoded). |

### Diagnostic
| Field | Type | Notes |
|-------|------|-------|
| severity | `error \| warning \| info` | |
| message | string | Encoded on projection. |
| location | string? | JSON Pointer / file:line into the source definition (FR-031). |
| exceptionType / stackTrace | string? | For `errored` nodes; stackTrace included in trace, shown in report only on opt-in. |

### Supporting value objects
- **HttpExchange**: `method, url, requestHeaders (HeaderMap), requestBody (RedactedValue),
  statusCode (int), status (int, alias of statusCode), responseHeaders (HeaderMap),
  responseBody (RedactedValue), durationMs`. `Cookie`/`Set-Cookie`/`Authorization` header values
  are redacted by default (FR-042).
- **HeaderMap**: a case-insensitive keyed map of header name → value, where a value is either a
  string (single-valued header) or an array of strings (multi-valued, e.g. `set-cookie`). This is
  the same shape steps read at runtime as `$.this.headers[...]` (FR-040). Values pass through the
  redaction pipeline.
- **Execution Scope / CookieJar**: the per-case execution scope (also called the session scope)
  owns a cookie container shared by its HTTP steps and isolated from other scopes (FR-038/FR-039).
  Not serialized into the trace as data; only its effects (Set-Cookie/Cookie headers, redacted)
  appear.
- **ContextChanges**: `added: RedactedMap, modified: RedactedMap`.
- **RedactedMap / RedactedValue / RedactedString**: logical wrappers indicating the value passes
  through the `ReportValuePipeline` (redaction by value+key, then format encoding) before it
  reaches any projection. In the persisted canonical trace, secrets are already replaced with the
  mask token (the trace is itself a shareable artifact — FR-025/26).

## Relationships (ancestry — FR-011)

```
ExecutionTrace(run)
 └─ SuiteResult (1..*)
     └─ CaseResult (1..*)
         └─ DatasetResult (1..*)
             └─ StepNode (0..*)            kind: step | template | loop
                 ├─ AssertionResult (0..*)
                 ├─ children: StepNode (0..*)      # template expansion
                 └─ iterations: Iteration (0..*)   # loops only
                     └─ steps: StepNode (0..*)     # per-iteration inner steps
```

Every node has `id` (stable) and `path` (e.g. `suite[2]/case[0]/dataset[0]/step[3]/iteration[1]/step[0]/assert[2]`),
so numbering and ancestry are unambiguous and reconstructable (FR-014, SC-009).

## Validation & invariants

- No child result may overwrite a sibling; loop `iterations.length` equals the number of executed
  iterations (SC-008), with unexecuted remainder represented as absent (not fabricated) and any
  intentionally skipped steps marked `skipped`.
- A suite that throws MUST appear with `outcome = errored` and a `Diagnostic` (FR-002); it MUST NOT
  be omitted.
- `counts` at each level MUST equal the aggregation of its children's outcomes.
- The persisted trace MUST contain no unredacted declared-secret values (SC-007).
- `traceSchemaVersion` and `toolVersion` MUST be non-empty (FR-010).
