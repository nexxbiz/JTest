# Language reference

The JTest test-definition language is defined by an authoritative JSON Schema
(`jtest-language-1.0.0.schema.json`, shipped with the tool) and enforced by `jtest validate`. This
page describes that language. Field names are case-sensitive.

## Document

The top level of a suite file:

| Field | Required | Description |
|-------|----------|-------------|
| `version` | yes | Language version the file targets. |
| `tests` | yes | Array of test **cases**. |
| `info` | no | Metadata, e.g. `{ "name": "..." }`. |
| `using` | no | Array of template file paths to include. |
| `env` | no | Environment variables available as `$.env`. |
| `globals` | no | Global variables available as `$.globals`. |

## Case

Each item of `tests`:

| Field | Required | Description |
|-------|----------|-------------|
| `name` | yes | Case name. |
| `steps` | yes | Array of **steps**, run in order. |
| `description` | no | Free text. |
| `datasets` | no | Array of data rows; the case runs once per row (see below). |

## Steps

Every step has a `type`. Unknown types are rejected by validation. Common optional fields on any
step: `id`, `name`, `description`, `save` (see [Variables](#variables-and-jsonpath)), and `assert`.

### `http`

Performs an HTTP request. See [HTTP steps](http-steps.md) for the response contract.

Required: `method`, `url`. Optional: `headers`, `body`, `contentType`, `query`, `file`, `formFiles`.

```json
{ "type": "http", "method": "POST", "url": "https://api.example.com/login", "body": { "user": "a" } }
```

### `assert`

Checks one or more conditions. Required: `assert` (an array of operations). Each operation has an
`op` and the values it compares, e.g. `actualValue`/`expectedValue`.

```json
{ "type": "assert", "assert": [ { "op": "equals", "actualValue": "{{$.this.statusCode}}", "expectedValue": 200 } ] }
```

Every operation also accepts `description` (human label shown in the report) and `mask` (redact the
values). Operator names are matched case-insensitively, and `jtest validate` rejects an operator it
does not recognize, pointing at its location in the file.

#### Operators

| `op` | Checks | `expectedValue` |
|------|--------|-----------------|
| `equals` / `notequals` | Value equality. | The value to compare against. |
| `exists` / `notexists` | The value is present and non-empty. | — |
| `in` | The actual value is **one of** the expected values. | An array, e.g. `[200, 201]`. |
| `contains` / `notcontains` | A string or collection contains the value. | The member/substring. |
| `startswith` / `endswith` | String prefix/suffix. | The substring. |
| `match` | The string matches a regular expression. | The pattern. |
| `greaterthan` / `greaterorequal` | Numeric comparison. | The bound. |
| `lessthan` / `lessorequal` | Numeric comparison. | The bound. |
| `between` | Numeric range, inclusive. | `[min, max]`. |
| `length` | The length of a string or collection. | The expected length. |
| `empty` / `notempty` | A string or collection is empty. | — |
| `type` | The value's type: `string`, `integer`, `number`, `boolean`, `array`, `object`, `null`. | The type name. |

`in` is the "one of these values" operator, and its actual value is a **scalar** — asserting a status
code that may legitimately vary is its central use:

```json
{ "type": "assert", "assert": [
  { "op": "in", "actualValue": "{{$.this.statusCode}}", "expectedValue": [200, 201] }
] }
```

Note the split: `in` takes a collection as its **expectedValue**, while `length`, `empty` and
`notempty` inspect the **actualValue** as a collection.

### `use`

Invokes a reusable template. Required: `template`. Optional: `with` (arguments).

```json
{ "type": "use", "template": "login", "with": { "user": "alice" } }
```

### `for`

Repeats its inner `steps` once per item. Required: `items`, `steps`. The current item/index are
available as `{{$.item}}` / `{{$.index}}` (configurable via `item`/`index`). Every iteration is
recorded in the trace and report.

An **empty `items` list runs zero iterations and succeeds** — it is not an error. This is what makes
a "clean up whatever is left over" loop expressible, where having nothing left to do is the normal
case. (`steps` must still be non-empty.)

```json
{ "type": "for", "items": ["a", "b", "c"], "steps": [ { "type": "wait", "ms": 10 } ] }
```

### `while`

Repeats its inner `steps` until a condition stops holding or a timeout is reached. Required:
`steps`, `condition`, `timeoutMs`. Optional: `delayMs`. A timeout is a distinct outcome (the run is
aborted, exit code 4).

```json
{ "type": "while", "timeoutMs": 5000, "delayMs": 100,
  "condition": { "op": "equals", "actualValue": "{{$.this.body.status}}", "expectedValue": "ready" },
  "steps": [ { "type": "http", "method": "GET", "url": "https://api.example.com/status" } ] }
```

### `wait`

Pauses. Required: `ms`.

```json
{ "type": "wait", "ms": 250 }
```

## Variables and JSONPath

Values may contain `{{ ... }}` tokens that resolve JSONPath expressions against the execution
context:

- `$.this` — the previous step's result (for `http`, the response — see [HTTP steps](http-steps.md)).
- `$.env`, `$.globals` — variables declared in the suite.
- `$.case` — the current dataset row.
- `$.ctx` — values you have `save`d.
- `$.run` — generated values unique to this run (see below).

### `$.run` — unique values per run

A suite that creates a server-side resource with globally-unique identity (an HTTP route, a tenant
name, an account) otherwise passes once and then conflicts forever. `$.run` supplies a fresh value
each run so the suite stays re-runnable:

| Field | Value |
|-------|-------|
| `$.run.id` | Short token (8 hex chars), safe inside URLs, route names and identifiers. |
| `$.run.uuid` | Full v4 GUID. |
| `$.run.timestamp` | Run start, ISO-8601 UTC. |
| `$.run.epoch` / `$.run.epochMs` | Run start, Unix seconds / milliseconds. |

```json
{ "type": "http", "method": "POST", "url": "https://api.example.com/routes",
  "body": { "path": "/orders-{{$.run.id}}" } }
```

The values are **stable for the whole run**: every step and every suite in one `jtest run` sees the
same `$.run.id`, so a create step and a later fetch step agree without saving anything in between.
They are recorded in the trace under `run`, so a failed run remains reproducible.

JSONPath follows **RFC 9535** (evaluated by JsonPath.Net). Filter selectors use `?@.expr`, and a
path that matches multiple nodes resolves to an array:

```json
{ "type": "assert", "assert": [
  { "op": "equals", "actualValue": "{{$.this.body.items[?@.active==true].id}}", "expectedValue": [1, 3] }
] }
```

### Unresolved paths

A path that matches nothing is reported as a distinct diagnostic (it is not silently treated as
`null`). JSONPath property matching is case-sensitive — `version.id` does not match `version.Id`.

Concretely: an assertion whose `actualValue` or `expectedValue` contains a path that matched nothing
fails with that path named, rather than comparing a blank value and failing for what looks like a
data problem. A `save` whose source matched nothing records a warning on the step. The `exists` and
`notexists` operators are exempt — for them, matching nothing is the answer being tested.

JSONPath is **RFC 9535**, not JavaScript. The JavaScript-isms authors reach for most often do not
exist, and resolve to nothing:

| Instead of | Use |
|------------|-----|
| `{{$.this.body.length}}` | the `length` operator: `{ "op": "length", "actualValue": "{{$.this.body.items}}", "expectedValue": 3 }` |
| `{{$.this.body.items.count}}` | the `length` operator, as above. |
| `{{$.this.headers['Content-Type']}}` | lower-case header keys: `{{$.this.headers['content-type']}}` (see [HTTP steps](http-steps.md)). |

### `save`

The `save` object on a step copies resolved values into the context for later steps. Each entry maps
a **target path** to a **source expression**:

```json
{ "type": "http", "method": "POST", "url": "https://api.example.com/login",
  "save": { "$.ctx.token": "{{$.this.body.token}}" } }
```

Later steps can then use `{{$.ctx.token}}`.

## Datasets

A case with `datasets` runs once per row; each row is available as `$.case`. Each dataset becomes its
own branch in the trace and report.

## A complete example

```json
{
  "version": "1.0",
  "info": { "name": "User API" },
  "tests": [
    {
      "name": "creates and reads a user",
      "steps": [
        { "type": "http", "method": "POST", "url": "https://api.example.com/users",
          "body": { "name": "alice" },
          "save": { "$.ctx.id": "{{$.this.body.id}}" } },
        { "type": "assert", "assert": [
          { "op": "equals", "actualValue": "{{$.this.statusCode}}", "expectedValue": 201 } ] },
        { "type": "http", "method": "GET", "url": "https://api.example.com/users/{{$.ctx.id}}" },
        { "type": "assert", "assert": [
          { "op": "equals", "actualValue": "{{$.this.statusCode}}", "expectedValue": 200 } ] }
      ]
    }
  ]
}
```
