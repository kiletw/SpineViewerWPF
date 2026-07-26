# TASK-016: Deterministic PNG Sequence Export

## Status

Completed on 2026-07-27.

## Objective

Replace the WPF Export placeholder with a bounded, deterministic PNG frame-sequence export that reuses the Application renderer path.

## Context

- `../03-behavior-contracts.md`
- `../05-technical-constraints.md`
- `../09-ai-working-rules.md`
- `../12-application-use-cases.md`
- `../16-ui-state-and-interaction-contracts.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `TASK-012-static-playback.md`
- `TASK-013-frame-capture.md`
- `TASK-015-diagnostics-summary.md`

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`
- `src/SpineViewerWPF.Wpf/App.xaml`
- `src/SpineViewerWPF.Wpf/App.xaml.cs`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- new FFmpeg, GIF, video, PSD, or renderer packages
- Runtime-specific types through Core or Application
- changes to Spine JSON, binary, atlas, or texture sources

## Required Behavior

- Application owns deterministic frame timing, frame naming, validation, cancellation, progress, and overwrite policy.
- WPF chooses a PNG sequence prefix, runs export off the UI thread, shows progress, and can cancel.
- Export samples time from zero through the selected animation duration at a fixed FPS and includes the endpoint frame.
- Existing output files are rejected unless overwrite is explicitly requested by the request.
- Export failure/cancellation retains the active asset and project state.

## Acceptance Criteria

- Application smoke proves deterministic frame count, naming, repeated output equality, and cancellation-safe behavior.
- WPF smoke/build checks remain green and the Export command is no longer a placeholder for real assets.
- No Runtime source or package changes.

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release -p:BaseOutputPath=.\artifacts\application-smoke\bin\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Known Ceiling

This first export is 64 by 64 PNG only. FPS customization is covered by TASK-017, while project-persisted export presets, auto-resolution, margins, multi-model composition, GIF/video/FFmpeg, PSD layers, and batch export remain separate work.

## Completion Report

Changed: Core now carries an export request/result/progress contract; Application validates and renders deterministic numbered PNG frames from time zero through the animation endpoint; WPF Export chooses a prefix, runs off the UI thread, reports progress, and supports cancellation.

Preserved: single-frame render/capture, playback, viewport, diagnostics, project editing, source isolation, and the deterministic v3 hash.

Validation: Application smoke covers frame count, naming, repeated byte equality, existing-file rejection, cancellation cleanup, and the WPF command; WPF shell and v3 CLI regressions pass; diff check passes.

Risks: this is a bounded 64 by 64 CPU export. Auto-resolution, margins, multi-model composition, encoded formats, and batch workflows are intentionally deferred.
