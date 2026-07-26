# TASK-020: Batch Scene Import and Auto Layout

## Status

Completed on 2026-07-27.

## Objective

Make multi-skeleton scenes practical by importing several assets at once and arranging them into a non-overlapping grid.

## Context

- `../05-technical-constraints.md`
- `../09-ai-working-rules.md`
- `../12-application-use-cases.md`
- `../17-external-feature-reference.md`
- `../decisions/ADR-007-non-destructive-viewer-project.md`
- `TASK-018-multi-skeleton-scene.md`
- `TASK-019-per-layer-animation-skin.md`

## Allowed Paths

- `src/SpineViewerWPF.Wpf/App.xaml`
- `src/SpineViewerWPF.Wpf/App.xaml.cs`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- new packages or renderer changes
- Spine source writes
- scene export formats

## Required Behavior

- The layer picker can select multiple skeleton files in one operation.
- The existing eight-layer bound remains enforced.
- Imported layers retain independent inspected animation/skin metadata.
- Auto Layout arranges layers in a deterministic grid with spacing so default previews do not overlap.
- Add/import and layout changes mark the Viewer project dirty and remain saveable.

## Acceptance Criteria

- Application smoke proves batch layer creation and deterministic non-overlap positions.
- WPF smoke exposes stable batch/layout automation IDs.
- Existing Application, WPF, v3, and diff validations remain green.
- No Runtime source or package changes.

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release -p:BaseOutputPath=.\artifacts\application-smoke\bin\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Known Ceiling

Auto Layout is a fixed 64-pixel preview grid; collision-aware bounds, drag placement, multi-select transforms, and scene export remain deferred.

## Completion Report

Changed: the asset picker now supports multi-select, the Scene Layers command imports all selected assets up to the eight-layer bound, and Auto Layout places layers in a deterministic grid with distinct positions. Batch import and layout changes participate in dirty state and sidecar save.

Preserved: per-layer animation/skin metadata, visibility/opacity, ordering, transforms, primary playback, deterministic PNG rendering, export FPS, diagnostics, and source isolation.

Validation: Application smoke covers batch creation of three layers, independent opacity, deterministic grid separation, reorder, and sidecar persistence; WPF shell reports 38 automation IDs and 4 shortcuts with zero build warnings; v3 CLI and `git diff --check` pass.

Risks: layout uses fixed 64-pixel preview spacing rather than runtime bounds. Drag placement, collision-aware layout, multi-select transforms, and scene export remain deferred.
