"""Check that no generated handle (content Handles: prefix + suffix) equals a real player's handle.

Real handles are read from the local raw snapshots (tournament rosters, team and player listings) and are only compared
in memory; colliding combinations are printed to this console so the handle parts can be changed. Nothing is written.
Exit code 1 when any combination collides.
"""
import argparse
import json
from pathlib import Path
import sys

from snapshots import SNAPSHOT_ROOT, completed_runs, load_records

FIXTURE = Path(__file__).resolve().parents[2] / "content" / "fixture.json"


def real_handles(runs):
    names = set()
    for t in load_records(runs, "tournaments"):
        for entry in t.get("expected_roster") or []:
            names.update((p.get("name") or "").casefold() for p in entry.get("players") or [])
    for team in load_records(runs, "teams"):
        names.update((p.get("name") or "").casefold() for p in team.get("players") or [])
    names.update((p.get("name") or "").casefold() for p in load_records(runs, "players"))
    names.discard("")
    return names


def collisions(prefixes, suffixes, real):
    return sorted(p + s for p in prefixes for s in suffixes if (p + s).casefold() in real)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--fixture", type=Path, default=FIXTURE)
    parser.add_argument("--snapshot-root", type=Path, default=SNAPSHOT_ROOT)
    args = parser.parse_args()
    handles = json.loads(args.fixture.read_text(encoding="utf-8"))["Handles"]
    real = real_handles(completed_runs(args.snapshot_root))
    found = collisions(handles["Prefixes"], handles["Suffixes"], real)
    print(f"{len(handles['Prefixes']) * len(handles['Suffixes'])} generated handles checked against {len(real)} real handles: {len(found)} collisions")
    for name in found:
        print("  COLLISION", name)
    sys.exit(1 if found else 0)
