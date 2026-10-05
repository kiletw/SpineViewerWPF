# TASK-017: Custom Export FPS

## Status

Completed on 2026-07-27.

## Objective

Allow the WPF export command to use a user-selected frame rate instead of a hardcoded 30 FPS.

## Context

- `../05-technical-constraints.md`
- `../09-ai-working-rules.md`
- `../12-application-use-cases.md`
- `../16-ui-state-and-interaction-contracts.md`
- `TASK-016-png-sequence-export.md`

## Allowed Paths

- `src/SpineViewerWPF.Wpf/App.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- new packages or encoded export formats
- Runtime-specific types through Core or Application
- changes to Spine source assets

## Required Behavior

- The Inspector exposes a bounded export FPS control from 1 through 240, defaulting to 30.
- Export snapshots the selected FPS into the existing Application export request.
- Changing export FPS does not dirty, undo, or alter the saved Viewer project.
- Existing deterministic PNG export, cancellation, progress, and overwrite behavior remain unchanged.

## Acceptance Criteria

- WPF smoke proves a custom FPS changes the exported frame count.
- The UI shell exposes a stable automation ID for the control.
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

FPS is a session-only WPF setting in the bounded 1–240 range; project persistence and fractional-FPS text entry remain deferred.

## Completion Report

Changed: WPF now exposes a bounded Export FPS slider, defaults to 30, and passes the selected value into the existing Application export request. The setting is intentionally outside the editable project snapshot, so it does not affect dirty state or Undo/Redo. UI smoke coverage includes a stable automation ID and the Application smoke exports 11 frames at a custom 10 FPS.

Preserved: deterministic PNG naming, endpoint-inclusive timing, cancellation, progress, overwrite rejection, playback, capture, viewport controls, diagnostics, and source isolation.

Validation: Application smoke, WPF shell (25 automation IDs, 4 shortcuts, compact launch), v3 CLI, and `git diff --check` pass with no build warnings or errors.

Risks: the control is session-only and uses slider-based integer steps; project-persisted presets and fractional-FPS text entry remain deferred.
