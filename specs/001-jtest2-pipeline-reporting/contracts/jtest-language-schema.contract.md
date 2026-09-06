# Contract: JTest Test-Definition Language Schema (JTest 2.0)

Deliverable for Pillar D (FR-029–FR-033): an authoritative, versioned JSON Schema (draft 2020-12)
for JTest test-definition files, shipped as an embedded resource in `JTest.Core/Language/Schema`
and enforced by `jtest validate`. Because there are no external consumers yet, the schema is
authored to the *correct* intended language and MAY break 1.0 definitions where that fixes a flaw
(FR-033); every breaking correction is recorded in `CHANGELOG.md`.

## Top-level document

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `version` | string | yes | Language version the file targets. |
| `info` | object | no | name/description metadata. |
| `env` / `globals` | object | no | Variables available to the suite. |
| `templates` | object/array | no | Reusable step templates referenced by `use`. |
| `tests` | array of Case | yes | Must be a non-empty array. |

## Case

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `name` | string | yes | |
| `description` | string | no | |
| `datasets` | array | no | Data-driven rows; each an object of parameters. |
| `steps` | array of Step | yes | Non-empty. |

## Step (discriminated union on `type`)

The schema uses `oneOf` + `if/then` on the `type` discriminator. Known step types (from the
current engine) and their required fields:

| `type` | Purpose | Key required fields |
|--------|---------|---------------------|
| `http` | HTTP request | `method`, `url` (headers/body/query optional) |
| `assert` | assertions | `assertions[]` each with `op`, `actual`, `expected` (as applicable) |
| `use` | invoke a template | `template` (ref), `with` (args) |
| `for` | for-loop | `items` (collection expr), `steps[]` |
| `while` | while-loop | `condition`, `steps[]`, optional `timeout`, optional `delay` |
| `wait` | delay | `duration`/`ms` |

Common optional fields on every step: `name`, `description`, `id`, `save`/`context` targets.

### Intended breaking corrections (FR-033 — logged in CHANGELOG)
- **`while` step must carry a type identifier** consistently (1.0 `WhileStep` lacked the
  `[TypeIdentifier]` the registry expects). Schema requires `type: "while"`.
- **Assertion operator naming** normalized to one canonical set (remove synonyms/ambiguity).
- **Reject unknown step types and unknown properties** (`additionalProperties: false` per step)
  rather than silently ignoring them.
- **Numeric/duration fields** typed and constrained (e.g. `timeout`, `delay` ≥ 0) instead of
  free-form.

## Validation & diagnostics (FR-030–FR-032)

- Enforce types, discriminators, required/optional fields, value constraints, and template/`use`
  reference resolution (a `use` must point at a declared template).
- Each violation → a machine-readable `Diagnostic` with `severity`, `message`, `location` (JSON
  Pointer, e.g. `/tests/2/steps/0/url`), and a stable `ruleId`.
- Honest reporting: the count of valid/invalid files equals the actual number processed; a
  structural-only pre-check is never labeled "schema validation".
- `jtest validate` exits `3` if any file fails (FR-004).

## Versioning

- The schema is versioned (its own semver, e.g. `1.0.0`) and pinned to the tool version it ships
  with. `$id` includes the version. Future language versions add a new schema document; the
  compatibility policy is documented per FR-033.
