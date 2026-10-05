# TASK-068: Folder asset browser

Status: Completed on 2026-10-06.

## Objective

Roadmap item 6 from `17-external-feature-reference.md`: browse a folder of
Spine skeletons, filter them, and add them to the scene in batches.

## Context

- User decisions (2026-10-06): a Browser view in the left panel next to
  Layers; double-click adds the file as a layer; no thumbnails yet (name,
  Runtime version, animation count); an Include subfolders toggle, off by
  default.
- `TASK-043` (Add layer), `TASK-066` (user settings file), `TASK-061`
  (stable list instances)

## Scope

Branch `codex/folder-browser`, stacked on `codex/ffmpeg-export` (PR #56) for
the user settings store.

## Allowed Paths

- `src/SpineViewerWPF.Application/AssetBrowser.cs` (new)
- `src/SpineViewerWPF.Wpf/**` (browser view model, panel view, rail button,
  settings fields, folder dialog, text resources)
- `tests/SpineViewerWPF.Application.Smoke/**`, `scripts/test-ui-shell.ps1`
- `docs/ai/00-context-index.md`, `12-application-use-cases.md`,
  `16-ui-state-and-interaction-contracts.md`, `17-external-feature-reference.md`,
  this task

## Forbidden Paths

- Core, Runtime adapters, `runtimes/**`, renderer, sidecar schema, CLI
- thumbnails, disk caches, new packages

## Required Behavior

- `AssetBrowser.Scan` lists `.skel` files and `.json` files whose first 4 KB
  contain `"skeleton"` or `"bones"`, excluding `*.spineviewer.json`; skips
  hidden, system, and inaccessible entries; sorts paths; stops at 5000 files
  and reports truncation; honors cancellation.
- The activity rail gains Browser. Layers and Browser share the left panel
  (and float together); pressing the active view's button closes the panel,
  the other button switches views.
- The Browser view shows the folder, a Folder… chooser, Refresh, a name filter,
  Include subfolders, and a multi-select list of name, details, and relative
  location. Details (`<export version> · N animations` or `setup pose`, or the
  inspection error in italics) load in the background one file at a time and
  stop when the list is rescanned.
- Double-click adds the file as a layer. Add to scene adds the selection in
  order through the existing Add layer path (an empty workspace opens the
  first file; the eight-layer limit applies). Open (single selection) opens
  the file as a new workspace with the existing discard confirmation.
- The folder and the subfolder choice persist in the user settings file next
  to the FFmpeg path; the remembered folder is scanned the first time the
  Browser is shown. Nothing dirties the project.

## Validation

```powershell
dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

### Changed

- Application: `AssetBrowser.Scan` / `IsSkeletonFile`, `AssetBrowserScan`.
- WPF: `AssetBrowserViewModel` and `AssetBrowserItem`; Shell
  `AssetBrowser`, `IsAssetBrowserActive` / `IsLayersPanelActive`,
  `AddLayerPathsAsync`; Browser rail button and view inside
  `BrowsePanelContent`; double-click handler; `OpenFolderDialog`;
  `UserSettingsStore` browser fields (same schema version, additive).
- Tests: `BrowserSmoke.cs`; 14 AutomationIds and the Browser activity check in
  `test-ui-shell.ps1`.

### Preserved

- Layers view content and AutomationIds, Add layer behavior and limits,
  floating Browse panel, FFmpeg setting in the same file.

### Validation (2026-10-06, Windows, local)

- Build: 0 warnings, 0 errors.
- Application.Smoke: passed. Browser checks: top-level and recursive scans
  list exactly the Spine files (sidecar, non-Spine JSON, and text excluded; a
  broken `.skel` is listed and marked as a problem), the limit and
  cancellation, details text, case-insensitive filter and count label, a stable
  list instance while details load, subfolder rescan with relative locations,
  settings round-trip beside the FFmpeg path, command enablement, adding two
  files to an empty workspace in order, Open replacing the workspace, and a new
  session restoring and scanning the remembered folder.
- `scripts/test-ui-shell.ps1`: passed (115 AutomationIds, Browser activity present).
- `git diff --check`: passed.
- Manual (built app, UI Automation and `PrintWindow`): with the official 4.1
  cache folder remembered and subfolders on, the Browser listed
  `spineboy-pro.json` and `.skel` (`4.1.23-beta · 11 animations`, location
  `spineboy`) and excluded the two `*-inspect.json` files; selecting both and
  Add to scene opened the JSON and added the binary as a second layer; the
  filter showed `1 of 2`; canceling the folder dialog kept the folder. The
  temporary user settings file used for this check was removed afterwards.

### Known Gaps

- Choosing a folder in the real dialog was not completed manually (the
  automation tool blocks typing in folder pickers); `SetFolderAsync`, which the
  dialog result calls, is covered by the smoke test.
- Details call the full Runtime inspection per file; very large folders fill
  details gradually.
- Thumbnails remain deferred.

### Integration (2026-10-14)

- Rebased onto `master` after PR #60 (TASK-070 to TASK-075) as one commit.
- `UserSettingsStore` keeps the per-field design from TASK-073/071/074 and adds
  `BrowserFolder` / `BrowserIncludeSubfolders`; older settings files load with defaults.
- The two `ParameterCommand` classes were merged into one that accepts a
  parameter-aware `canExecute` and re-queries on both `CommandManager` and `Refresh()`.
- Re-validated: Release build, Application.Smoke (including TASK-068),
  `scripts/test-v3.ps1`, `scripts/test-ui-shell.ps1` (115 AutomationIds), `git diff --check`.
