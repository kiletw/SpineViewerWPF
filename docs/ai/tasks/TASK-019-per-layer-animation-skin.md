# TASK-019: Per-Layer Animation and Skin

## Status

Completed on 2026-07-27.

## Objective

Allow each scene layer to select its own animation and skin while sharing the existing viewport clock.

## Context

- `../05-technical-constraints.md`
- `../09-ai-working-rules.md`
- `../12-application-use-cases.md`
- `../17-external-feature-reference.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `TASK-018-multi-skeleton-scene.md`

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`
- `src/SpineViewerWPF.Wpf/App.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/SceneLayerViewModel.cs`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- new renderer, docking, or animation packages
- Spine source writes
- scene-level GIF/video/PSD export

## Required Behavior

- Each scene layer exposes its inspected animation and skin lists.
- Changing a layer's animation or skin rerenders that layer through the existing Application path.
- The shared playback clock clamps each layer to its own animation duration.
- Existing primary-asset animation/skin controls, scene ordering, transforms, save behavior, and source isolation remain intact.

## Acceptance Criteria

- Application smoke proves scene layers retain independent animation/skin metadata and render requests.
- WPF smoke exposes stable per-layer animation and skin automation IDs.
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

Layers share one viewport clock and still export through the existing primary-asset PNG sequence command; per-layer timelines, track mixing, scene export, and batch export remain deferred.

## Completion Report

Changed: each Scene Layer now retains its inspected animation and skin lists, exposes selected animation/skin controls, rerenders through the existing Application scene path, and clamps the shared playback time to that layer's animation duration.

Preserved: primary-asset playback controls, scene ordering and transforms, per-layer visibility/opacity, sidecar persistence, deterministic PNG rendering, export FPS, diagnostics, and source isolation.

Validation: Application smoke covers independent layer metadata and per-layer property persistence; WPF shell reports 37 automation IDs and 4 shortcuts with zero build warnings; v3 CLI and `git diff --check` pass.

Risks: layers still share one viewport clock; independent timelines, track mixing/alpha, scene export, and batch export remain deferred.
