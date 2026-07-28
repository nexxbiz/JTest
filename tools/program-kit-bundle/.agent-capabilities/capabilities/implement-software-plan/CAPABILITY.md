# implement-software-plan

## Identity and trigger

`implement-software-plan` owns execution of an exact human-approved software
implementation plan. Use it when a human directly requests implementation and
supplies or identifies the exact approved design, plan, and approval record, or
explicitly continues a routed request into this flow.

## Purpose

Implement the bounded approved plan against current repository source truth,
verify each work unit, preserve reviewability, bind evidence, and stop on any
material architectural deviation.

## Non-goals

- Do not design a materially different architecture under implementation
  authority.
- Do not infer, create, repair, or supersede human approval.
- Do not expand into release, deployment, promotion, feed transport, or
  unrelated repository work.
- Do not create hooks, watchers, autonomous loops, MCP bindings, tool bindings,
  or speculative capabilities.
- Do not backdate a receipt or claim this capability produced bootstrap work
  completed before registration.

## Inputs and outputs

Inputs:

- The exact versioned design and implementation plan.
- For Implementation Plan `3.0.0`, the exact static-conformance disposition,
  gate design/definition/selection lock, activation matrix/profile references,
  and current compatible activation evidence.
- A valid, non-superseded human approval binding both exact digests.
- Current repository-owned source truth and applicable `AGENTS.md` guidance.
- Explicit secrets, network, provider, or destructive-action authority if a
  bounded plan task genuinely requires it.

Outputs:

- Scoped source, test, schema, fixture, and documentation changes.
- Per-work-unit verification and review notes.
- Commits and pushes required by repository guidance.
- Evidence binding exact inputs, outputs, diagnostics, and test results.
- A development receipt for the actual implementation event when requested.
- A clear deviation/blocker report when implementation must stop.

## Preconditions

- The human has requested implementation.
- The design and plan bytes match an `approved`, non-superseded decision with no
  open conditions.
- The working repository and branch are explicit and safe to modify.
- The planned work unit permits the intended files and behavior.
- Required active-provider capability registration is current.

## Allowed actions

- Read current repository source truth and exact approved artifacts.
- Edit only files within the active approved work unit.
- Run deterministic builds, tests, formatters, analyzers, package checks, and
  bounded fixture commands.
- Request scoped approval for an in-plan network, secret, provider, or
  destructive action when the environment requires it.
- Commit and push each completed work unit as required by repository guidance.

## Prohibited actions

- Do not weaken or bypass approval, compiler, analyzer, test, package, or
  conformance gates.
- Do not run Program Kit's private `Orbyss.ProgramKit.CSharpGate` on
  consumer-owned source. Execute only exact selected public Program Kit
  contract analyzers and exact selected consumer-owned analyzers there.
- Do not silently design, scaffold, select, implement, activate, suppress, or
  accept an empty gate, and do not renew a temporary exception.
- Do not silently redesign architecture or broaden the approved scope.
- Do not overwrite unrelated user changes, reset history, or delete broad paths.
- Do not inspect sibling repositories or unrelated history.
- Do not expose secrets in source, logs, receipts, commits, or responses.
- Do not publish packages, deploy applications, or create release state unless
  a separately approved future flow owns that work.

## Stop conditions

Stop before mutation when approval is absent, mismatched, rejected, conditional,
or superseded. Stop when current source truth makes a plan step unsafe or
materially different, a required file lies outside the approved work unit, a
gate can pass only by weakening policy, or new human authority is required.
For Implementation Plan `3.0.0`, also stop when the disposition or its exact
artifacts are missing, accepted-empty lacks its human decision, blocked is
selected, reuse lacks a compatible active lock, or product/closure work is
requested before create/extend activation evidence. Report the exact deviation
and smallest safe decision needed.

## Source of truth and freshness

The current repository, exact approved artifact bytes, approval record, and
human instructions are authoritative. Revalidate approval and working-tree
state before the first edit and before each materially dependent work unit. Do
not rely on cached artifact digests, sibling repositories, or remembered
implementation state.

The inert shared completion profile set at
`../../supporting-resources/completion-profiles/software-change/completion-profile-set-1.0.0.json`
owns the reusable source-review, refresh, integrity, build/test, optional
publication, evidence, diff-review, commit, and push procedures. Its exact
bundle-bound resources grant no authority and are not independently invokable.

## Procedure

1. Confirm the human implementation request, repository, branch, and work-unit
   boundary.
2. Load applicable guidance and validate the exact design, plan, and approval
   relationship.
3. Validate the Implementation Plan `3.0.0` static-conformance disposition and
   gate admission state. `reuse-existing` requires a compatible active
   selection lock at preflight. `create-new` and `extend-existing` admit only
   dependency-ready `gate-establishment` units until exact activation evidence
   exists. Human-accepted `not-justified` admits only the exact approved empty
   plan. `blocked-unavailable` admits no work.
4. Inspect working-tree and recent commit state; preserve unrelated human work.
5. Restate the current work unit's allowed edits, outputs, verification, and
   stop conditions.
6. Implement the smallest complete slice without speculative architecture.
   When establishment is required, implement the approved consumer-owned
   analyzer, gate definition, selection, activation, bootstrap evidence, and
   compatible lock in dependency order before starting product work.
7. Resolve and follow the exact inert shared completion profile set. Run
   focused verification while developing, then every required work-unit gate.
   At preflight, each applicable gate-establishment/product unit, generated
   output, and final closure, execute the finite applicable profiles. On
   consumer-owned source, run exact public `PKCC...` analyzers for public
   Program Kit contract semantics and exact consumer-owned analyzers for
   consumer-specific policy. Require per-analyzer participation and
   exception-use receipts. Correct failures without weakening policy.
8. Compare the result with the approved design and plan. If a material deviation
   exists, stop for human review.
9. Review the diff for scope, secrets, generated noise, version/digest updates,
   and deliberately omitted work.
10. Commit and push the completed work unit with an understandable message when
   repository guidance requires it.
11. Bind evidence and, when requested, emit a receipt for the actual registered
    capability event. Report completion and the next unstarted work unit.

Judgment decides whether observed differences are material deviations.
Deterministic tools implement mechanical changes and verify evidence; they do
not extend authority.

## Verification and failure reporting

Report exact commands, passing counts, warnings, failures, assumptions, and
checks that could not run. Verify the final diff is within the work-unit
allow-list, the working tree contains no unintended changes, and pushed commit
identity matches the reviewed result. A partial implementation is never
reported as a completed work unit. For gated plans, verify exact project/input
inventories, finite applicability, distinct analyzer ownership, required
receipts, active lock compatibility, and zero private-analyzer or runtime
leakage.

## Authority and safety boundaries

The approved plan authorizes only its bounded implementation work. Secrets,
network access, provider actions, destructive operations, deployment, and
release actions remain separately controlled. Resolve and validate exact
filesystem targets before destructive actions and prefer recoverable behavior.

## Compatibility and versioning

Preserve this stable capability ID for compatible execution refinements.
Changes to approval validation, deviation handling, mutation authority, or
evidence semantics require explicit compatibility review and an updated digest.
Renames, splits, supersession, or retirement require human approval, index and
wrapper migration, and removal of stale registration.

## Provider wrapper mapping and drift check

The inert Codex adapter template at
`.agent-capabilities/provider-adapters/codex/implement-software-plan/SKILL.md`
and the inert Claude Code adapter template at
`.agent-capabilities/provider-adapters/claude/implement-software-plan/SKILL.md`
contain only registration metadata and one canonical-path token each.
Initialization renders the selected workspace wrapper. Verify its exact
pointer, absence of copied procedure text, and workspace ownership binding.
