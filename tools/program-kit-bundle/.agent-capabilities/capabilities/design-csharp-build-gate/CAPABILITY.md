# design-csharp-build-gate

## Identity and trigger

`design-csharp-build-gate@1.0.0` owns the design of one consumer-controlled
C# build gate and its exact gate-establishment implementation-plan fragment.
Use it only after a human explicitly starts this capability or explicitly
accepts a `design-software` invitation to design the missing gate.

## Purpose

Turn an approved or in-review software design's static invariants into a
reviewable gate design that composes exact compiler baselines, selected public
Program Kit contract-conformance analyzers, and zero or more separately owned
consumer-owned analyzers. Produce an establishment-first plan fragment, then
stop for human approval.

## Non-goals

- Do not implement, activate, approve, renew, or silently select a gate.
- Do not create a separate gate-implementation capability.
- Do not run Program Kit's private `Orbyss.ProgramKit.CSharpGate` on
  consumer-owned source.
- Do not make Program Kit own consumer-specific diagnostic semantics.
- Do not generate a consumer-owned analyzer unless its exact design and
  scaffold inputs are approved.
- Do not accept an empty analyzer selection or a temporary exception for the
  human.

## Inputs and outputs

Inputs:

- The human-started gate-design request and exact repository scope.
- The exact software design and `StaticConformanceDisposition@1.0.0`.
- Static invariant allocations, semantic owners, applicable source/project
  inventories, and desired implementation boundaries.
- Existing gate definitions, activation matrices, selection locks, public
  contract-analyzer manifests, consumer-owned analyzer definitions, and
  temporary-exception policy in scope.

Outputs:

- One versioned C# gate design with exact component identities and ownership.
- Exact profiles and a finite activation matrix over named operation,
  boundary, project, target framework, configuration, platform, and other
  reviewed parameters.
- Exact diagnostic, suppression, exception, receipt, bootstrap, update,
  rollback, package-closure, and evidence rules.
- An Implementation Plan `3.0.0` fragment whose dependency-ready
  `gate-establishment` work units precede every product or closure unit.
- Explicit unresolved decisions and a human approval request; no approval.

## Preconditions

- A human has explicitly started this capability.
- The software design identifies its exact static-conformance disposition.
- The repository has the backed C# build-gate contracts, authoring,
  workbench, testing, build integration, and verification operations required
  by the proposed fragment.
- The active provider has an initialized thin wrapper that points to this exact
  canonical definition. A missing wrapper is a setup blocker.

## Allowed actions

- Inspect relevant repository-owned designs, source inventories, analyzer
  manifests, gate artifacts, and deterministic evidence.
- Allocate each static invariant to the narrowest suitable enforcement layer.
- Select exact public Program Kit contract analyzers for their public
  semantics and exact consumer-owned analyzers for consumer-specific policy.
- Design a new consumer-owned analyzer when the human requested one and
  existing selections cannot enforce the allocated invariant.
- Define finite activation and typed temporary-exception conditions.
- Produce the exact establishment-first plan fragment and deterministic
  fixtures needed for later implementation.

## Prohibited actions

- Do not mutate product/runtime source under gate-design authority.
- Do not invoke implementation or scaffolding operations.
- Do not activate a package, targets file, analyzer, selection lock, or
  exception.
- Do not silently continue from `design-software`; this capability requires
  the human's explicit start.
- Do not infer that no gate is required, create empty-selection acceptance,
  or turn `blocked-unavailable` into an empty disposition.
- Do not copy, rename, suppress, or substitute diagnostics across semantic
  owners.
- Do not create provider hooks, watchers, MCP bindings, tool bindings, or
  autonomous execution.

## Stop conditions

Stop when the disposition is missing, implicit, unaccepted-empty, or blocked;
when ownership or applicability is ambiguous; when the private Program Kit
analyzer would be required on consumer source; when backed operations are
unavailable; or when a material choice needs human authority. Stop after
presenting the validated gate design and plan fragment for approval.

## Source of truth and freshness

Use current repository-owned source truth and exact referenced artifact bytes.
Re-read the software design, disposition, analyzer manifests, gate artifacts,
and plan immediately before binding digests. Generated catalogs and wrappers
are projections and never replace their canonical sources.

## Procedure

1. Confirm the human explicitly started `design-csharp-build-gate@1.0.0` and
   identify the exact software design and disposition.
2. Inventory the static invariants and allocate each to compiler baseline,
   public Program Kit contract analyzer, consumer-owned analyzer,
   architecture test, unit/integration test, or non-static verification.
3. Determine whether the disposition is `reuse-existing`, `extend-existing`,
   or `create-new`; stop for exact accepted-empty or blocked states.
4. Preserve semantic ownership: public `PKCC...` diagnostics belong to their
   public Program Kit contracts; consumer diagnostics belong to their
   consumer-owned analyzer; private `PKCS...` diagnostics remain internal to
   Program Kit.
5. Define exact analyzer identities, versions, digests, project/input
   inventories, profiles, suppressions, collision rules, same-assembly
   participation receipts, and runtime/package closure.
6. Define a finite activation matrix. Every non-execution must be outside
   exact applicability or authorized by a current typed temporary exception
   with use evidence; unknown parameters fail closed.
7. For every required but absent consumer-owned analyzer, ask the human
   whether it should be designed. If yes, specify exact scaffold inputs and a
   bounded work unit; if no, record an accepted empty selection or blocker
   only through the human-owned disposition decision.
8. Produce an Implementation Plan `3.0.0` fragment. Put bounded
   `gate-establishment` units first, including analyzer authoring when needed,
   gate definition, selection, activation, bootstrap verification, and lock
   evidence. Make all product and closure units depend on compatible
   activation evidence.
9. Define preflight, per-work-unit, generated-output, and final-closure gate
   executions for every applicable profile.
10. Validate exact schemas, inventories, digests, wrappers, bootstrap paths,
    update/rollback behavior, and package/runtime closure. Present the review
    set and stop for human approval.

## Verification and failure reporting

Verify that every static invariant has one explicit disposition; every selected
analyzer has one semantic owner and exact identity; activation is finite and
fail-closed; exceptions expire and cannot self-renew; gate-establishment is
dependency-first; and product work cannot start before compatible lock and
activation evidence. Report missing backing, unavailable analyzers, unresolved
ownership, and checks that could not run.

## Authority and safety boundaries

The human owns the disposition, analyzer selection, accepted empty value,
temporary-exception approval, gate-design approval, and implementation start.
This capability may propose but cannot make those decisions. Keep work within
the named repository and design artifacts. Network, secrets, destructive
actions, publishing, deployment, and release remain separately authorized.

## Compatibility and versioning

The canonical capability version is `1.0.0`. Preserve the stable capability ID
for compatible clarifications. Changes to trigger authority, empty-selection
authority, analyzer ownership, activation, exception, or plan-fragment
semantics require compatibility review and an updated definition digest.

## Provider wrapper mapping and drift check

The inert Codex adapter template at
`.agent-capabilities/provider-adapters/codex/design-csharp-build-gate/SKILL.md`
and Claude Code adapter template at
`.agent-capabilities/provider-adapters/claude/design-csharp-build-gate/SKILL.md`
each contain one canonical-path token and no procedure copy. Initialization
must verify the CapabilityBundle `3.0.0` manifest, render only the selected
provider wrapper, and bind exact canonical, template, output, and manifest
digests in the workspace ownership lock.
