# Contract: HTTP Step — Response Data & Session Semantics (JTest 2.0)

Defines the runtime shape an `http` step exposes as `$.this` (consumed by assertions/JSONPath and
saved into context), and the cookie-session semantics. Derived from the HTTP hardening finding and
reconciled with the JTest 2.0 execution/trace design. Clean break allowed (FR-033): the legacy
array-of-`{name,value}` header shape is replaced, not dual-emitted.

## `$.this` response object

```jsonc
{
  "statusCode": 200,            // canonical HTTP status (int) — FR-041
  "status": 200,                // alias of statusCode (retained for existing tests/back-compat)
  "headers": {                  // case-insensitive keyed map — FR-040
    "content-type": "application/json",
    "set-cookie": [             // multi-valued headers are arrays
      "session=…; Path=/; HttpOnly",
      "csrf=…; Path=/"
    ]
  },
  "body": { /* parsed JSON when Content-Type is JSON, else raw string */ },
  "request": {                  // echo of what was sent
    "url": "…",
    "method": "POST",
    "headers": { /* same keyed-map shape */ },
    "body": "…"
  }
}
```

Access patterns that MUST work (documented today, currently broken):
- `$.this.statusCode` and `$.this.status` → integer status.
- `$.this.headers['content-type']` → case-insensitive lookup.
- `$.this.headers['set-cookie']` → array of all cookie directives.

### Header map rules
- Keys compared case-insensitively (`Content-Type` == `content-type`).
- Single-valued header → string; multi-valued header (e.g. `Set-Cookie`) → array of strings.
- Applies to both response and request header maps.

### Redaction (FR-042)
- `Cookie`, `Set-Cookie`, and `Authorization` values are redacted by default in the report and the
  persisted trace, via the central `ReportValuePipeline`. The raw values are still used at runtime
  for session handling; only the *projected/persisted* forms are masked.

## Cookie / session semantics (FR-038, FR-039, FR-043)

- **Persistence across steps**: within a single execution scope (a **test case** by default), all
  HTTP steps share one cookie container. A `Set-Cookie` from an earlier step (e.g. login) is
  automatically sent on later requests to matching hosts/paths — no manual `Cookie` header.
- **Isolation across scopes**: different cases (and different runs) get **separate** cookie
  containers. Under `--parallel`, no case observes another case's cookies. Sequential and parallel
  runs are equivalent (FR-005).
- **Independence from handler pooling**: behavior MUST NOT depend on `IHttpClientFactory` pooled
  primary-handler lifetime. JTest owns the cookie container for the scope (it is not left to the
  factory's default `UseCookies` handler), so recycling the handler pool does not drop sessions.
- **Scope configuration**: default scope is per-case; a per-run scope MAY be selectable for suites
  that intentionally share one session across cases.

### DI reconciliation (implementation note)
`JTest.Cli` currently calls `services.AddHttpClient()` in two separate service collections
(`JTestApplication.CreateHost` and `JTestApplication.CreateCommandApp`). Both MUST route through
the JTest HTTP client provider bound to the execution scope's cookie container; a process-wide
singleton `CookieContainer` is explicitly rejected (breaks isolation). Steps obtain their client
via that provider rather than a raw factory-resolved `HttpClient`.

## Tests (FR-037)
- Login → authenticated call in one case succeeds with no manual `Cookie` header (SC-013).
- Forced handler-pool recycle mid-suite: session still carried (SC-013).
- Two parallel cases as different users: no cookie cross-contamination (SC-014).
- `statusCode` and `status` both resolve; `headers['content-type']` case-insensitive; multi-valued
  `set-cookie` returns all values (SC-015).
- `Cookie`/`Set-Cookie`/`Authorization` redacted in report and trace (SC-015).
