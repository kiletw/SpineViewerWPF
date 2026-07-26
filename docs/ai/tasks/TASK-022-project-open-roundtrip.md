# TASK-022: Open Viewer Project Round-Trip

## Status

Completed on 2026-07-27.

## Objective

Let the WPF shell open a saved `*.spineviewer.json` sidecar and restore its asset, playback settings, transforms, and bounded scene layers without modifying Spine source files.

## Context

- `../09-ai-working-rules.md`
- `../decisions/ADR-007-non-destructive-viewer-project.md`
- `src/SpineViewerWPF.Application/ViewerProjectStore.cs`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`

## Allowed Paths

- `src/SpineViewerWPF.Wpf/App.xaml`
- `src/SpineViewerWPF.Wpf/App.xaml.cs`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `src/SpineViewerWPF.Wpf/SceneLayerViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- Spine source writes
- new packages or renderer changes

## Required Behavior

- The user can select and open a Viewer sidecar from the WPF shell.
- Opening a project restores the primary asset, selected animation/skin, playback settings, transform, background, and saved scene-layer state.
- All assets are loaded and rendered before replacing the current session; a failed open preserves the current session.
- Opening a valid project starts clean and keeps source Spine files read-only.

## Acceptance Criteria

- Application smoke proves save then open restores a three-layer project.
- WPF smoke exposes a stable Open Project automation ID.
- Existing UI, CLI, official-runtime, and diff validations remain green.

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release -p:BaseOutputPath=.\artifacts\application-smoke\bin\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Completion Report

Implemented the WPF Open Project command and Ctrl+Shift+O shortcut. A saved sidecar is loaded and validated off the UI thread, each asset is rendered before the current session is replaced, and the saved primary settings plus scene-layer transforms, visibility, opacity, z-order, animation, and skin are restored. Failed loads leave the existing session unchanged; source Spine files remain read-only.

Validation passed:

- Application smoke: save/reopen restores a three-layer project, transforms, opacity, and preview files; a failed reopen preserves the current session.
- WPF shell smoke: 39 automation IDs, including `Main.Command.OpenProject`.
- v3 deterministic CLI validation.
- Official 4.1 offline runtime validation.
- `git diff --check`.

Known boundary: runtime language switching and Spine source editing remain deferred by the existing product contract.
