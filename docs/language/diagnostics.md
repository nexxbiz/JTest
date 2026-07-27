# JTest diagnostic code registry

This registry is append-only: codes are never renumbered, reused, or given a
different meaning. Every diagnostic carries a stable code, a severity, a
message, the source document, and an RFC 6901 JSON pointer.

Ranges: `JT00xx` document/syntax · `JT01xx` structure · `JT02xx` expressions ·
`JT03xx` templates · `JT04xx` datasets · `JT05xx` assertions · `JT9xxx`
internal.

| Code | Severity | Meaning |
| --- | --- | --- |
| JT0001 | error | The document is not syntactically valid JSON. |
| JT0002 | error | The document root is not a JSON object. |
| JT0101 | error | A required property is missing. |
| JT0102 | error | A property has the wrong JSON type. |
| JT0103 | error | An unknown property is present (documents are closed shapes). |
| JT0104 | error | A step declares an unknown `type`. |
| JT0105 | error | The `jtest` language discriminator is missing or unsupported. |
| JT0106 | error | An array that must contain at least one element is empty. |
| JT0107 | error | A string property holds a value outside its allowed set. |
| JT0108 | error | A step id is declared more than once in the same frame. |
| JT0109 | error | A save target addresses a scope that cannot be written. |
| JT0110 | error | An http step declares more than one body source. |
| JT0111 | error | A numeric property is outside its allowed range. |
| JT0112 | error | A reserved scope name is used as a step id or loop binding. |
| JT0201 | error | An expression token is malformed. |
| JT0202 | error | An expression token is opened but never terminated. |
| JT0203 | error | An expression token has an empty path. |
| JT0204 | warning | A loop binding shadows a name already visible in the frame. |
| JT0301 | error | A `use` step references a template that is not loaded. |
| JT0302 | error | A required template parameter has no argument and no default. |
| JT0303 | error | A `with` argument names a parameter the template does not declare. |
| JT0304 | error | A template step writes to `$.globals`, which templates may not do. |
| JT0305 | error | Two loaded templates share the same name. |
| JT0306 | error | Template invocations form a cycle. |
| JT0401 | error | Two datasets of one test case share the same name. |
| JT0501 | error | An assertion declares an unknown operator. |
| JT0502 | error | An assertion is missing an operand its operator requires. |
| JT9001 | error | Validation itself failed unexpectedly; the document is treated as invalid. |

Execution diagnostics (`JT06xx` runtime expression/step failures, `JT9xxx`
engine errors) are registered here as they ship with the engine work units.
