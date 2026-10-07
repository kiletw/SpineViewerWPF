# TASK-073: Auto reload on source change, and recent files

Status: Implemented on 2026-10-07.

## Objective

Re-exporting a skeleton from Spine updates the open viewer without a manual
reload, and previously opened assets and projects can be reopened from a menu.

## Context

- Round 2 competitor review, item 4 (`17-external-feature-reference.md`):
  the official Skeleton Viewer reloads changed files; recent files is listed in
  `14-ui-product-design.md` scope but was never implemented.
- The existing per-layer Reload (TASK-043) already reopens a layer from its
  document while keeping its settings; auto reload reuses it.
- Cross-session preferences live in `UserSettingsStore` (TASK-066), not in the
  project sidecar or the per-session asset selection memory (TASK-059).
- File watching is a desktop host concern; no Application use case changes.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/**` (settings store, watcher, Shell, menu, App startup)
- `scripts/test-ui-shell.ps1` (scratch settings path only)
- `tests/SpineViewerWPF.Application.Smoke/**`
- `docs/ai/00-context-index.md`, `docs/ai/16-ui-state-and-interaction-contracts.md`,
  `docs/ai/17-external-feature-reference.md`, this task

## Forbidden Paths

- `runtimes/**`, Core, Application
- the Viewer project sidecar schema
- new packages

## Required Behavior

- `SourceFileWatcher` watches the skeleton, the atlas, and atlas page images
  named in the atlas (lines without `:` ending in an image extension), so
  unrelated files such as exported PNG frames beside the asset do not trigger
  it. Changes are reported once writes have been quiet for 0.6 s.
- Every layer reading a changed file reloads with its current settings and
  z-order; the selection is kept; failures show `LAYER_RELOAD_FAILED` and keep
  the existing layer. While loading, exporting, capturing, or running another
  layer operation, the change is retried after 1 s.
- File > Auto Reload toggles watching, defaults on, and is remembered.
- File > Open Recent lists up to 10 successfully opened assets and projects
  (most recent first, unique ignoring case) by file name with the full path as
  tooltip; a missing file is removed with `RECENT_FILE_MISSING`; File > Clear
  Recent Files empties it. Both are remembered in user settings.
- Settings files written before this task load with defaults for the new fields.
- `--settings=<path>` selects the user settings file; the UI shell test uses a
  scratch file so it never writes the developer's `%APPDATA%` settings.

## Acceptance Criteria

- Watcher batches a multi-file write into one report and ignores unwatched files.
- Shell auto reload reopens a changed layer with its settings and selection,
  ignores unrelated changes, and does nothing while turned off.
- Recent files are recorded, capped, de-duplicated, persisted, and cleared;
  missing entries are removed.
- Running the built application, editing the open skeleton on disk reloads it.
- Existing build, smoke, and UI shell checks pass.

## Validation

```text
dotnet build SpineViewerWPF.sln -c Release
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

- Changed: `UserSettingsStore` (recent files, auto reload), new
  `SourceFileWatcher`, Shell reload/recent-file logic and commands, File menu
  items, `--settings=` startup argument, UI test scratch settings,
  `AutoReloadSmoke`.
- Preserved: manual Reload/Reload Layer behavior and messages; project sidecar
  format; per-session animation/skin memory.
- Validation (2026-10-07): Release build clean; Application.Smoke passed with
  the new TASK-073 checks; `test-ui-shell.ps1` passed and no longer touches the
  real settings file; a live run of the built application opened a copied
  fixture, the skeleton was edited on disk, and the status bar showed
  "Auto-reloaded hero.json" with the new animation listed.
- Gaps: auto reload covers files already loaded; a newly added atlas page is
  watched only after the next reload re-reads the atlas. The Shell-level
  primary animation list is not refreshed by a layer reload (same as manual
  Reload Layer).
- Risks: TBD whether Undo/Redo across an auto reload behaves exactly like
  across a manual Reload Layer (same code path, not separately tested); the
  reloaded asset content itself is never part of history.
