# TASK-018: Multi-Skeleton Scene Layers

## Status

Completed on 2026-07-27.

## Objective

Add a bounded scene layer list so multiple Spine assets can be displayed together in the WPF viewport and saved in the Viewer sidecar.

## Context

- `../05-technical-constraints.md`
- `../09-ai-working-rules.md`
- `../12-application-use-cases.md`
- `../16-ui-state-and-interaction-contracts.md`
- `../17-external-feature-reference.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `../decisions/ADR-007-non-destructive-viewer-project.md`
- `../decisions/ADR-006-dockable-workspace-and-localization-boundary.md`
- `TASK-004-editable-project-session.md`
- `TASK-012-static-playback.md`

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`
- `src/SpineViewerWPF.Application/ViewerProjectStore.cs`
- `src/SpineViewerWPF.Wpf/App.xaml`
- `src/SpineViewerWPF.Wpf/App.xaml.cs`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml.cs`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `src/SpineViewerWPF.Wpf/SceneLayerViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- new renderer or docking packages
- Spine JSON, binary, atlas, or texture source writes
- GIF/video/PSD export

## Required Behavior

- A scene may contain up to eight Spine layers.
- Each layer has an asset source, preview image, visibility, opacity, z-order, and transform.
- Layers render through Application contracts; WPF only presents and edits the layer list.
- Add, remove, select, and reorder operations work without discarding the other layers.
- Viewer sidecars preserve the layer list while remaining backward-compatible with single-layer documents.
- Existing single-asset playback, capture, export, diagnostics, undo/redo, and source isolation remain intact.

## Acceptance Criteria

- Application smoke proves multiple layer render requests use the existing deterministic renderer path.
- WPF smoke verifies stable layer automation IDs and a compact launch.
- A saved project contains the scene layer list.
- Application, WPF, v3, and diff validations remain green.
- No Runtime source or package changes.

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release -p:BaseOutputPath=.\artifacts\application-smoke\bin\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Known Ceiling

This slice composites layers as independent 64 by 64 WPF images, supports at most eight layers, and does not yet provide scene-level export, per-layer animation/skin timeline controls, project-open UI, drag/drop placement, or dockable layer panels.

## Completion Report

Changed: Core now defines scene layer documents and render requests; Application renders a bounded list through the existing Runtime adapter; WPF exposes a Scene Layers panel with add, remove, visibility, ordering, opacity, and selected-layer transform controls. Saved Viewer sidecars now preserve the optional layer list while old single-layer documents remain valid.

Preserved: single-asset open/playback, deterministic rendering, capture, export FPS, export cancellation/progress, diagnostics, Undo/Redo for the existing editor fields, and read-only Spine source files.

Validation: Application smoke covers two deterministic scene layer renders, WPF layer creation/reordering/rendered previews, dirty state, and sidecar persistence; WPF shell reports 35 automation IDs and 4 shortcuts with zero build warnings; v3 CLI and `git diff --check` pass.

Risks: composition is presentation-layer stacking of independent 64 by 64 transparent PNGs, not a scene-wide renderer or export path. Additional layers use their first animation/skin; scene-layer edits mark dirty and save but are not yet part of the existing Undo history; project-open UI is still deferred.
