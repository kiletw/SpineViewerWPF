# TASK-013: Current-Frame PNG Capture

## Status

Completed on 2026-07-27.

## Objective

Make the existing Screenshot command save the currently displayed deterministic PNG frame through a WPF-owned save path.

## Context

- `../05-technical-constraints.md`
- `../09-ai-working-rules.md`
- `../14-ui-product-design.md`
- `../15-ui-information-architecture.md`
- `../16-ui-state-and-interaction-contracts.md`
- `../decisions/ADR-004-ui-architecture-and-shell.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `TASK-012-static-playback.md`

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
- Core, Application, CLI, and legacy production source
- animation sequence/GIF/video export
- new renderer packages or docking toolkit selection

## Required Behavior

- Screenshot is enabled only when a rendered preview exists.
- The WPF adapter chooses a PNG output path; canceling the dialog is a no-op.
- The command copies the current displayed PNG without changing the active asset, playback position, or dirty state.
- Existing preview and source files remain untouched.
- User-facing strings continue to use resource keys.

## Acceptance Criteria

- Smoke coverage proves the captured PNG exists and matches the displayed preview bytes.
- WPF shell build and automation checks remain green.
- No Runtime source or package changes.

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release -p:BaseOutputPath=.\artifacts\application-smoke\bin\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Completion Report

Changed: the WPF Screenshot command now asks the adapter for a PNG path, copies the current displayed preview, and reports success or a recoverable write error. The smoke test verifies byte equality and unchanged dirty state.

Preserved: current asset, playback position, preview path, source files, project editing, Runtime isolation, and existing automation identifiers.

Validation: Application smoke, WPF shell, and v3 CLI regressions pass with zero build errors; `git diff --check` passes.

Known ceiling: this captures the current 64 by 64 CPU preview PNG. Full viewport capture, GIF/video encoding, and PSD output remain separate export work; TASK-016 now covers deterministic 64 by 64 PNG sequences.

Documentation: context index and migration plan now record current-frame capture as complete.
