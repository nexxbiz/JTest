# JTest 2.0 — Final review

Release readiness review for the `001-jtest2-pipeline-reporting` branch.

## Security review (T082)

Report/trace output was reviewed for injection and leakage across all projections:

- **No XSS / active markup.** The HTML report renders all dynamic values via `textContent`
  (never `innerHTML`) and embeds the trace as an inert `<script type="application/json">` island;
  `System.Text.Json`'s default encoder escapes `<`, `>`, `&`, so no `</script>` breakout or live tag
  from data is possible. Covered by `HtmlReportTests` (script-breakout, injected-markup-inert) and
  by the Markdown projector's encode + pipe-escape (`MarkdownReportTests`).
- **Secrets redacted by default.** `Cookie`/`Set-Cookie`/`Authorization` and declared secret values
  are masked by value in headers, bodies, and query strings, in both the report and the persisted
  trace (`RedactionTests`, `ExecutionTraceAssemblerTests`).
- **Opt-in variable dump stays masked.** The environment/global dump is excluded by default and
  masks secret-like keys when included (`VariableDumpTests`).
- **Self-contained.** The HTML report makes zero external requests; safe to open offline and to
  publish as a pipeline artifact (`HtmlReportTests` self-contained assertion).

No injection or leakage paths were found.

## Constitution compliance re-check (T083)

Re-checked against `.specify/memory/constitution.md` v1.0.0. All eight principles hold:

| # | Principle | Status | Evidence |
|---|-----------|--------|----------|
| I | Evidence is canonical, reports are projections | ✅ | One `ExecutionTrace`; HTML/Markdown/exit-code all derive from it |
| II ★ | No false-green | ✅ | Trace-driven exit codes; crashing/empty/cancelled/timed-out never green (`FalseGreenTests`, `ExitCodeIntegrationTests`) |
| III | Complete, faithful history | ✅ | Full ancestry, unique ids, every loop iteration retained (`LoopRetentionTests`, `AncestryAndParallelTests`) |
| IV | Secure & redacted by default | ✅ | Value-based redaction + contextual encoding via `ReportValuePipeline` |
| V | Formal, versioned language contract | ✅ | Versioned JSON Schema + real located-diagnostic validation (`SchemaValidationTests`) |
| VI | Pipeline-first & deterministic | ✅ | Self-contained HTML, deterministic ordering, cancellation/timeout outcomes, parallel==sequential |
| VII ★ | Test-backed correctness | ✅ | Loops, nesting, cancellation, timeout, parallel, exit codes, escaping, redaction all covered |
| VIII | Honest, reconciled releases | ✅ | Single-sourced version, LICENSE present, CI tag==version gate (`ReleaseMetadataTests`) |

The two NON-NEGOTIABLE gates (II, VII) are satisfied.

## Release notes

JTest 2.0 turns JTest into a trustworthy CI/CD gate: honest, class-specific exit codes; a
self-contained, safe, failure-first HTML report projected from a versioned canonical trace; a formal
language schema with real validation; deterministic HTTP (keyed headers, `statusCode`/`status`,
per-case cookie sessions); and reconciled release metadata. See [CHANGELOG.md](../../CHANGELOG.md)
for the full list, including the intentional breaking corrections to the language.

Ready to tag and publish as `2.0.0`.
