"""Write the calibrated generation tables into content/fixture.json and refresh content/fixture.sha256.

Reads reports/calibration_2026.json (aggregate statistics only) and replaces, in place:
  Generation.Ages      the age distribution of players on 2026 tournament rosters (ages 17..31; outer ages folded in);
  Nationalities[].Weight  the nationality mix of the league's home region (ASIA) for the nationalities the fixture has
                       name pools for; nationalities the region does not list share its "other" part equally;
  Calibration          the report's source, SHA-256 and observed window.
Season format, league spread and roster churn are read by the rules that need them (see RULES_V4_PLAYER_WORLD.md).
Everything else in the fixture is left byte for byte as it was.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
REPORT = Path(__file__).resolve().parent / "reports" / "calibration_2026.json"
FIXTURE = ROOT / "content" / "fixture.json"
HOME_REGION = "ASIA"
AGES = range(17, 32)


def basis_points(weights):
    """Integer basis points summing to 10,000 by largest remainder (ties by key order), each at least 1."""
    total = sum(weights.values())
    raw = {k: v * 10_000 / total for k, v in weights.items()}
    points = {k: max(1, int(v)) for k, v in raw.items()}
    for k in sorted(raw, key=lambda k: (-(raw[k] - int(raw[k])), str(k)))[: 10_000 - sum(points.values())]:
        points[k] += 1
    return points


def age_weights(report):
    histogram = {int(k): v for k, v in report["demographics"]["age"]["histogram_all_roles"].items()}
    folded = {a: 0 for a in AGES}
    for age, count in histogram.items():
        folded[min(max(age, AGES.start), AGES.stop - 1)] += count
    return basis_points(folded)


def nationality_weights(report, codes):
    shares = report["demographics"]["nationality_share_by_region"][HOME_REGION]
    other = shares.get("other", 0) + shares.get("unknown", 0)
    unlisted = [c for c in codes if c not in shares]
    weights = {c: shares[c] if c in shares else other / len(unlisted) for c in codes}
    return basis_points(weights)


def replace_line(text, key, value):
    """Replace one compact one-line JSON member `"key": value` (as the fixture is laid out)."""
    pattern = re.compile(rf'^(\s*"{re.escape(key)}": ).*?(,?)$', re.M)
    new, count = pattern.subn(lambda m: m.group(1) + json.dumps(value, ensure_ascii=False, separators=(",", ":")) + m.group(2), text, count=1)
    if count != 1:
        raise SystemExit(f"fixture has no one-line member {key}")
    return new


def apply(report_path: Path, fixture_path: Path):
    raw = report_path.read_bytes()
    report = json.loads(raw)
    text = fixture_path.read_text(encoding="utf-8")
    fixture = json.loads(text)
    ages = age_weights(report)
    text = replace_line(text, "Ages", [{"Value": a, "Weight": w} for a, w in sorted(ages.items())])
    codes = [n["Id"] for n in fixture["Nationalities"]]
    weights = nationality_weights(report, codes)
    for n in fixture["Nationalities"]:
        line = json.dumps(n, ensure_ascii=False, separators=(",", ":"))
        updated = json.dumps(dict(n, Weight=weights[n["Id"]]), ensure_ascii=False, separators=(",", ":"))
        if line not in text:
            raise SystemExit(f"nationality {n['Id']} is not laid out on one line")
        text = text.replace(line, updated)
    window = report["observed_window"]
    text = replace_line(text, "Calibration", {"Source": f"PandaScore 2026 aggregates, {report_path.name} (format {report['format']} v{report['version']})",
                                              "ReportSha256": hashlib.sha256(raw).hexdigest(), "ObservedFrom": window["from"], "ObservedTo": window["to"]})
    fixture_path.write_text(text, encoding="utf-8", newline="\n")
    fixture_path.with_suffix(".sha256").write_text(hashlib.sha256(fixture_path.read_bytes()).hexdigest() + "\n", encoding="utf-8", newline="\n")
    return ages, weights


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--report", type=Path, default=REPORT)
    parser.add_argument("--fixture", type=Path, default=FIXTURE)
    args = parser.parse_args()
    ages, weights = apply(args.report, args.fixture)
    print("Ages (basis points):", ages)
    print("Nationalities (basis points):", weights)
    print("Updated", args.fixture, "and its .sha256")
