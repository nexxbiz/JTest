# Authoring JTest suites as an AI agent

JTest is contract-first: never guess syntax from examples — read the
contracts the tool itself emits.

## The workflow

1. **Read the contract.** `jtest describe` emits the language manifest:
   every step type with required/optional properties and rules, every
   assertion operator with operand semantics, the scope and expression
   rules, and the secret-handling model. `jtest describe --schema suite`
   emits the exact JSON Schema your document must satisfy.
2. **Author the suite.** Documents are closed shapes — an unknown property
   is an error, so the schema tells you everything that is allowed.
3. **Validate before running.** `jtest validate <file> --diagnostics json`
   returns machine-readable findings: stable `JT****` code, severity,
   message, source, RFC 6901 pointer, and often a hint listing the allowed
   values. Fix and re-validate until the array contains no errors.
4. **Run.** `jtest run <patterns> --diagnostics json`. Exit codes are
   frozen: 0 pass, 1 test failures, 2 input/validation, 3 internal.
5. **Interpret the evidence, not the console.** The run prints the path of
   `result.json` — the canonical, schema-published
   (`jtest describe --schema result`) record of everything that happened.
   Walk `trace.children` recursively; every node has `path`, `kind`,
   `outcome`, `diagnostics`, and kind-specific `evidence` (http exchanges,
   assertion operands, loop counts). Failure analysis starts at the first
   non-passed leaf.

## Rules that catch agents out

- An unresolvable `{{$.path}}` fails the step (JT0601); there is no silent
  null. Use the `exists` operator to probe optional data.
- `assert` and `wait` steps do not change `$.this`; an assert step after an
  http step still sees the http exchange.
- `save` targets only `$.ctx.*` (always) or `$.globals.*` (outside
  templates). Templates export values only through `output`.
- `${NAME}` process-environment tokens work only inside suite `env` and
  `globals` values.
- Secrets: declare them (`secrets` array or `--secret-env`); evidence
  redacts them, and expected assertion operands are redacted too — compare
  with `equals` against another expression rather than pasting the secret
  literal into the document.
