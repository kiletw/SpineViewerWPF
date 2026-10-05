# TASK-014: Viewport Navigation

## Status

Completed on 2026-07-27.

## Objective

Connect the existing Fit command and viewport gestures without changing editable model transforms.

## Context

- `../03-behavior-contracts.md`
- `../05-technical-constraints.md`
- `../09-ai-working-rules.md`
- `../14-ui-product-design.md`
- `../16-ui-state-and-interaction-contracts.md`
- `../decisions/ADR-004-ui-architecture-and-shell.md`
- `TASK-004-editable-project-session.md`
- `TASK-012-static-playback.md`
- `TASK-013-frame-capture.md`

## Allowed Paths

- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml.cs`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- Core, Application, CLI, and legacy production source
- new renderer packages or docking toolkit selection

## Required Behavior

- Ctrl+mouse wheel adjusts viewport zoom in a bounded range.
- Ctrl+left-drag pans the viewport.
- Fit resets only viewport zoom and pan.
- Model transform edits remain separate and continue to drive project dirty/undo behavior.
- The same viewport transform applies to real PNG and prototype previews.

## Acceptance Criteria

- Smoke coverage proves zoom/pan and Fit without changing model transforms or dirty state.
- WPF shell build and automation checks remain green.
- No Runtime source or package changes.

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release -p:BaseOutputPath=.\artifacts\application-smoke\bin\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Known Ceiling

This is a single-window viewport transform with fixed bounds; pinch gestures, inertial pan, persisted layout, and a production renderer remain future work.

## Completion Report

Changed: the WPF viewport now handles Ctrl+wheel zoom, Ctrl+drag pan, and a Fit command backed by separate bounded viewport state. The same derived transform drives both real PNG and prototype previews.

Preserved: model transform editing, dirty state, Undo/Redo, playback, capture, source isolation, and existing automation identifiers.

Validation: Application smoke, WPF shell, and v3 CLI regressions pass; `git diff --check` passes. The first parallel build attempt hit a shared MSBuild `obj` file lock, then the same validations passed sequentially.

Risks: viewport state is session-only and intentionally not serialized into the project sidecar; it is not a production camera model.
