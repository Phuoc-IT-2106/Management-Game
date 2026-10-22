#!/usr/bin/env python3
"""Reject NEW committed design images outside runtime art folders.

Existing tracked QA evidence is grandfathered; future screenshots must be
uploaded to Google Drive via tools/design-library/asset_sync.py. This is an
additional CI guard, not a replacement for correct .gitignore rules.
"""
import argparse
import subprocess
import sys
from pathlib import PurePosixPath

EXTS = {".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".bmp",
        ".tif", ".tiff", ".avif", ".psd", ".kra", ".aseprite"}

def blocked(paths):
    return [p for p in paths if PurePosixPath(p).suffix.lower() in EXTS
            and not p.startswith(("game/Client/Assets/", "game/Client/addons/"))]

def run_git(args):
    result = subprocess.run(["git", *args], check=True, capture_output=True)
    return [p for p in result.stdout.decode("utf-8").split("\0") if p]

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", help="Compare Git history to a base ref in CI. Default checks staged additions.")
    args = ap.parse_args()
    if args.base:
        paths = run_git(["diff", "--name-only", "-z", "--diff-filter=A",
                         f"{args.base}...HEAD"])
    else:
        paths = run_git(["diff", "--cached", "--name-only", "-z", "--diff-filter=A"])
    violations = blocked(paths)
    if violations:
        print("Design images belong in Google Drive, not new Git blobs:", file=sys.stderr)
        for path in violations:
            print("  " + path, file=sys.stderr)
        print("Use artifacts/design-inbox/<collection>/... and asset_sync.py.", file=sys.stderr)
        return 1
    print(f"Design-asset guard passed ({len(paths)} added paths checked).")
    return 0

if __name__ == "__main__":
    sys.exit(main())
