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

  function sortFailureFirst(nodes) {
    return (nodes || []).slice().sort(function (a, b) {
      return (SEVERITY[a.outcome] || 9) - (SEVERITY[b.outcome] || 9);
    });
  }

  function searchText(parts) {
    return parts.filter(function (p) { return p != null; }).join(" ").toLowerCase();
  }

  // Build a collapsible node. childrenBuilder appends the body content.
  function node(kindLabel, label, outcome, durationMs, extraSearch, buildBody) {
    var d = el("details", { class: "node", attrs: { "data-outcome": (outcome || "").toLowerCase() } });
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
      sortFailureFirst(it.steps).forEach(function (s) { body.appendChild(renderStep(s)); });
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
      sortFailureFirst(c.datasets).forEach(function (ds) { body.appendChild(renderDataset(ds)); });
      renderDiagnostics(body, c.diagnostics);
    });
  }

  function renderSuite(s) {
    return node("suite", s.name || s.filePath || "suite", s.outcome, s.durationMs, s.filePath, function (body) {
      if (s.filePath) body.appendChild(kv("file", s.filePath));
      renderDiagnostics(body, s.diagnostics); // suite crash surfaces here
      sortFailureFirst(s.cases).forEach(function (c) { body.appendChild(renderCase(c)); });
    });
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

  function buildControls(root) {
    var controls = el("div", { class: "controls" });
    var search = el("input", { attrs: { type: "search", placeholder: "Search suites, steps, assertions…", "aria-label": "Search report" } });
    var select = el("select", { attrs: { "aria-label": "Filter by outcome" } });
    select.appendChild(el("option", { text: "All results", attrs: { value: "all" } }));
    select.appendChild(el("option", { text: "Failures only", attrs: { value: "failures" } }));
    controls.appendChild(search);
    controls.appendChild(el("label", { text: "" }, select));

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

    app.textContent = "";
    app.appendChild(renderSummary(trace));
    app.appendChild(buildControls(app));

    if (trace.environment) {
      var envNode = el("details", { class: "node", attrs: { "data-outcome": "passed", "data-text": "variables environment globals" } });
      var envSummary = el("summary");
      envSummary.appendChild(el("span", { class: "kind", text: "variables" }));
      envSummary.appendChild(el("span", { class: "label", text: "Environment & globals (masked)" }));
      envNode.appendChild(envSummary);
      var envBody = el("div", { class: "body" });
      Object.keys(trace.environment).forEach(function (k) { envBody.appendChild(kv(k, trace.environment[k])); });
      envNode.appendChild(envBody);
      app.appendChild(envNode);
    }

    var results = el("section", { attrs: { id: "results", "aria-label": "Execution results" } });
    var suites = sortFailureFirst(trace.suites);
    if (suites.length === 0) results.appendChild(el("p", { class: "empty", text: "No suites in this run." }));
    suites.forEach(function (s) { results.appendChild(renderSuite(s)); });
    (trace.diagnostics || []).forEach(function (dg) {
      results.appendChild(el("div", { class: "diag", text: "run: " + dg.message }));
    });
    app.appendChild(results);
    app.removeAttribute("aria-busy");
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", main);
  else main();
})();
