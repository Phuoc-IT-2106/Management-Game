#!/usr/bin/env python3
"""Upload design images to the established Google Drive library with rclone.

Requires: Python 3.10+, rclone remote configured as gdrive. Never deletes Git files.
Images in artifacts/design-inbox/<Drive collection>/<subpath>/ are routed by folder
name. 'migrate' copies tracked GitHub documentation images without removing them.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

REPO = Path(__file__).resolve().parents[2]
MAPPING = REPO / "docs/design/drive-library-map.json"
INBOX = REPO / "artifacts/design-inbox"
STATE_PATH = REPO / "artifacts/design-library-sync/state.json"
IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg",
                    ".bmp", ".tif", ".tiff", ".avif", ".psd", ".kra", ".aseprite"}
ROOTS = {
    "00_Art_Direction": "art_direction",
    "01_UI_References": "references",
    "02_UI_Mockups": "mockups",
    "03_UI_Assets": "ui_assets",
    "04_Game_Art": "game_art",
    "05_Approved_Designs": "approved",
    "06_Production_Exports": "exports",
    "07_Archive": "archive",
}


def digest(path: Path, algo: str) -> str:
    value = hashlib.new(algo)
    with path.open("rb") as handle:
        for data in iter(lambda: handle.read(1024 * 1024), b""):
            value.update(data)
    return value.hexdigest()


def load_json(path: Path, default: dict) -> dict:
    return json.loads(path.read_text(encoding="utf-8")) if path.exists() else default


def write_json(path: Path, data: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def execute(args: list[str]) -> str:
    result = subprocess.run(args, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode:
        raise RuntimeError(f"rclone command failed ({result.returncode}): {result.stderr[-1600:]}")
    return result.stdout


def check_remote(remote: str) -> None:
    if not shutil.which("rclone"):
        raise RuntimeError("rclone executable not found. Install and configure rclone first.")
    entries = execute(["rclone", "listremotes"]).splitlines()
    if remote + ":" not in entries:
        raise RuntimeError(f"Missing rclone remote {remote!r}; run: rclone config")


def safe_parts(value: str) -> list[str]:
    parts = value.replace("\\", "/").split("/")
    if not parts or any(p in ("", ".", "..") or ":" in p for p in parts):
        raise ValueError(f"Unsafe relative path: {value}")
    return parts


def destination(file: Path, mode: str, mapping: dict, runtime_backup: bool = False) -> tuple[str, str, str]:
    folders = mapping["folders"]
    if mode == "inbox":
        relative = file.relative_to(INBOX).as_posix()
        segments = safe_parts(relative)
        if len(segments) < 2 or segments[0] not in ROOTS:
            raise ValueError("Inbox file must be in <collection>/<filename> (optional subfolders). "
                             + "Allowed collections: " + ", ".join(ROOTS))
        folder_key = ROOTS[segments[0]]
        remote_path = "/".join(segments[1:])
    else:
        relative = file.relative_to(REPO).as_posix()
        segments = safe_parts(relative)
        if segments[:3] == ["game", "Client", "Assets"]:
            if not runtime_backup:
                raise ValueError("Runtime asset stays in Git and is excluded from migration")
            folder_key = "github_runtime_asset_backup"
        elif "ui-research" in segments and "mockups" in segments:
            folder_key = "github_ui_mockups_import"
        elif segments[0] == "docs":
            folder_key = "github_documentation_evidence"
        else:
            raise ValueError("Not classified for Git-to-Drive migration: " + relative)
        remote_path = relative
    if folder_key not in folders:
        raise ValueError(f"Unknown Drive folder key: {folder_key}")
    return folder_key, folders[folder_key], remote_path


def remote_md5(remote: str, folder_id: str, path: str) -> str | None:
    result = subprocess.run(
        ["rclone", "md5sum", f"{remote}:{path}", "--drive-root-folder-id", folder_id],
        capture_output=True, text=True, encoding="utf-8", errors="replace",
    )
    if result.returncode != 0:
        return None
    for line in result.stdout.splitlines():
        candidate = line.strip().split(None, 1)
        if len(candidate) >= 1 and len(candidate[0]) == 32:
            try:
                int(candidate[0], 16)
                return candidate[0].lower()
            except ValueError:
                pass
    return None


def process(file: Path, mode: str, mapping: dict, state: dict, remote: str,
            apply: bool, remove_verified: bool, runtime_backup: bool) -> str:
    if not file.is_file() or file.suffix.lower() not in IMAGE_EXTENSIONS:
        return "SKIP_NON_IMAGE"
    folder_key, folder_id, remote_path = destination(file, mode, mapping, runtime_backup)
    length = file.stat().st_size
    if length <= 0:
        return "SKIP_EMPTY"
    sha256 = digest(file, "sha256")
    md5 = digest(file, "md5")
    key = f"{mode}:{file.relative_to(REPO).as_posix()}:{sha256}"
    prior = state["records"].get(key)
    if not apply:
        print(json.dumps({"action": "PLAN", "source": str(file.relative_to(REPO)),
                          "drive_folder": folder_key, "destination": remote_path,
                          "bytes": length, "sha256": sha256}))
        return "PLAN"
    if not prior or prior.get("status") != "VERIFIED":
        # Never overwrite an existing destination whose content differs.
        existing = remote_md5(remote, folder_id, remote_path)
        if existing and existing != md5:
            raise RuntimeError(f"Remote name collision: {folder_key}/{remote_path}; not overwriting.")
        if existing != md5:
            execute(["rclone", "copyto", str(file), f"{remote}:{remote_path}",
                     "--drive-root-folder-id", folder_id, "--retries", "3"])
    observed = remote_md5(remote, folder_id, remote_path)
    if observed != md5:
        raise RuntimeError(f"Checksum verification failed: {folder_key}/{remote_path}")
    state["records"][key] = {"status": "VERIFIED", "source": file.relative_to(REPO).as_posix(),
                             "folder_key": folder_key, "folder_id": folder_id,
                             "remote_path": remote_path, "sha256": sha256, "md5": md5,
                             "bytes": length, "verified_at_utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())}
    write_json(STATE_PATH, state)
    if mode == "inbox" and remove_verified:
        # Delete only after remote hash agrees AND local content is still unchanged.
        if file.exists() and file.stat().st_size == length and digest(file, "sha256") == sha256:
            file.unlink()
    print(json.dumps({"action": "VERIFIED", "source": str(file.relative_to(REPO)),
                      "destination": f"{folder_key}/{remote_path}", "sha256": sha256}))
    return "VERIFIED"


def tracked_images(runtime_backup: bool) -> list[Path]:
    tracked = subprocess.run(["git", "-C", str(REPO), "ls-files", "-z"],
                             capture_output=True, check=True).stdout.decode("utf-8").split("\0")
    result = []
    for relative in tracked:
        if not relative:
            continue
        file = REPO / relative
        if file.suffix.lower() not in IMAGE_EXTENSIONS or not file.is_file():
            continue
        if relative.startswith("docs/") or (runtime_backup and relative.startswith("game/Client/Assets/")):
            result.append(file)
    return sorted(result)


def process_queue(files: list[Path], mode: str, config: dict, state: dict, args: argparse.Namespace) -> dict:
    totals: dict[str, int] = {}
    for file in files:
        try:
            status = process(file, mode, config, state, args.remote, args.apply,
                             args.remove_verified, args.include_runtime)
        except (OSError, ValueError, RuntimeError) as exc:
            status = "FAILED"
            print(f"ERROR {file}: {exc}", file=sys.stderr)
        totals[status] = totals.get(status, 0) + 1
    print(json.dumps({"summary": totals, "mode": mode, "applied": args.apply}))
    return totals


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("doctor", "once", "watch", "migrate"))
    parser.add_argument("--apply", action="store_true", help="Perform uploads (otherwise dry-run)")
    parser.add_argument("--remote", default="gdrive", help="Existing rclone remote name")
    parser.add_argument("--remove-verified", action="store_true",
                        help="Remove only VERIFIED inbox files; never removes Git source files")
    parser.add_argument("--include-runtime", action="store_true",
                        help="Optional Drive backup of game runtime assets, never remove them from Git")
    parser.add_argument("--interval", type=float, default=6, help="Watch polling interval, in seconds")
    args = parser.parse_args()
    config = load_json(MAPPING, {})
    if not config.get("folders"):
        raise RuntimeError(f"Missing mapping at {MAPPING}")
    if args.command == "doctor":
        check_remote(args.remote)
        print("OK: rclone remote configured. Drive write permission is NOT yet verified.")
        return 0
    if args.apply:
        check_remote(args.remote)
    state = load_json(STATE_PATH, {"schema_version": 1, "records": {}})
    if args.command == "migrate":
        totals = process_queue(tracked_images(args.include_runtime), "git", config, state, args)
        return 1 if totals.get("FAILED") else 0
    if args.command == "once":
        files = sorted(f for f in INBOX.rglob("*") if f.is_file()) if INBOX.is_dir() else []
        totals = process_queue(files, "inbox", config, state, args)
        return 1 if totals.get("FAILED") else 0
    if args.command == "watch":
        if not args.apply:
            print("Watch is a perpetual dry-run without --apply.", file=sys.stderr)
        print(f"Monitoring {INBOX} every {args.interval} seconds. Ctrl+C to stop.")
        previous: dict[str, tuple[int, int]] = {}
        while True:
            current: dict[str, tuple[int, int]] = {}
            for file in INBOX.rglob("*") if INBOX.is_dir() else []:
                if not file.is_file() or file.suffix.lower() not in IMAGE_EXTENSIONS:
                    continue
                stat = file.stat()
                key = str(file)
                current[key] = (stat.st_size, stat.st_mtime_ns)
                if previous.get(key) == current[key]:
                    try:
                        process(file, "inbox", config, state, args.remote,
                                args.apply, args.remove_verified, args.include_runtime)
                    except (OSError, ValueError, RuntimeError) as exc:
                        print(f"ERROR {file}: {exc}", file=sys.stderr)
            previous = current
            time.sleep(max(2, args.interval))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        print("Stopped.")
        sys.exit(0)
    except (OSError, ValueError, RuntimeError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        sys.exit(2)
