#!/usr/bin/env python3
"""Check that `jtest report` merged two runs honestly.

Run after two `jtest run` invocations and a `jtest report` merge:

    python3 scripts/check-merged-trace.py artifacts

It asserts the properties that make a merged report trustworthy, each of which
is a way the merge could look right and be wrong:

  - counts are the SUM of the inputs, not one invocation's
  - every suite from both runs survives, under a distinct, input-namespaced id
  - durationMs is the summed test time and the merge block says so
  - per-invocation $.run / environment are not presented as the whole run's
  - the rendered reports are self-contained (no external requests)

Exits non-zero and prints a GitHub Actions error per problem.
"""
import json
import sys
from pathlib import Path


def main(directory):
    artifacts = Path(directory)
    one = json.loads((artifacts / "trace.json").read_text(encoding="utf-8"))
    two = json.loads((artifacts / "trace-2.json").read_text(encoding="utf-8"))
    merged = json.loads((artifacts / "merged-trace.json").read_text(encoding="utf-8"))

    problems = []

    def check(ok, message):
        if not ok:
            problems.append(message)

    # Counts must be the sum. Taking one invocation's would still look plausible.
    for key in ("total", "passed", "failed", "errored", "cancelled", "timedOut", "skipped"):
        expected = one["counts"][key] + two["counts"][key]
        actual = merged["counts"][key]
        check(actual == expected, "counts.{}: expected {}, got {}".format(key, expected, actual))

    # Every suite survives, under an id that is distinct across runs. Suite ids are positional per
    # run, so both inputs carry suite[0]: this is the case that must not collapse or collide.
    ids = [suite["id"] for suite in merged["suites"]]
    expected_suites = len(one["suites"]) + len(two["suites"])
    check(len(ids) == expected_suites,
          "expected {} suites, got {}".format(expected_suites, len(ids)))
    check(len(set(ids)) == len(ids), "duplicate suite ids: {}".format(ids))
    check(all(i.startswith("input[") for i in ids), "suite ids not namespaced by input: {}".format(ids))

    # durationMs is testing time, not wall clock, and the trace records which.
    block = merged.get("merge")
    check(block is not None, "merged trace has no merge block")
    if block:
        check(block["durationSemantics"] == "sumOfSources",
              "unexpected durationSemantics: {}".format(block["durationSemantics"]))
        check(abs(block["testDurationMs"] - merged["durationMs"]) < 1e-6,
              "merge.testDurationMs does not match the root durationMs")
        check(len(block["sources"]) == 2,
              "expected 2 merge sources, got {}".format(len(block["sources"])))
        check(block["testDurationMs"] <= block["elapsedMs"] + 1e-6,
              "test time exceeds the wall clock, so durationMs is not a sum of the runs")
        summed = one.get("durationMs", 0) + two.get("durationMs", 0)
        check(abs(block["testDurationMs"] - summed) < 1e-6,
              "merge.testDurationMs {} is not the sum of the inputs {}".format(
                  block["testDurationMs"], summed))

    # No invocation's values may stand for the whole run.
    check("run" not in merged, "merged root kept a per-invocation $.run")
    check("environment" not in merged, "merged root kept a per-invocation environment")

    # A report must open with zero network requests, merged or re-rendered.
    for name in ("merged.html", "rerendered.html"):
        html = (artifacts / name).read_text(encoding="utf-8")
        check("<script src=" not in html, "{} loads an external script".format(name))
        check("<link rel=" not in html, "{} loads an external stylesheet".format(name))

    if problems:
        for problem in problems:
            print("::error::{}".format(problem))
        return 1

    print("merged {} + {} suites, {} results, outcome {}".format(
        len(one["suites"]), len(two["suites"]), merged["counts"]["total"], merged["outcome"]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else "artifacts"))
