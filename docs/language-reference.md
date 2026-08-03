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

### `use`

Invokes a reusable template. Required: `template`. Optional: `with` (arguments).

```json
{ "type": "use", "template": "login", "with": { "user": "alice" } }
```

### `for`

Repeats its inner `steps` once per item. Required: `items`, `steps`. The current item/index are
available as `{{$.item}}` / `{{$.index}}` (configurable via `item`/`index`). Every iteration is
recorded in the trace and report.

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

JSONPath follows **RFC 9535** (evaluated by JsonPath.Net). Filter selectors use `?@.expr`, and a
path that matches multiple nodes resolves to an array:

```json
{ "type": "assert", "assert": [
  { "op": "equals", "actualValue": "{{$.this.body.items[?@.active==true].id}}", "expectedValue": [1, 3] }
] }
```

A path that matches nothing is reported as a distinct diagnostic (it is not silently treated as
`null`). JSONPath property matching is case-sensitive — `version.id` does not match `version.Id`.

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
