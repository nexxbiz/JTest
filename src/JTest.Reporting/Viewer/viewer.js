// JTest report viewer. Hand-authored, no toolchain, no external requests.
// Every piece of run data is rendered through textContent/createElement —
// data can never become markup.
"use strict";

(function () {
  const app = document.getElementById("app");

  // --- tiny DOM helpers (element structure is static; data goes in as text) ---
  function el(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined && text !== null) node.textContent = String(text);
    return node;
  }

  function clear(node) {
    while (node.firstChild) node.removeChild(node.firstChild);
  }

  function fmtMs(ms) {
    if (typeof ms !== "number") return "";
    if (ms < 1000) return ms.toFixed(0) + " ms";
    return (ms / 1000).toFixed(2) + " s";
  }

  function badge(outcome) {
    return el("span", "badge " + outcome, outcome);
  }

  // --- routing ---
  function currentRoute() {
    const hash = window.location.hash.replace(/^#/, "");
    const params = new URLSearchParams(hash);
    return { run: params.get("run"), node: params.get("node") };
  }

  function setRoute(runId, nodePath) {
    const params = new URLSearchParams();
    if (runId) params.set("run", runId);
    if (nodePath) params.set("node", nodePath);
    window.location.hash = params.toString();
  }

  // --- catalog view ---
  function renderCatalog(catalog) {
    clear(app);
    const head = el("div", "masthead");
    head.appendChild(el("h1", null, "JTest Reports"));
    head.appendChild(el("span", "sub", catalog.runs.length + " run(s) — refresh after a new run"));
    app.appendChild(head);

    const toolbar = el("div", "toolbar");
    const search = el("input");
    search.type = "search";
    search.placeholder = "Filter runs by suite, outcome, or run id…";
    toolbar.appendChild(search);
    app.appendChild(toolbar);

    const list = el("div");
    app.appendChild(list);

    function draw() {
      clear(list);
      const needle = search.value.toLowerCase();
      let shown = 0;
      for (const run of catalog.runs) {
        const haystack = (run.runId + " " + run.outcome + " " + (run.suites || []).join(" ")).toLowerCase();
        if (needle && !haystack.includes(needle)) continue;
        shown++;
        const card = el("div", "card catalog-run");
        card.appendChild(badge(run.outcome));
        const grow = el("div", "grow");
        grow.appendChild(el("div", "title", (run.suites || []).join(", ") || run.runId));
        grow.appendChild(el("div", "meta", run.startUtc + "  ·  " + fmtMs(run.durationMs) + "  ·  " + run.runId));
        card.appendChild(grow);
        const counts = el("div", "counts");
        const c = run.counts || {};
        const parts = [["passed", c.passed], ["failed", c.failed], ["error", c.error], ["timedOut", c.timedOut]];
        for (const [label, value] of parts) {
          if (value) {
            const chip = el("span");
            chip.appendChild(el("b", null, value));
            chip.appendChild(document.createTextNode(" " + label));
            counts.appendChild(chip);
          }
        }
        card.appendChild(counts);
        card.addEventListener("click", function () { setRoute(run.runId, null); });
        list.appendChild(card);
      }
      if (shown === 0) list.appendChild(el("div", "empty", "No runs match."));
    }

    search.addEventListener("input", draw);
    draw();
  }

  // --- run loading (script injection works over file:// where fetch does not) ---
  const runCache = Object.create(null);

  function loadRun(runId, done, fail) {
    if (runCache[runId]) { done(runCache[runId]); return; }
    const script = document.createElement("script");
    script.src = "runs/" + encodeURIComponent(runId) + "/result.js";
    script.onload = function () {
      const data = window.__JTEST_RUN__;
      delete window.__JTEST_RUN__;
      script.remove();
      if (data && data.runId === runId) {
        runCache[runId] = data;
        done(data);
      } else {
        fail();
      }
    };
    script.onerror = function () { script.remove(); fail(); };
    document.body.appendChild(script);
  }

  // --- trace helpers ---
  function findByPath(node, path) {
    if (!path || node.path === path) return node;
    for (const child of node.children || []) {
      if (path === child.path || path.startsWith(child.path + "/")) {
        const hit = findByPath(child, path);
        if (hit) return hit;
      }
    }
    return node.path === "" ? null : null;
  }

  function subtreeMatches(node, needle) {
    const own = ((node.name || "") + " " + (node.stepType || "") + " " + (node.template || "") + " " +
      (node.dataset || "") + " " + node.path + " " + node.outcome).toLowerCase();
    if (own.includes(needle)) return true;
    return (node.children || []).some(function (child) { return subtreeMatches(child, needle); });
  }

  // --- run view ---
  function renderRun(data, focusPath) {
    clear(app);
    const state = { failFirst: true, needle: "" };
    const focus = focusPath ? findByPath(data.trace, focusPath) || data.trace : data.trace;

    const back = el("div", "back");
    if (window.__JTEST_CATALOG__) {
      const link = el("button", "ghost", "← All runs");
      link.addEventListener("click", function () { setRoute(null, null); });
      back.appendChild(link);
    }
    app.appendChild(back);

    const header = el("div", "card run-header");
    header.appendChild(badge(data.outcome));
    header.appendChild(el("h2", null, "Run " + data.runId));
    const c = data.counts.caseRuns;
    header.appendChild(el("div", "meta",
      data.startUtc + "  ·  " + fmtMs(data.durationMs) +
      "  ·  cases: " + c.total + " (" + c.passed + " passed, " + c.failed + " failed, " +
      c.error + " error, " + c.skipped + " skipped, " + c.cancelled + " cancelled, " + c.timedOut + " timed out)" +
      "  ·  assertions: " + data.counts.assertions.total + " (" + data.counts.assertions.failed + " failed)" +
      "  ·  jtest " + data.toolVersion));
    app.appendChild(header);

    if (focus !== data.trace) {
      const crumbs = el("div", "crumbs");
      const segments = focus.path.split("/");
      const rootLink = el("a", null, "run");
      rootLink.addEventListener("click", function () { setRoute(data.runId, null); });
      crumbs.appendChild(rootLink);
      for (let i = 2; i <= segments.length; i += 2) {
        const partial = segments.slice(0, i).join("/");
        crumbs.appendChild(el("span", "sep", "›"));
        const link = el("a", null, segments[i - 2] + "/" + segments[i - 1]);
        link.addEventListener("click", function () { setRoute(data.runId, partial); });
        crumbs.appendChild(link);
      }
      app.appendChild(crumbs);
    }

    const toolbar = el("div", "toolbar");
    const search = el("input");
    search.type = "search";
    search.placeholder = "Filter steps, templates, outcomes…";
    toolbar.appendChild(search);
    const failFirst = el("button", "ghost", "Failures first");
    failFirst.setAttribute("aria-pressed", "true");
    const expandAll = el("button", "ghost", "Expand all");
    const collapseAll = el("button", "ghost", "Collapse all");
    toolbar.appendChild(failFirst);
    toolbar.appendChild(expandAll);
    toolbar.appendChild(collapseAll);
    app.appendChild(toolbar);

    const tree = el("div");
    app.appendChild(tree);

    function orderedChildren(node) {
      const children = (node.children || []).slice();
      if (state.failFirst) {
        const rank = { error: 0, timedOut: 1, cancelled: 2, failed: 3, skipped: 5, passed: 4 };
        children.sort(function (a, b) {
          const byOutcome = rank[a.outcome] - rank[b.outcome];
          return byOutcome !== 0 ? byOutcome : a.ordinal - b.ordinal;
        });
      }
      return children;
    }

    function renderNode(node, depth) {
      const wrap = el("div", "node " + node.outcome);
      const row = el("div", "node-row");
      const hasBody = (node.children || []).length > 0 || node.evidence || (node.diagnostics || []).length > 0;
      const open = depth < 2 || node.outcome !== "passed";
      if (open) row.classList.add("open");

      row.appendChild(el("span", "twist", hasBody ? "▶" : ""));
      row.appendChild(el("span", "kind", node.stepType || kindLabel(node.kind)));
      row.appendChild(el("span", "label", labelFor(node)));
      row.appendChild(badge(node.outcome));
      row.appendChild(el("span", "dur", fmtMs(node.durationMs)));
      const canDive = node.kind === "step" || node.kind === "templateInvocation" ||
        node.kind === "iteration" || node.kind === "case" || node.kind === "datasetRun" || node.kind === "suite";
      if (canDive && node.path) {
        const dive = el("span", "dive", "step into ▸");
        dive.addEventListener("click", function (event) {
          event.stopPropagation();
          setRoute(data.runId, node.path);
        });
        row.appendChild(dive);
      }
      wrap.appendChild(row);

      const body = el("div", "node-body" + (open ? "" : " collapsed"));
      if (hasBody) {
        row.addEventListener("click", function () {
          body.classList.toggle("collapsed");
          row.classList.toggle("open");
        });
      }

      appendDiagnostics(body, node);
      appendEvidence(body, node);
      for (const child of orderedChildren(node)) {
        if (child.kind === "assertion") continue; // rendered as a table below
        body.appendChild(renderNode(child, depth + 1));
      }
      appendAssertions(body, node);

      wrap.appendChild(body);
      return wrap;
    }

    function draw() {
      clear(tree);
      const needle = state.needle.toLowerCase();
      const roots = focus === data.trace ? orderedChildren(data.trace) : [focus];
      let shown = 0;
      for (const node of roots) {
        if (needle && !subtreeMatches(node, needle)) continue;
        shown++;
        tree.appendChild(renderNode(node, 0));
      }
      if (shown === 0) tree.appendChild(el("div", "empty", "Nothing matches."));
    }

    search.addEventListener("input", function () { state.needle = search.value; draw(); });
    failFirst.addEventListener("click", function () {
      state.failFirst = !state.failFirst;
      failFirst.setAttribute("aria-pressed", String(state.failFirst));
      draw();
    });
    expandAll.addEventListener("click", function () { setAll(false); });
    collapseAll.addEventListener("click", function () { setAll(true); });

    function setAll(collapsed) {
      for (const body of tree.querySelectorAll(".node-body")) body.classList.toggle("collapsed", collapsed);
      for (const row of tree.querySelectorAll(".node-row")) row.classList.toggle("open", !collapsed);
    }

    draw();
  }

  function kindLabel(kind) {
    if (kind === "datasetRun") return "dataset";
    if (kind === "templateInvocation") return "template";
    return kind;
  }

  function labelFor(node) {
    if (node.kind === "iteration") return "iteration " + node.iterationIndex;
    if (node.kind === "templateInvocation") return node.template || "template";
    return node.name || node.template || node.dataset || node.stepId || node.path.split("/").slice(-2).join("/");
  }

  function appendDiagnostics(body, node) {
    for (const diagnostic of node.diagnostics || []) {
      const row = el("div", "diag " + diagnostic.severity);
      row.appendChild(el("code", null, diagnostic.code));
      row.appendChild(el("span", null, diagnostic.message + (diagnostic.pointer ? "  (" + diagnostic.pointer + ")" : "")));
      body.appendChild(row);
    }
  }

  function appendEvidence(body, node) {
    const evidence = node.evidence;
    if (!evidence) return;
    if (evidence.request || evidence.response) {
      if (evidence.request) body.appendChild(exchangePanel("Request", evidence.request));
      if (evidence.response) body.appendChild(exchangePanel("Response", evidence.response));
      return;
    }
    if (node.kind === "assertion") return;
    const panel = el("div", "detail");
    panel.appendChild(el("h4", null, "Evidence"));
    panel.appendChild(jsonBlock(evidence));
    body.appendChild(panel);
  }

  function exchangePanel(title, exchange) {
    const panel = el("div", "detail");
    panel.appendChild(el("h4", null, title));
    const kv = el("div", "kv");
    for (const key of ["method", "url", "status", "timedOutAfterMs"]) {
      if (exchange[key] !== undefined && exchange[key] !== null) {
        kv.appendChild(el("span", "k", key));
        kv.appendChild(el("span", "v", exchange[key]));
      }
    }
    panel.appendChild(kv);
    if (exchange.headers && Object.keys(exchange.headers).length) {
      const headers = el("div", "kv");
      for (const name of Object.keys(exchange.headers)) {
        headers.appendChild(el("span", "k", name));
        headers.appendChild(el("span", "v", exchange.headers[name]));
      }
      const head = el("div", "detail");
      head.appendChild(el("h4", null, title + " headers"));
      head.appendChild(headers);
      panel.appendChild(head);
    }
    if (exchange.body !== undefined && exchange.body !== null) panel.appendChild(jsonBlock(exchange.body));
    if (exchange.raw) panel.appendChild(jsonBlock(exchange.raw));
    return panel;
  }

  function jsonBlock(value) {
    const pre = el("pre", "json");
    pre.textContent = typeof value === "string" ? value : JSON.stringify(value, null, 2);
    return pre;
  }

  function appendAssertions(body, node) {
    const assertions = (node.children || []).filter(function (child) { return child.kind === "assertion"; });
    if (!assertions.length) return;
    const panel = el("div", "detail");
    panel.appendChild(el("h4", null, "Assertions"));
    const table = el("table", "asserts");
    const head = el("tr");
    for (const title of ["", "Operator", "Actual", "Expected", "Detail"]) head.appendChild(el("th", null, title));
    table.appendChild(head);
    for (const assertion of assertions) {
      const evidence = assertion.evidence || {};
      const row = el("tr");
      const cell = el("td");
      cell.appendChild(badge(assertion.outcome));
      row.appendChild(cell);
      row.appendChild(el("td", "mono", evidence.op));
      row.appendChild(el("td", "mono", short(evidence.actual)));
      row.appendChild(el("td", "mono", short(evidence.expected)));
      row.appendChild(el("td", null, assertion.outcome === "passed" ? (assertion.name || "") : (evidence.message || "")));
      table.appendChild(row);
    }
    panel.appendChild(table);
    body.appendChild(panel);
  }

  function short(value) {
    if (value === undefined) return "";
    const text = typeof value === "string" ? value : JSON.stringify(value);
    return text && text.length > 200 ? text.slice(0, 200) + "…" : text;
  }

  // --- boot ---
  function boot() {
    const route = currentRoute();
    if (window.__JTEST_RUN__ && !window.__JTEST_CATALOG__) {
      renderRun(window.__JTEST_RUN__, route.node);
      return;
    }
    const catalog = window.__JTEST_CATALOG__;
    if (!catalog) {
      clear(app);
      app.appendChild(el("div", "empty",
        "No catalog found. Run jtest to produce reports, then refresh this page."));
      return;
    }
    if (route.run) {
      loadRun(route.run, function (data) { renderRun(data, route.node); }, function () {
        clear(app);
        app.appendChild(el("div", "empty", "Run " + route.run + " could not be loaded."));
      });
    } else {
      renderCatalog(catalog);
    }
  }

  window.addEventListener("hashchange", boot);
  boot();
})();
