"""Recompute qualification evidence statistics; not a game/runtime test suite."""
import hashlib
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parent
EVIDENCE = ROOT.parent / "docs/09-technical-foundation/qualification/evidence"


def read(name):
    return json.loads((EVIDENCE / name).read_text(encoding="utf-8-sig"))


def p95(values):
    return sorted(values)[max(0, math.ceil(len(values) * .95) - 1)]


def same(actual, expected):
    if not math.isclose(actual, expected, rel_tol=1e-10, abs_tol=1e-10):
        raise ValueError(f"Statistic mismatch: {actual} != {expected}")


profiles = {}
for name in ("baseline", "reuse", "reuse-light", "rebuild-control", "reuse-no-vsync"):
    data = read(f"followup-profile-{name}.json")
    assert data["debugBuild"] is False and len(data["results"]) == 24
    rows = []
    for stage in data["results"]:
        assert stage["seconds"] >= 4
        for key, distribution in stage.items():
            if not isinstance(distribution, dict) or "samples" not in distribution:
                continue
            samples = distribution["samples"]
            assert len(samples) == distribution["count"]
            assert all(math.isfinite(value) and value >= 0 for value in samples)
            if samples:
                same(distribution["p95"], p95(samples))
                same(distribution["p50"], sorted(samples)[len(samples)//2])
                same(distribution["max"], max(samples))
            else:
                assert distribution["p95"] is None
        rows.append({"rows": stage["rows"], "workflow": stage["workflow"],
                     "frameSamples": stage["deltaMs"]["count"],
                     **{key + "P95": stage[key]["p95"] for key in (
                         "deltaMs", "wallMs", "bindMs", "bindBytes", "workerQueryMs",
                         "renderCpuMs", "renderGpuMs", "responseProxyMs")},
                     "allocationBytes": stage["allocationBytes"], "gcCounts": stage["gcDelta"]})
    assert len({(r["rows"], r["workflow"]) for r in rows}) == 24
    profiles[name] = {"vsync": data["vsync"], "light": data["lightInstrumentation"], "results": rows}

data = read("followup-reuse-sustained.json")
assert not data["debugBuild"] and len(data["checks"]) == 51 and len(set(data["checks"])) == 51
assert len(data["results"]) == 3
sustained = []
for row in data["results"]:
    for raw, summary in (("frameMs", "frameP95Ms"), ("queryMs", "queryP95Ms"), ("inputAckMs", "inputP95Ms")):
        same(p95(row[raw]), row[summary])
    assert row["interactionSeconds"] >= 100 and len(row["navigation"]) == 10
    same(row["retainedRatio"], row["managedAfter"] / row["managedBefore"])
    sustained.append({"rows": row["rows"], "frameP95Ms": row["frameP95Ms"],
                      "frameMaxMs": max(row["frameMs"]), "frameSamples": len(row["frameMs"]),
                      "responseP95Ms": row["inputP95Ms"], "queryP95Ms": row["queryP95Ms"],
                      "retainedRatio": row["retainedRatio"], "privateBytes": row["privateBytes"],
                      "controlsBefore": row["controlsBefore"], "controlsAfter": row["controlsAfter"],
                      "frameBudgetPass": row["frameP95Ms"] <= 16.7,
                      "responseBudgetPass": row["inputP95Ms"] <= 100,
                      "queryBudgetMs": {10000: 250, 100000: 1000}.get(row["rows"]),
                      "queryBudgetPass": None if row["rows"] == 1000 else row["queryP95Ms"] <= (1000 if row["rows"] == 100000 else 250),
                      "memoryRetentionPass": row["retainedRatio"] < 1.1,
                      "boundedControlsPass": row["controlsAfter"] == row["controlsBefore"] == 11})

reference = read("qg03/host-Release-default.json")
actual = read("followup-reuse-regression.json")
assert len(actual["Hashes"]) == 32
for key in ("InitialHash", "Hashes", "FinalHash"):
    assert actual[key] == reference[key]
for name, marker in (("reuse-layout", "QG02_LAYOUT_PASS 84"),
                     ("reuse-smoke", "QG01_INTERACTION_PASS"),
                     ("reuse-regression", "REGRESSION_PASS")):
    log = (EVIDENCE / f"followup-{name}.log").read_text(encoding="utf-8-sig")
    assert marker in log and "EXIT: 0" in log

manifest = read("followup-reuse-manifest.json")
artifact = ROOT / "artifacts/followup"
assert {p.relative_to(artifact).as_posix() for p in artifact.rglob("*") if p.is_file()} == {
    entry["Path"].replace("\\", "/") for entry in manifest}
for entry in manifest:
    path = artifact / entry["Path"]
    assert path.stat().st_size == entry["Bytes"]
    assert hashlib.sha256(path.read_bytes()).hexdigest().upper() == entry["SHA256"]
for entry in read("followup-reuse-source.json"):
    path = ROOT / "Host" / entry["Path"]
    assert hashlib.sha256(path.read_bytes()).hexdigest().upper() == entry["SHA256"]

report = {"statisticsRecomputed": True, "correctnessChecks": 51, "layoutChecks": 84,
          "qg03TransitionsMatched": 32, "qg04Rerun": False,
          "artifactFilesVerified": len(manifest), "sourceHashesVerified": True,
          "currentMachinePerformancePass": all(all(r[key] is not False for key in (
              "frameBudgetPass", "responseBudgetPass", "queryBudgetPass", "memoryRetentionPass", "boundedControlsPass")) for r in sustained),
          "approvedTargetHardware": "INCONCLUSIVE: not available",
          "sustained": sustained, "profiles": profiles}
(EVIDENCE / "followup-audit.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
print(json.dumps({key: value for key, value in report.items() if key != "profiles"}, indent=2))
