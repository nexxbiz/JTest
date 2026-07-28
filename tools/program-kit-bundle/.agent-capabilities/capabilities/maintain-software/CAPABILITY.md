# maintain-software

## Identity and trigger

`maintain-software` owns one small, human-started,
architecture-compatible application change. Use it only when a human
explicitly asks to make or continue a bounded change and current source truth
shows that no material design decision is required.

## Purpose

Implement one coherent incremental change without imposing a full design and
planning cycle, while preserving the same backed refresh, integrity,
build/test, evidence, review, commit, and push discipline used by full
implementation.

## Non-goals

- Do not design or introduce a material architecture, mechanism, schema kind,
  security boundary, package family, runtime platform, or deployment model.
- Do not implement an approved multi-work-unit plan; use
  `implement-software-plan`.
- Do not infer semantics from ambiguous source edits or adopt hand-edited
  generated output as authoritative.
- Do not release, deploy, promote, publish, or upgrade Program Kit without the
  separate exact human authority required for that action.
- Do not create hooks, watchers, autonomous loops, MCP bindings, tool bindings,
  runtime services, or provider integrations.

## Inputs and outputs

Inputs:

- The human's explicit bounded change request.
- Current repository guidance, authoritative consumer source, accepted
  architecture, contracts, and generated-output ownership state.
- The exact inert software-change completion profile set at
  `../../supporting-resources/completion-profiles/software-change/completion-profile-set-1.0.0.json`.
- An exact human-approved Program Kit version when the requested maintenance
  unit includes a Program Kit upgrade.
- Separate network, secret, publication, destructive-action, or provider
  authority when genuinely required.

Outputs:

- One architecture-compatible source change and every affected derived
  artifact refreshed from authoritative inputs.
- Integrity, build, test, review, and evidence outcomes selected by the shared
  completion profiles.
- One coherent reversible commit and required push.
- A clear route to `design-software`, `implement-software-plan`, or a human
  decision when the change is not safely maintainable.

## Preconditions

- A human has explicitly requested the maintenance work.
- The selected repository and bounded outcome are clear.
- `maintain-software` is available in the canonical capability index and the
  active provider wrapper points to this exact definition.
- The accepted architecture and contracts admit the change without a material
  design decision.
- The shared completion profile manifest and every referenced profile match
  their exact bundle-bound digests.
- Generated output is either valid or the human has explicitly authorized its
  backed repair flow.

## Allowed actions

- Read current repository-owned source truth and exact relevant history.
- Edit authoritative consumer source, tests, documents, contract instances,
  configuration, and other files necessary for one bounded compatible change.
- Analyze explicit human edits and update authoritative documents or bindings
  when the mapping is unambiguous and validated.
- Use existing backed operations to refresh affected derived artifacts, verify
  integrity, build, test, record evidence, review, commit, and push.
- Apply an exact human-approved Program Kit version before affected refresh and
  verification.
- Use separately authorized backed publication only when the human explicitly
  requests publication.

## Prohibited actions

- Do not start work autonomously or treat routing, installation, or profile
  availability as authority.
- Do not guess an ambiguous mapping from consumer edits to Open Console,
  bindings, schemas, models, or generated artifacts.
- Do not hand-edit, adopt, or reproduce Program Kit-generated output or backed
  generator, compiler, analyzer, integrity, test, publication, commit, or push
  mechanics.
- Do not silently broaden the maintenance unit or bypass design for a material
  change.
- Do not auto-upgrade Program Kit, choose a version on the human's behalf, or
  continue after an unapproved version difference.
- Do not weaken gates, delete integrity evidence, expose secrets, inspect
  sibling repositories, write outside the selected consumer workspace, or
  write user-global provider configuration.

## Stop conditions

Stop and route to `design-software` when the change introduces or materially
alters architecture, a mechanism or schema kind, a security or trust boundary,
a package family, externally visible compatibility, deployment, or runtime
topology. Stop for a human decision when semantics, ownership, mapping,
authority, or scope is ambiguous. Stop on generated-output drift unless the
human explicitly authorizes the backed repair flow. Stop before any Program Kit
version change unless the human has approved the exact target version. Stop
when required verification can pass only by weakening policy.

## Source of truth and freshness

The current human request and current consumer repository bytes are
authoritative. Accepted architecture and contract instances own intended
meaning; generated output is never source truth. Re-read applicable guidance,
capability availability, completion-profile bytes, Program Kit selection,
generated-output integrity, and working-tree state before mutation and before
commit. Do not rely on remembered mappings, cached versions, or unrelated
history.

## Procedure

1. Confirm the human-started request, selected consumer workspace, and one
   coherent intended outcome.
2. Read repository guidance, active capability registration, accepted
   architecture and contracts, exact Program Kit selection, and working-tree
   ownership.
3. Classify the change. Admit only a bounded architecture-compatible unit;
   route material change to `design-software`, approved plan execution to
   `implement-software-plan`, and ambiguity to the human.
4. If an exact Program Kit upgrade was requested, restate the exact approved
   version, update that selection first, and stop on any unapproved
   substitution.
5. Analyze the authoritative source change. Update documents, bindings, and
   contract instances only when the mapping is unambiguous; otherwise stop
   without guessing.
6. Implement the smallest complete compatible change in authoritative
   consumer-owned source.
7. Resolve and follow the exact shared software-change completion profile set
   in order. Delegate refresh, integrity, build/test, optional publication,
   evidence, diff review, commit, and push to their existing backed
   operations.
8. Confirm every affected derived artifact was refreshed, integrity remains
   valid, required gates passed, and the diff contains one coherent unit with
   no unrelated work or secrets.
9. Commit the source, derived artifacts, locks, and evidence as one reversible
   historical event, push when required, report the exact commit, and stop.

Judgment owns admission, semantic mapping, escalation, and coherence.
Deterministic tooling owns only the backed transformations and checks; it
cannot expand authority or decide meaning.

## Verification and failure reporting

Report the exact source and derived artifacts changed, completion profiles and
backed operations selected, Program Kit version, commands, passing counts,
warnings, failures, evidence, commit, and push result. Verify unchanged
regeneration is byte-identical, integrity passes, no generated bytes were
adopted, optional publication had separate authority, and both source and
derived artifacts occur in the same coherent history event. Report any
unavailable check or route without claiming completion.

## Authority and safety boundaries

The human starts and bounds every maintenance unit. Installation, routing, and
supporting profiles grant no authority. Secrets, network access, publication,
destructive actions, provider changes, and exact Program Kit upgrades remain
separately controlled. Resolve and contain every filesystem target beneath the
selected consumer workspace. Prefer recoverable repair and stop before any
unapproved external or global write.

## Compatibility and versioning

Preserve this stable capability ID while bounded-change admission, escalation,
upgrade approval, completion-profile binding, evidence, and history semantics
remain compatible. Version the shared profile set independently. Changes to
authority, material-change classification, provider initialization, or upgrade
policy require explicit compatibility review. Rename, split, supersession, or
retirement requires human approval, index and bundle updates, wrapper
migration, and removal of stale registration.

## Provider wrapper mapping and drift check

The inert Codex adapter template at
`.agent-capabilities/provider-adapters/codex/maintain-software/SKILL.md` and
the inert Claude Code adapter template at
`.agent-capabilities/provider-adapters/claude/maintain-software/SKILL.md`
contain only provider registration metadata and one canonical-path token.
Initialization renders them only in an explicitly selected non-authoring
consumer workspace. Verify the exact canonical pointer, bundle and profile
digests, absence of copied procedure text, ownership-lock entry, and absence of
user-global or Program Kit authoring-workspace writes.
