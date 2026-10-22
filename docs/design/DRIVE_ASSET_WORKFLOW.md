# Google Drive design asset library — repository integration

**Status: STAGED / NOT YET ACTIVATED on the Windows workstation.**

Drive root: https://drive.google.com/drive/folders/13HH9hS6Is6kcOGQiS_3al0dFc9UfIN9V

Repository: https://github.com/Phuoc-IT-2106/Management-Game

The canonical folder IDs are in [drive-library-map.json](drive-library-map.json). Never infer an ID from the display name.

## Scope and ownership

- Google Drive stores mockups, concept iterations, screenshots, source artwork and non-runtime assets.
- GitHub stores code, design metadata, validation reports and **runtime assets required by Godot**.
- The local ignored inbox is only a temporary upload queue.
- Google Drive is NOT accessed during gameplay or during a production build.
- Images of real players and third-party logos need their own rights/provenance records. Upload is not permission to distribute.
- Existing QA screenshots referenced by documentation must not be removed from Git until every reference is preserved or migrated to stable links.

## Existing source inventory

[legacy-image-inventory.json](legacy-image-inventory.json) enumerates **293 tracked image blobs / 70,298,912 bytes** from `stage5-sponsor-instruments` HEAD `587b910...` (as of 2026-10-22).

| Class | Count | Drive destination | Git policy |
| --- | ---: | --- | --- |
| Documentation / QA evidence | 255 | `07_Archive/GitHub_Documentation_Evidence` | Keep in Git until dependency/link audit |
| Generated UI mockup references | 12 | `02_UI_Mockups/GitHub_UI_Mockups_Import` | Review links before untracking |
| Godot UI runtime assets | 26 | Optional backup `06_Production_Exports/GitHub_Runtime_Asset_Backup` | **Keep in Git** |

This is an inventory of tracked files, **not a completed upload manifest**. Current verified copy count is 0 until a live run writes a local verification log.

## One-time local setup (Windows / Claude Code)

1. Merge or cherry-pick this feature branch into the active developer worktree **after review**, without overwriting concurrent uncommitted changes.
2. Install [rclone](https://rclone.org/downloads/) and Python 3.10+. Set up `rclone config` with an authenticated Google Drive remote called `gdrive`. Complete OAuth in the browser on **the user's own workstation**. Never commit token/config files.
3. Verify the remote: `python tools/design-library/asset_sync.py doctor`.
4. Run a dry-run of the tracked Git migration: `python tools/design-library/asset_sync.py migrate`.
5. Run the actual **non-destructive copy**, if the classification is accepted:
   `python tools/design-library/asset_sync.py migrate --apply`
6. If a Drive backup of production Godot assets is desired, run separately:
   `python tools/design-library/asset_sync.py migrate --apply --include-runtime`
7. Check the resulting `artifacts/design-library-sync/state.json` for `VERIFIED` items and reconcile with the source inventory. Verify Drive folder contents and hashes. This state file is deliberately Git-ignored.
8. Do not run `git rm` for QA or mockup images before updating dependent Markdown/test references and obtaining explicit approval.

`migrate` is a **copy-first** operation. It does not delete any source Git file, rewrite documentation or rewrite history. Checksum verification uses local and Drive MD5; SHA-256 is also stored locally for provenance. If an existing remote destination has a different MD5, migration refuses to overwrite it.

## New image authoring path

Always save newly generated *non-runtime* artwork in:

```text
artifacts/design-inbox/
  00_Art_Direction/<study>/<image>
  01_UI_References/<topic>/<image>
  02_UI_Mockups/01_Portal/Editorial_Command/<revision>/<image>
  02_UI_Mockups/02_Roster/<revision>/<image>
  03_UI_Assets/Book_UI_Assets/<image>
  03_UI_Assets/Tablet_UI_Assets/<image>
  04_Game_Art/Illustrations/<image>
  05_Approved_Designs/<asset-id>/<image>
  06_Production_Exports/<asset-id>/<image>
  07_Archive/<topic>/<image>
```

The first folder name must match one of the above top-level Drive collection names. Nested paths are preserved under the mapped destination folder ID.

Check a queue once:

```powershell
python tools/design-library/asset_sync.py once
python tools/design-library/asset_sync.py once --apply
```

For successful verified uploads, opt in to removing the temporary local image:

```powershell
python tools/design-library/asset_sync.py once --apply --remove-verified
```

Do not assume any file placed **elsewhere** on Windows, attached in ChatGPT, or committed by another computer will be automatically uploaded. The upload automation monitors only this designated inbox.

## Automatic background upload on Windows

After the **first real upload has been verified**, enable the per-user Windows scheduled task:

```powershell
powershell -ExecutionPolicy Bypass -File tools/design-library/Install-DesignSync.ps1
Start-ScheduledTask -TaskName "ManagementGameDesignDriveSync"
```

At each user logon, the watcher scans the inbox periodically, requires a stable file size/modification time for two scans, uploads using rclone, verifies MD5 from Drive, writes a local manifest and only then removes the temporary inbox image. Errors leave files in the inbox for retry.

The scheduled task is not yet installed, authenticated or verified from this chat. Installation must be performed on the Windows machine.

## Git policy and validation

A pull-request guard at `.github/workflows/design-asset-guard.yml` rejects newly added image binaries outside `game/Client/Assets` and `game/Client/addons`. Existing tracked screenshots are grandfathered; keep them until references are dealt with. The guard does not protect against direct pushes unless branch protection requires its status check.

Verify with:

```powershell
python -m unittest discover -s tools/design-library/tests -v
python tools/design-library/git_asset_guard.py
```

Neither this documentation nor these scripts confer rights to copy, publicly show, or commercially distribute licensed esports images.

## Migration acceptance criteria

1. File count, bytes and SHA-256/MD5 source snapshots are complete and reproducible.
2. Every migrated item has a `VERIFIED` Drive copy with matching MD5.
3. No destination file is overwritten with different bytes.
4. Existing code/build/QA screenshots continue to work.
5. All moved documentation references resolve after their source files are untracked.
6. Only then approve and perform a separate removal commit; never rewrite Git history as part of this migration.
7. New generated images enter only the inbox, and scheduled upload has survived a real pilot and restart.

When any gate is not met, report **PARTIAL** rather than claiming completed migration.
