# JTest language reference (2.0)

A JTest suite is a JSON document declaring end-to-end API tests: arrange,
act, assert. The authoritative shape contract is the published JSON Schema
(`jtest describe --schema suite`); the language manifest
(`jtest describe`) carries the semantics this page explains. Documents are
closed shapes: unknown properties are rejected.

## Suite document

```json
{
  "jtest": "2.0",
  "info": { "name": "Orders", "description": "Order lifecycle." },
  "using": [ "auth.templates.json" ],
  "env": { "baseUrl": "https://api.example.test", "apiToken": "${API_TOKEN}" },
  "globals": { "counter": 0 },
  "secrets": [ "$.env.apiToken" ],
  "tests": [ { "name": "…", "steps": [ … ], "datasets": [ … ] } ]
}
```

- `jtest` — the required language discriminator, exactly `"2.0"`.
- `using` — template file paths relative to the suite file.
- `env` — immutable run-level values. `${NAME}` tokens (permitted only in
  `env` and `globals` values) substitute process environment variables at
  load; an undefined variable fails loading (JT0602) and substituted values
  are sensitive by default.
- `globals` — suite-scoped mutable values persisting across cases and
  dataset runs in file order.
- `secrets` — context paths whose values are replaced by redaction markers
  in all evidence.
- `datasets` — named rows; the case runs once per row with the row exposed
  as `$.case`.

## Steps

Every step shares `id`, `name`, `description`, `save`, and `assert`. A step
`id` exposes its result as `$.<id>` within the frame.

| type | required | purpose |
| --- | --- | --- |
| `http` | `method`, `url` | One HTTP request. Exactly one of `body`, `file`, `formFiles`. Result: `{ request, response { status, headers, body, raw }, durationMs }`. |
| `assert` | `assert` (non-empty) | Assertions only; transparent to `$.this`. |
| `wait` | `ms` | Delay; number or expression; transparent to `$.this`. |
| `use` | `template` | Invokes a template with `with` arguments; result = the template's declared outputs. |
| `for` | `items`, `steps` | One iteration per item; bindings `as` (default `item`) and `indexAs` (default `index`); optional `delayMs`. Result: `{ items, completedIterations, allPassed }`. |
| `while` | `condition`, `timeoutMs`, `steps` | Do-while polling: runs the steps, then repeats while the condition assertion holds; the mandatory timeout yields a timedOut outcome. Optional `delayMs`. Result: `{ passes, timedOut }`. |

A failed or errored step fails its case; the remaining steps of the frame
are recorded as `skipped` — visible in the evidence, never invented.

## Expressions and scopes

- `{{$.path}}` resolves a JSONPath over the execution context. A string
  that is exactly one token yields the typed value; embedded tokens
  stringify with invariant culture. An unresolvable path fails the step
  (JT0601) — there is no silent null. Tokens do not nest, and resolved
  values are never re-interpreted as expressions.
- Scopes: `$.env` (read-only), `$.globals` (mutable, suite-scoped),
  `$.case` (dataset row, read-only), `$.ctx` (frame scratch), `$.this`
  (most recent result-producing step; `assert` and `wait` are
  transparent), plus loop bindings and step ids.
- `save` targets are explicit: `$.ctx.<path>` always, `$.globals.<path>`
  outside templates. Templates export values only through their `output`
  map.

## Templates

```json
{
  "jtest": "2.0",
  "components": { "templates": [ {
    "name": "authenticate",
    "params": { "baseUrl": { "type": "string", "required": true },
                 "user": { "type": "string", "default": "demo" } },
    "steps": [ … ],
    "output": { "token": "{{$.ctx.token}}" }
  } ] }
}
```

Inside a template: declared parameters, `$.case`, and read-only `$.env` and
`$.globals` are visible; the template gets a fresh `$.ctx`; caller step ids
are not visible; invocation cycles are rejected (JT0306).

## Assertions

`{ "op": "equals", "actual": "{{$.this.response.status}}", "expected": 200,
"description": "created" }`

Operators: `equals`, `notEquals`, `contains`, `notContains`, `exists`,
`notExists`, `greaterThan`, `lessThan`, `greaterOrEqual`, `lessOrEqual`,
`between` (expected `[min, max]`), `in`, `matches` (regular expression),
`startsWith`, `endsWith`, `length`, `empty`, `notEmpty`, `type`.
Comparisons use invariant culture; string operators compare ordinally. The
`while` condition is one assertion object with the same semantics.

## Validation

`jtest validate` runs three fail-closed layers — JSON syntax, exact schema
conformance, semantics (template references, parameters, cycles, save
targets, ids) — and reports every finding as a stable
[diagnostic](diagnostics.md) with an RFC 6901 pointer. `--diagnostics json`
emits them machine-readably.
