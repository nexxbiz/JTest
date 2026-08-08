# HTTP steps

An `http` step sends a request and exposes the response as `$.this` for later steps and assertions.

## Request

| Field | Required | Description |
|-------|----------|-------------|
| `method` | yes | HTTP method, e.g. `GET`, `POST`. |
| `url` | yes | Request URL (may contain `{{ }}` tokens). |
| `headers` | no | Request headers. |
| `body` | no | Request body (JSON object or string). |
| `contentType` | no | Overrides the request content type. |
| `query` | no | Query-string parameters. |
| `file` / `formFiles` | no | File / multipart uploads. |

## Response (`$.this`)

```jsonc
{
  "statusCode": 200,          // HTTP status (canonical)
  "status": 200,              // alias of statusCode
  "headers": {                // keys are always lower case
    "content-type": "application/json",
    "set-cookie": ["session=…; HttpOnly", "csrf=…"]   // multi-valued headers are arrays
  },
  "body": { /* parsed JSON, or the raw string */ },
  "request": { "method": "GET", "url": "…", "headers": { }, "body": "…" }
}
```

- Read the status with `{{$.this.statusCode}}` (or `{{$.this.status}}`).
- **Header keys are normalized to lower case**, on both the response and `request.headers`. Address
  them that way regardless of the casing the server sent: `{{$.this.headers['content-type']}}` works
  whether the response said `Content-Type`, `content-type`, or `CONTENT-TYPE`.
  Header names are case-insensitive per RFC 9110, but JSONPath name selectors are case-**sensitive**
  per RFC 9535, so the map is normalized at capture rather than matched loosely at lookup.
  `{{$.this.headers['Content-Type']}}` therefore matches nothing and is reported as an unresolved
  path — see [unresolved paths](language-reference.md#variables-and-jsonpath).
- Multi-valued headers such as `set-cookie` are arrays.

## Cookies and sessions

Cookies are handled automatically and deterministically **per test case**:

- A `Set-Cookie` from one step (for example a login) is sent on later requests in the **same case**,
  with no manual `Cookie` header.
- Cookies are **isolated between cases** — one case never sees another's session, including under
  parallel execution.
- Behavior does not depend on HTTP connection pooling; JTest owns the cookie jar for each case.

```json
{
  "version": "1.0",
  "tests": [
    {
      "name": "authenticated flow",
      "steps": [
        { "type": "http", "method": "POST", "url": "https://api.example.com/auth/login",
          "body": { "user": "alice", "pass": "…" } },
        { "type": "http", "method": "GET", "url": "https://api.example.com/me" },
        { "type": "assert", "assert": [
          { "op": "equals", "actualValue": "{{$.this.statusCode}}", "expectedValue": 200 } ] }
      ]
    }
  ]
}
```

## Redaction

`Cookie`, `Set-Cookie`, and `Authorization` header values, and any value you declare as a secret,
are redacted by default in the report and the trace — in headers, bodies, and query strings. Reports
are safe to publish as pipeline artifacts.
