# Reports and evidence

## The canonical result document

Every run produces one `result.json` conforming to the published result
schema (`jtest describe --schema result`): run metadata, aggregate counts,
and the complete execution trace — every suite, case, dataset run, step,
template invocation, loop iteration, and assertion, each with a stable
path, ordinal, outcome, timing, diagnostics, and redacted evidence. The
document is serialized canonically (RFC 8785), so identical evidence yields
byte-identical files, and the `runId` derives from the trace digest.

**The result document is the single source of truth.** Every report is a
projection of it; nothing appears in a report that is not in the document,
and there is no debug mode that reveals more.

## Catalog mode (default, for humans)

`jtest run` maintains `.jtest/reports/`:

```
.jtest/reports/
  index.html      viewer (committed static HTML/CSS/JS, no toolchain, no CDN)
  viewer.css, viewer.js
  catalog.js      run index, newest first
  runs/<runId>/result.json   canonical evidence
  runs/<runId>/result.js     the same data, script-loadable over file://
```

Open `index.html`, keep the tab open, rerun tests, press refresh — the new
run appears at the top. The run view is failure-first with a collapsible
trace tree; step into templates, loops, and dataset runs with breadcrumbs,
and deep-link any node via the URL hash.

A page opened via `file://` cannot list directories or fetch sibling
files, which is why the writer maintains `catalog.js` and `result.js`
script wrappers.

## Standalone mode (for pipelines)

`jtest run --report standalone --report-out <dir>` writes exactly two
artifacts: one self-contained `index.html` (viewer and data inlined) and
`result.json` beside it for downstream automation. Auto-open never applies.

## Security

Sensitivity is declared, not guessed: suite `secrets` paths, `${NAME}`
substitutions, `--secret-env` values, and credential headers
(`Authorization`, `Proxy-Authorization`, `Cookie`, `Set-Cookie`,
`X-Api-Key`) are replaced by stable `«redacted:…»` markers when evidence is
captured — secrets never enter the trace, so no projection can leak them.
Assertion evidence redacts both actual and expected operands. The viewer
renders every piece of data through DOM text APIs and performs no network
requests. Known limitation: values derived from secrets (for example a
substring saved to `$.ctx`) are not taint-tracked; assert on secrets with
`exists` or `matches` instead of copying them.
