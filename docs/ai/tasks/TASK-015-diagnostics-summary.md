# TASK-015: Diagnostics Summary

## Status

Completed on 2026-07-27.

## Objective

Expose the existing project-owned diagnostics in the WPF shell instead of reducing them to a count or placeholder action.

## Context

- `../03-behavior-contracts.md`
- `../05-technical-constraints.md`
- `../09-ai-working-rules.md`
- `../15-ui-information-architecture.md`
- `../16-ui-state-and-interaction-contracts.md`
- `../decisions/ADR-004-ui-architecture-and-shell.md`
- `TASK-005-wpf-real-asset-open.md`
- `TASK-012-static-playback.md`
- `TASK-014-viewport-navigation.md`

## Allowed Paths

- `src/SpineViewerWPF.Wpf/App.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- Core, Application, CLI, and legacy production source
- new diagnostics packages or logging frameworks
- animation sequence/GIF/video export

## Required Behavior

- Preserve inspect diagnostics and display severity, code, message, and optional path.
- Convert expected open/render failures into one actionable diagnostic while retaining the current state mapping.
- Diagnostics opens and closes from the existing status command without changing asset, playback, or project dirty state.
- Empty diagnostics display a clear no-diagnostics message.
- User-facing panel text continues to use resource keys where applicable.

## Acceptance Criteria

- Smoke coverage proves diagnostics summary/toggle and failed-open diagnostic mapping.
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

This is an in-window summary only; diagnostics filtering, links to repair actions, persistent logs, and a dedicated dockable Diagnostics panel remain future work.

## Completion Report

Changed: the WPF shell now retains project-owned diagnostics, maps open and render failures to actionable codes, and toggles a compact in-window summary from the existing status command.

Preserved: state transitions, metadata retention on failed replacement, playback, viewport, capture, project dirty state, source isolation, and existing automation identifiers.

Validation: Application smoke covers clean, failed-open, and failed-render diagnostics plus toggle behavior; WPF shell and v3 CLI regressions pass; `git diff --check` passes.

Risks: diagnostics are session-only presentation state; filtering, repair actions, persistent logs, and a dockable panel are intentionally deferred.
