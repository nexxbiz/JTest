# Reporting

JTest produces one canonical, machine-readable **execution trace** per run. Every human-facing
report is a read-only projection of that trace — it adds nothing the trace doesn't carry and hides
nothing except through an explicit view choice.

## The canonical trace (`--trace`)

`jtest run --trace trace.json` writes the versioned trace. It preserves the full ancestry of a run:

```
run → suite → case → dataset → step → (template children | loop iterations) → assertion
```

Every node carries a stable id/path, kind, ordinal, timings, an outcome
(`passed | failed | errored | cancelled | timedOut | skipped`), and its diagnostics. Loop iterations
are all retained. The trace is the source of truth for the exit code and for every report, and it is
redacted (declared secrets, cookies, and authorization values are masked).

## The HTML report (`--report`)

`jtest run --report report.html` writes a single self-contained HTML file:

- **Self-contained** — all CSS/JS inlined, no external requests; opens offline.
- **Failure-first** — failed, errored, cancelled, and timed-out nodes are surfaced before passing
  detail.
- **Complete** — the full nested execution (templates, every loop iteration, assertions) is shown by
  default, not hidden behind a debug flag.
- **Searchable & keyboard-navigable** — filter by text or "failures only"; the tree is standard
  collapsible markup that meets WCAG 2.1 AA.
- **Safe** — all dynamic values are inert (no XSS), and secrets are redacted, so it is safe to attach
  as a pipeline artifact.

## Rendering a report from a saved trace

Because the report is a pure projection, you can keep the trace as the durable artifact and render a
report from it later with the same run's evidence.

## Other outputs

A console summary is always printed. A Markdown projection of the trace is also available via the
`run` output options.
