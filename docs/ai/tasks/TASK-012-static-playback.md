# TASK-012: Static Preview Playback

## Status

Completed on 2026-07-27.

## Objective

Turn the WPF Play/Pause, Stop, loop, speed, and timeline controls into a bounded playback slice that re-renders the current animation through the existing deterministic CPU renderer.

## Context

- `../05-technical-constraints.md`
- `../09-ai-working-rules.md`
- `../16-ui-state-and-interaction-contracts.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `TASK-010-bounds-aware-fit.md`
- `TASK-011-atlas-discovery.md`

## Allowed Paths

- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/SpineRuntime.V41/src/**`
- Core, Application, CLI, and legacy production source
- new renderer packages or multi-asset scene work

## Required Behavior

- advance the selected animation on a WPF dispatcher tick
- honor pause, stop, loop, and playback speed
- render off the UI thread and publish the latest PNG safely
- reset the timeline when changing animation or skin
- keep the existing static render and project-editing behavior

## Acceptance Criteria

- the Play/Pause command changes actual preview frames and the timeline position
- Stop returns to time zero and renders the setup time
- non-looping playback stops at the animation duration; looping wraps to zero
- Application, v3, and WPF shell regressions remain green
- no Runtime source or package changes

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release -p:BaseOutputPath=.\artifacts\application-smoke\bin\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
git diff --check
```

## Completion Report

Changed: the WPF shell now owns a dispatcher timer for playback state and re-renders animation frames through `AssetService` on a worker task. Position, duration label, loop, speed, animation, and skin changes are connected to the same frame path.

Preserved: deterministic CLI output, static fit, atlas discovery, source isolation, sidecar editing, and existing automation identifiers.

Known gaps: this is still a single-asset CPU playback slice; full viewport/sequence export, production GPU rendering, multi-track timelines, and frame coalescing beyond one pending render remain future work. TASK-013 now covers current-frame PNG capture.
