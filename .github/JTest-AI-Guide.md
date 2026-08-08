# JTest guide

**This guide has been replaced by the documentation in [`docs/`](../docs/), which is the only
authoritative description of JTest 2.0.**

Start at [`docs/README.md`](../docs/README.md):

| Topic | Document |
|-------|----------|
| First suite | [getting-started.md](../docs/getting-started.md) |
| Language: steps, assertion operators, variables, `$.run`, `$.now`, `$.random` | [language-reference.md](../docs/language-reference.md) |
| HTTP steps, response contract, headers, cookies | [http-steps.md](../docs/http-steps.md) |
| CLI options and the exit-code contract | [cli-and-exit-codes.md](../docs/cli-and-exit-codes.md) |
| Reports and the canonical execution trace | [reporting.md](../docs/reporting.md) |
| CI integration | [ci.md](../docs/ci.md) |

The definitive contract is the shipped JSON Schema, enforced by `jtest validate`. Every example in
`docs/` is validated against it in CI, so those documents cannot drift from the implemented system.

## Why this file no longer holds a copy of the guide

It described JTest 1.x and had drifted badly from the shipped 2.0 system. Because it lives in
`.github/`, coding agents read it as repository guidance and wrote suites against things that do not
exist — an evaluation across six independent sessions traced real failures back to it. Specifically,
it documented:

- step types that do not exist (`conditional`, `database`, `queue`, `script`); the language accepts
  exactly `http`, `assert`, `use`, `for`, `while`, `wait`
- the assertion operator `matches`; the real operator is `match` (`jtest validate` now rejects the
  wrong name instead of failing at run time)
- an exit-code contract of only `0`/`1`; JTest 2.0 uses `0` success, `1` test failure,
  `2` execution error, `3` validation error, `4` aborted

A stale guide in an agent-visible location is worse than no guide: it is read with the same authority
as the real documentation. Keep JTest's documentation in `docs/`, where CI validates it.
