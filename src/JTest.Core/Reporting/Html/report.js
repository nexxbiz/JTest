(function () {
  "use strict";

  var SEVERITY = { errored: 0, failed: 1, timedOut: 2, cancelled: 3, passed: 4, skipped: 5 };
  function isFailure(o) { return o !== "passed" && o !== "skipped"; }

  function el(tag, opts) {
    var e = document.createElement(tag);
    if (opts && opts.class) e.className = opts.class;
    if (opts && opts.text != null) e.textContent = String(opts.text); // textContent → never parses markup
    if (opts && opts.attrs) for (var k in opts.attrs) e.setAttribute(k, opts.attrs[k]);
    for (var i = 2; i < arguments.length; i++) { var c = arguments[i]; if (c) e.appendChild(c); }
    return e;
  }

  function badge(outcome) {
    var cls = (outcome || "skipped").toLowerCase();
    return el("span", { class: "badge " + cls, text: outcome });
  }

  function dur(ms) {
    if (ms == null) return null;
    return el("span", { class: "dur", text: ms >= 1000 ? (ms / 1000).toFixed(2) + " s" : Math.round(ms) + " ms" });
  }

  function fmtMs(ms) {
    if (ms == null) return "—";
    if (ms < 1000) return Math.round(ms) + " ms";
    if (ms < 60000) return (ms / 1000).toFixed(1) + " s";
    var m = Math.floor(ms / 60000), sec = Math.round((ms % 60000) / 1000);
    return m + " min " + sec + " s";
  }

  function fmtClock(iso) {
    if (!iso) return "—";
    var d = new Date(iso);
    return isNaN(d) ? iso : d.toLocaleString();
  }

  function kv(key, value) {
    var row = el("div", { class: "kv" });
    row.appendChild(el("span", { class: "k", text: key + ": " }));
    row.appendChild(el("span", { text: value == null ? "—" : value }));
    return row;
  }

  function pre(value) {
    var text = typeof value === "string" ? value : JSON.stringify(value, null, 2);
    return el("pre", { text: text });
  }

  // "failures" surfaces the worst first; "execution" restores the order the runs actually happened
  // in. Execution order is recoverable from the node path, whose bracketed indices are assigned at
  // execution time — input[0]/suite[0] < input[0]/suite[1] < input[1]/suite[0] — so it survives the
  // failure-first ordering already applied to the embedded trace.
  var orderMode = "failures";

  // A path is a sequence of "kind[index]" segments assigned at execution time. Comparing segment by
  // segment — name first, then index NUMERICALLY — keeps suite[10] after suite[9], and still orders
  // segments that carry no index rather than treating them all as equal.
  function pathSegments(node) {
    var path = (node && (node.path || node.id)) || "";
    return path.split("/").map(function (segment) {
      var m = /^(.*?)\[(\d+)\]$/.exec(segment);
      return m ? { name: m[1], index: parseInt(m[2], 10) } : { name: segment, index: -1 };
    });
  }

  function compareExecution(a, b) {
    var sa = pathSegments(a), sb = pathSegments(b);
    for (var i = 0; i < Math.max(sa.length, sb.length); i++) {
      if (i >= sa.length) return -1;
      if (i >= sb.length) return 1;
      if (sa[i].name !== sb[i].name) return sa[i].name < sb[i].name ? -1 : 1;
      if (sa[i].index !== sb[i].index) return sa[i].index - sb[i].index;
    }
    return 0;
  }

  // `SEVERITY[o] || 9` would read the most severe rank, errored === 0, as falsy and score it 9 —
  // sorting errored BELOW passed, the exact opposite of failure-first.
  function severityRank(outcome) {
    return Object.prototype.hasOwnProperty.call(SEVERITY, outcome) ? SEVERITY[outcome] : 9;
  }

  function sortNodes(nodes) {
    var list = (nodes || []).slice();
    if (orderMode === "execution") return list.sort(compareExecution);
    return list.sort(function (a, b) {
      return severityRank(a.outcome) - severityRank(b.outcome);
    });
  }


  function searchText(parts) {
    return parts.filter(function (p) { return p != null; }).join(" ").toLowerCase();
  }

  // Build a collapsible node. childrenBuilder appends the body content.
  function node(kindLabel, label, outcome, durationMs, extraSearch, buildBody) {
    var d = el("details", { class: "node", attrs: { "data-outcome": (outcome || "").toLowerCase() } });
    // Only suites and cases are drawn as boxes; everything nested uses the indentation guide rail.
    if (kindLabel === "suite" || kindLabel === "case") d.className += " box";
    if (isFailure(outcome)) d.open = true; // failure-first: expand failing paths

    var summary = el("summary");
    summary.appendChild(el("span", { class: "kind", text: kindLabel }));
    summary.appendChild(el("span", { class: "label", text: label }));
    summary.appendChild(badge(outcome));
    var du = dur(durationMs); if (du) summary.appendChild(du);
    d.appendChild(summary);

    var body = el("div", { class: "body" });
    buildBody(body);
    d.appendChild(body);

    d.setAttribute("data-text", searchText([kindLabel, label, outcome, extraSearch]));
    return d;
  }

  function renderAssertion(a) {
    var d = el("div", { class: "assertion " + (a.outcome || "").toLowerCase() });
    d.appendChild(el("span", { class: "op", text: a.operation }));
    d.appendChild(el("span", { text: "  " })); d.appendChild(badge(a.outcome));
    // What the check is: a human description (if provided) and the asserted subject (the original
    // expression, e.g. the JSONPath) — so a passing assertion reads as what it verified, not a bare value.
    if (a.description) d.appendChild(kv("check", a.description));
    if (a.subject != null && a.subject !== "") d.appendChild(kv("subject", stringify(a.subject)));
    if (a.expected !== undefined) d.appendChild(kv("expected", stringify(a.expected)));
    if (a.actual !== undefined) d.appendChild(kv("actual", stringify(a.actual)));
    if (a.message) d.appendChild(el("div", { class: "diag", text: a.message }));
    d.setAttribute("data-text", searchText([a.operation, a.description,
      a.subject == null ? null : stringify(a.subject), a.outcome, a.message,
      stringify(a.expected), stringify(a.actual)]));
    return d;
  }
  function stringify(v) { return typeof v === "string" ? v : JSON.stringify(v); }

  function renderDiagnostics(body, diags) {
    (diags || []).forEach(function (dg) {
      body.appendChild(el("div", { class: "diag", text: (dg.severity || "error") + ": " + dg.message + (dg.location ? " (" + dg.location + ")" : "") }));
    });
  }

  function renderHttp(body, http) {
    var box = el("div");
    box.appendChild(kv("request", (http.method || "") + " " + (http.url || "")));
    if (http.statusCode != null) box.appendChild(kv("status", http.statusCode));
    if (http.requestBody != null && http.requestBody !== "") box.appendChild(renderBodyBox("request body", http.requestBody));
    if (http.responseBody != null && http.responseBody !== "") box.appendChild(renderBodyBox("response body", http.responseBody));
    body.appendChild(box);
  }

  // Pretty-print a body string as indented JSON when it parses as JSON; report whether it did.
  function prettyJson(raw) {
    if (raw == null) return { text: "", json: false };
    if (typeof raw !== "string") {
      try { return { text: JSON.stringify(raw, null, 2), json: true }; }
      catch (e) { return { text: String(raw), json: false }; }
    }
    var t = raw.trim();
    if (t && (t.charAt(0) === "{" || t.charAt(0) === "[")) {
      try { return { text: JSON.stringify(JSON.parse(raw), null, 2), json: true }; }
      catch (e) { /* not valid JSON — fall through and show raw */ }
    }
    return { text: raw, json: false };
  }

  // A body viewer: a header row (label + expand/collapse + copy) over a pretty, JSON-aware <pre>.
  function renderBodyBox(labelText, raw) {
    var pj = prettyJson(raw);
    var boxEl = el("div", { class: "bodybox" });

    var head = el("div", { class: "bodybox-head" });
    head.appendChild(el("span", { class: "k", text: labelText + (pj.json ? " (JSON)" : "") }));
    var toggle = el("button", { class: "btn", text: "Collapse", attrs: { type: "button", "aria-expanded": "true" } });
    var copy = el("button", { class: "btn", text: "Copy", attrs: { type: "button" } });
    head.appendChild(toggle);
    head.appendChild(copy);
    boxEl.appendChild(head);

    var pre = el("pre", { class: "bodybox-pre" + (pj.json ? " json" : ""), text: pj.text });
    boxEl.appendChild(pre);

    toggle.addEventListener("click", function () {
      var hidden = pre.classList.toggle("hidden");
      toggle.textContent = hidden ? "Expand" : "Collapse";
      toggle.setAttribute("aria-expanded", String(!hidden));
    });
    copy.addEventListener("click", function () { copyText(pj.text, copy); });
    return boxEl;
  }

  function copyText(text, btn) {
    function done(ok) { flash(btn, ok ? "Copied" : "Copy failed"); }
    try {
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text).then(function () { done(true); }, function () { done(fallbackCopy(text)); });
        return;
      }
    } catch (e) { /* fall through to execCommand */ }
    done(fallbackCopy(text));
  }

  // Clipboard API is unavailable in some offline/file:// contexts — fall back to a hidden textarea.
  function fallbackCopy(text) {
    try {
      var ta = document.createElement("textarea");
      ta.value = text; ta.setAttribute("readonly", "");
      ta.style.position = "absolute"; ta.style.left = "-9999px";
      document.body.appendChild(ta); ta.select();
      var ok = document.execCommand("copy");
      document.body.removeChild(ta);
      return ok;
    } catch (e) { return false; }
  }

  function flash(btn, msg) {
    var prev = btn.getAttribute("data-label") || btn.textContent;
    btn.setAttribute("data-label", prev);
    btn.textContent = msg;
    setTimeout(function () { btn.textContent = btn.getAttribute("data-label") || prev; }, 1200);
  }

  function renderStep(step) {
    var label = (step.name || step.stepType) + "  #" + step.ordinal;
    return node((step.kind || "step"), label, step.outcome, step.durationMs,
      searchText([step.stepType, step.path]), function (body) {
        if (step.description) body.appendChild(kv("description", step.description));
        if (step.http) renderHttp(body, step.http);
        (step.assertions || []).forEach(function (a) { body.appendChild(renderAssertion(a)); });
        renderDiagnostics(body, step.diagnostics);
        // template-expanded children keep execution order
        (step.children || []).forEach(function (c) { body.appendChild(renderStep(c)); });
        // loop iterations
        (step.iterations || []).forEach(function (it) { body.appendChild(renderIteration(it)); });
      });
  }

  function renderIteration(it) {
    return node("iteration", "iteration " + it.index, it.outcome, it.durationMs, it.path, function (body) {
      sortNodes(it.steps).forEach(function (s) { body.appendChild(renderStep(s)); });
      renderDiagnostics(body, it.diagnostics);
    });
  }

  function renderDataset(ds) {
    return node("dataset", ds.label || "default", ds.outcome, ds.durationMs, ds.path, function (body) {
      if (ds.parameters) body.appendChild(pre(ds.parameters));
      (ds.steps || []).forEach(function (s) { body.appendChild(renderStep(s)); }); // keep step order
      renderDiagnostics(body, ds.diagnostics);
    });
  }

  function renderCase(c) {
    return node("case", c.name, c.outcome, c.durationMs, c.path, function (body) {
      var datasets = c.datasets || [];
      // A single default (unparameterized) dataset is engine bookkeeping — collapse it away and
      // render its steps directly under the case. Data-driven cases (multiple/named/parameterized
      // datasets) still show each dataset.
      var lone = datasets.length === 1 &&
        (!datasets[0].label || datasets[0].label === "default") && !datasets[0].parameters;
      if (lone) {
        (datasets[0].steps || []).forEach(function (s) { body.appendChild(renderStep(s)); });
        renderDiagnostics(body, datasets[0].diagnostics);
      } else {
        sortNodes(datasets).forEach(function (ds) { body.appendChild(renderDataset(ds)); });
      }
      renderDiagnostics(body, c.diagnostics);
    });
  }

  // In a merged report every node id starts with input[n]; label the suite with the run it came
  // from, so a row is attributable without opening it.
  var runLabels = null; // index -> label, built from trace.merge

  function runLabelFor(path) {
    if (!runLabels) return null;
    var m = /^input\[(\d+)\]/.exec(path || "");
    return m ? runLabels[parseInt(m[1], 10)] : null;
  }

  function renderSuite(s) {
    var runLabel = runLabelFor(s.path || s.id);
    var d = node("suite", s.name || s.filePath || "suite", s.outcome, s.durationMs,
      searchText([s.filePath, runLabel]), function (body) {
        if (runLabel) body.appendChild(kv("run", runLabel));
        if (s.filePath) body.appendChild(kv("file", s.filePath));
        renderDiagnostics(body, s.diagnostics); // suite crash surfaces here
        sortNodes(s.cases).forEach(function (c) { body.appendChild(renderCase(c)); });
      });
    if (runLabel) d.querySelector("summary").appendChild(el("span", { class: "chip", text: runLabel }));
    return d;
  }

  // A merged report's root numbers do not describe one run, so they get their own panel rather than
  // a sentence: what the time figures mean, and which runs produced the results below.
  function renderMergePanel(merge) {
    var wrap = el("section", { class: "merge", attrs: { "aria-label": "Merged runs" } });

    var head = el("div", { class: "merge-head" });
    head.appendChild(el("h2", { text: "Merged from " + merge.sources.length + " runs" }));
    wrap.appendChild(head);

    var figures = el("div", { class: "merge-figures" });
    function figure(value, label, hint) {
      var f = el("div", { class: "merge-figure" });
      f.appendChild(el("b", { text: value }));
      f.appendChild(el("span", { class: "merge-figure-label", text: label }));
      if (hint) f.appendChild(el("span", { class: "merge-figure-hint", text: hint }));
      return f;
    }
    figures.appendChild(figure(fmtMs(merge.testDurationMs), "spent testing", "added up across the runs"));
    figures.appendChild(figure(fmtMs(merge.betweenRunsMs), "between runs", "restarts and waiting — not testing"));
    figures.appendChild(figure(fmtMs(merge.elapsedMs), "start to finish", "first run started to last run ended"));
    wrap.appendChild(figures);

    var table = el("table", { class: "merge-table" });
    var thead = el("tr");
    ["Run", "Started", "Tests", "Testing time", "Result", "Trace file"].forEach(function (h) {
      thead.appendChild(el("th", { text: h }));
    });
    table.appendChild(el("thead", {}, thead));

    var tbody = el("tbody");
    merge.sources.forEach(function (src) {
      if (src.gapBeforeMs) {
        var gapRow = el("tr", { class: "merge-gap" });
        var gapCell = el("td", { text: "↓ " + fmtMs(src.gapBeforeMs) + " with no tests running" });
        gapCell.setAttribute("colspan", "6");
        gapRow.appendChild(gapCell);
        tbody.appendChild(gapRow);
      }

      var c = src.counts || {};
      var row = el("tr");
      row.appendChild(el("td", { text: "run " + (src.index + 1) }));
      row.appendChild(el("td", { text: fmtClock(src.startedAt) }));
      row.appendChild(el("td", { text: (c.total == null ? src.suiteCount : c.total) +
        (c.failed ? " (" + c.failed + " failed)" : "") }));
      row.appendChild(el("td", { text: fmtMs(src.durationMs) }));
      row.appendChild(el("td", {}, badge(src.outcome)));
      row.appendChild(el("td", { class: "merge-src", text: src.source }));
      tbody.appendChild(row);
    });
    table.appendChild(tbody);
    wrap.appendChild(table);

    // Per-run $.run / environment: kept per source because no single set of them is the merged run's.
    merge.sources.forEach(function (src) {
      if (!src.run && !src.environment) return;
      var det = el("details", { class: "node box", attrs: { "data-outcome": "passed", "data-text": "run variables " + src.source } });
      var sum = el("summary");
      sum.appendChild(el("span", { class: "kind", text: "variables" }));
      sum.appendChild(el("span", { class: "label", text: "run " + (src.index + 1) + " — $.run and environment" }));
      det.appendChild(sum);
      var body = el("div", { class: "body" });
      if (src.run) Object.keys(src.run).forEach(function (k) { body.appendChild(kv("$.run." + k, src.run[k])); });
      if (src.environment) Object.keys(src.environment).forEach(function (k) { body.appendChild(kv(k, src.environment[k])); });
      det.appendChild(body);
      wrap.appendChild(det);
    });

    return wrap;
  }

  function renderSummary(trace) {
    var wrap = el("div");
    wrap.appendChild(el("h1", { text: "JTest Report" }));
    wrap.appendChild(el("p", { class: "subtitle", text:
      "tool " + trace.toolVersion + " · schema " + trace.traceSchemaVersion + " · exit " + trace.exitCode +
      " · " + trace.startedAt }));

    var c = trace.counts || {};
    var s = el("div", { class: "summary" });
    s.appendChild(el("span", { class: "metric" }, badge(trace.outcome)));
    function metric(label, value) {
      var m = el("span", { class: "metric" });
      m.appendChild(el("b", { text: value == null ? 0 : value }));
      m.appendChild(document.createTextNode(" " + label));
      return m;
    }
    s.appendChild(metric("total", c.total));
    s.appendChild(metric("passed", c.passed));
    s.appendChild(metric("failed", c.failed));
    s.appendChild(metric("errored", c.errored));
    if (c.cancelled) s.appendChild(metric("cancelled", c.cancelled));
    if (c.timedOut) s.appendChild(metric("timed out", c.timedOut));
    if (c.skipped) s.appendChild(metric("skipped", c.skipped));
    wrap.appendChild(s);
    return wrap;
  }

  function buildControls(root, onReorder) {
    var controls = el("div", { class: "controls" });
    var search = el("input", { attrs: { type: "search", placeholder: "Search suites, steps, assertions…", "aria-label": "Search report" } });
    var select = el("select", { attrs: { "aria-label": "Filter by outcome" } });
    select.appendChild(el("option", { text: "All results", attrs: { value: "all" } }));
    select.appendChild(el("option", { text: "Failures only", attrs: { value: "failures" } }));

    var order = el("select", { attrs: { "aria-label": "Order results" } });
    order.appendChild(el("option", { text: "Chronological", attrs: { value: "execution" } }));
    order.appendChild(el("option", { text: "Failures first", attrs: { value: "failures" } }));
    order.value = orderMode;

    controls.appendChild(search);
    controls.appendChild(el("label", { text: "" }, select));
    controls.appendChild(el("label", { text: "" }, order));

    order.addEventListener("change", function () {
      orderMode = order.value;
      onReorder();
      apply();
    });

    function apply() {
      var q = search.value.trim().toLowerCase();
      var failuresOnly = select.value === "failures";
      root.querySelectorAll("details.node").forEach(function (d) {
        var outcome = d.getAttribute("data-outcome");
        var matchesFilter = !failuresOnly || (outcome !== "passed" && outcome !== "skipped");
        var text = d.getAttribute("data-text") || "";
        var selfMatch = q === "" || text.indexOf(q) !== -1;
        var descMatch = q !== "" && d.querySelector("[data-text*='" + cssEscape(q) + "']") != null;
        var show = matchesFilter && (selfMatch || descMatch);
        d.classList.toggle("hidden", !show);
        if (q !== "" && (selfMatch || descMatch)) d.open = true;
      });
    }
    function cssEscape(s) { return s.replace(/['\\]/g, "\\$&"); }
    search.addEventListener("input", apply);
    select.addEventListener("change", apply);
    return controls;
  }

  function main() {
    var app = document.getElementById("app");
    var raw = document.getElementById("jtest-trace").textContent;
    var trace;
    try { trace = JSON.parse(raw); }
    catch (e) { app.textContent = "Failed to parse embedded trace: " + e.message; app.removeAttribute("aria-busy"); return; }

    // A merged report defaults to chronological: its story is what ran, then what happened in
    // between, then what ran next. A single run keeps failure-first.
    if (trace.merge) {
      orderMode = "execution";
      runLabels = {};
      trace.merge.sources.forEach(function (src) { runLabels[src.index] = "run " + (src.index + 1); });
    }

    app.textContent = "";
    app.appendChild(renderSummary(trace));
    if (trace.merge) app.appendChild(renderMergePanel(trace.merge));

    var results = el("section", { attrs: { id: "results", "aria-label": "Execution results" } });

    function fillResults() {
      results.textContent = "";
      var suites = sortNodes(trace.suites);
      if (suites.length === 0) results.appendChild(el("p", { class: "empty", text: "No suites in this run." }));
      suites.forEach(function (s) { results.appendChild(renderSuite(s)); });
      (trace.diagnostics || []).forEach(function (dg) {
        results.appendChild(el("div", { class: "diag", text: "run: " + dg.message }));
      });
    }

    app.appendChild(buildControls(app, fillResults));

    if (trace.environment) {
      var envNode = el("details", { class: "node box", attrs: { "data-outcome": "passed", "data-text": "variables environment globals" } });
      var envSummary = el("summary");
      envSummary.appendChild(el("span", { class: "kind", text: "variables" }));
      envSummary.appendChild(el("span", { class: "label", text: "Environment & globals (masked)" }));
      envNode.appendChild(envSummary);
      var envBody = el("div", { class: "body" });
      Object.keys(trace.environment).forEach(function (k) { envBody.appendChild(kv(k, trace.environment[k])); });
      envNode.appendChild(envBody);
      app.appendChild(envNode);
    }

    fillResults();
    app.appendChild(results);
    app.removeAttribute("aria-busy");
  }

  // The ordering rules are the one piece of report behaviour with no DOM in it, and the one a
  // merged report depends on to show runs in the order they happened. Exposing them lets the test
  // suite drive them directly; in a browser this is an unused property on the report's own object.
  var api = { sortNodes: sortNodes, compareExecution: compareExecution, setOrder: function (mode) { orderMode = mode; } };
  if (typeof globalThis !== "undefined") globalThis.JTestReport = api;

  // Only bootstrap when there is a document to render into, so the file can be loaded and its
  // ordering exercised without one.
  if (typeof document === "undefined") return;
  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", main);
  else main();
})();
