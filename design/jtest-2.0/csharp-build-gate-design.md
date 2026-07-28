# JTest C# build gate design

Gate design identity: `pkid:design:jtest:csharp-build-gate` version 1.0.0
Status: awaiting human approval, together with the host rebuild amendment.
Produced under `design-csharp-build-gate@1.0.0`, explicitly started by the
human on 2026-07-27 with `StaticConformanceDisposition@1.0.0 = create-new`
(`gate/static-conformance-disposition.json`).

## 1. Static invariant inventory and allocation

Invariants of the host-rebuild design (and the standing repository
baseline), each allocated to the narrowest suitable layer:

| invariant | allocation |
| --- | --- |
| Warnings-as-errors (all variants), `AnalysisLevel latest-recommended`, nullable, style-in-build, locked restore | **compiler-baseline** — already active via `Directory.Build.props` / `.editorconfig`; the gate definition records it as the exact baseline component. |
| Generated host tree conforms to the Program Kit generated-source convention (canonical `ProgramKitGenerated` paths, ownership headers, no consumer edits inside generated files) | **program-kit-public-contract** — the public `PKCC` analyzer (§2). |
| Doc-digest integrity of the generated tree (byte tampering) | **non-static verification** — `program-kit dotnet verify-host` in the acceptance suite (amendment §Integrity); not a compiler concern. |
| One-type-per-file, namespace-mirrors-folders beyond IDE0130, fresh-receiver ban, static uncontracted helpers, writer purity (no clock/env/culture) | **consumer-owned analyzer (absent)** — today review-enforced per governance. Open decision §5. |

## 2. Selected public contract analyzer (exact)

| field | value |
| --- | --- |
| package | `Orbyss.ProgramKit.GeneratedSourceContract.Analyzers` 0.1.0-alpha.1 (local feed; add to the feed pack list) |
| manifest identity | `pkid:manifest:program-kit:generated-source-contract-analyzer` 0.1.0-alpha.1 |
| contract | `pkid:contract:program-kit:generated-source-convention` 1.0.0, digest `sha256:32beba7214f2b52af50eb03c8339203db83946ebd1d8ef73214368410c1f989c` |
| semantic owner | `pkid:domain:program-kit:generated-source-contract` — `PKCC` diagnostics stay Program Kit-owned; no copying, renaming, or suppression across owners. Private `PKCS` diagnostics never appear in this consumer gate. |

## 3. Gate composition, activation, and rules

- **Components:** compiler baseline (as recorded) + the §2 analyzer.
  Consumer-owned analyzers: none selected (open decision §5).
- **Activation matrix** (finite, conjunctive, fail-closed on unknown
  parameters) — `gate/activation-matrix.json`
  (`pkid:activation-matrix:jtest:csharp-gate-build-spine` 1.0.0): the §2
  analyzer activates for project `src/JTest.Cli.Host` (the generated
  tree), commands `build`/`test`, configuration `Release`, target
  `net10.0`; the compiler baseline activates for every solution project,
  same commands/configuration/target.
- **Verification profile** — `gate/verification-profile.json`
  (`pkid:profile:jtest:csharp-gate-exhaustive` 1.0.0): full-solution
  Release build and test with the gate active; used preflight, per work
  unit, and at closure.
- **Suppressions:** none permitted beyond the governance ledger
  (`governance/csharp-source-quality-gate.md` rule 5); any suppression is
  source-local, keeps the analyzer executing, and requires exact human
  reconciliation recorded in that ledger.
- **Temporary exceptions:** none defined; any future exception must be a
  typed, human-authorized, expiring condition with use evidence — no
  self-renewal.
- **Bootstrap / update / rollback:** establishment binds the analyzer
  package by exact digest from the local feed and records a selection
  lock; a Program Kit pin update regenerates the lock through the same
  backed `csharp-gate bind`/`verify` operations; rollback is reverting the
  lock and package reference in one commit.
- **Evidence:** the selection lock, bind output, and a passing
  `csharp-gate verify` run are committed as activation evidence; every
  later work unit depends on that evidence being current.

## 4. Establishment-first plan fragment

Work unit `JT2H-W010` (`workUnitKind: gate-establishment`) in
`host-rebuild-plan.json` — it precedes every product and closure unit:
add the analyzer package to the feed and `Directory.Packages.props`;
author the gate definition through `program-kit csharp-gate
validate-definition`/`bind`; activate per the matrix; run `csharp-gate
verify` and commit the lock plus evidence. Stop conditions: the backed
operations reject the definition, the analyzer package cannot be packed
from the pinned commit, or activation would require a `PKCS`-private
component.

## 5. Open decision for the human (with the review set)

Should a **consumer-owned analyzer** be designed for the review-enforced
governance rules (one type per file, fresh-receiver ban, static helpers,
writer purity)? Not required by this design's invariants; recommendation:
defer to a separate `design-csharp-build-gate` iteration after the host
rebuild lands, keeping this gate bounded. A "yes" would add analyzer
authoring (scaffolded via `csharp-gate scaffold`) as a second
establishment unit; a "no" keeps those rules governance-enforced as today.
