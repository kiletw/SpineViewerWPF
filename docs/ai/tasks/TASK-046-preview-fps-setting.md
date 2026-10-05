# TASK-046: Configurable Preview FPS

## Status

Completed.

## Objective

Make interactive playback use one user-configurable preview cadence for both
GPU and CPU paths, defaulting to 30 FPS, while keeping deterministic export FPS
independent.

## Context

- The GPU playback timer currently targets 16 ms while CPU fallback targets 33 ms.
- The status bar reports actual published preview cadence, not source or export FPS.
- Product direction: normal Spine preview defaults to 30 FPS, with an explicit override.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/**`
- `scripts/test-ui-shell.ps1`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- vendored Runtime source
- Core/Application contracts
- Viewer project schema
- new packages

## Required Behavior

- Preview FPS defaults to 30 and is independently configurable from 1 through 240.
- GPU and CPU fallback use the same preview cadence setting.
- Changing Preview FPS does not change Export FPS, dirty the Viewer project, or enter Undo history.
- The status bar continues to report measured published FPS, work time, and coalesced updates.
- Existing playback speed, animation time, deterministic capture/export, and Runtime selection remain unchanged.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore -p:BaseOutputPath=artifacts/application-smoke/bin/
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

### Changed

- Added session-only `PreviewFramesPerSecond`, default 30 and bounded to 1-240.
- GPU and CPU playback now derive their shared timer interval from Preview FPS.
- Added a separate Preview FPS field beside playback/export controls.
- Added smoke coverage for defaults, bounds, Export FPS independence, dirty state, and Undo history.
- Added the stable `Main.Inspector.PreviewFps` automation identity.

### Preserved

- Export FPS remains independent and deterministic export timing is unchanged.
- Playback speed and elapsed-time animation progression remain independent from refresh cadence.
- No Viewer sidecar/schema, Runtime adapter, Core, or Application contract changed.

### Validation

- WPF Release build: passed, 0 warnings / 0 errors.
- Application smoke: passed.
- Full WPF UI shell smoke: passed with 74 automation IDs; the existing Slot physical-click check was stabilized by activating the WPF asset window and waiting for tab layout.
- `git diff --check`: passed with only existing LF/CRLF notices.

### Risks

- `DispatcherTimer` is not a real-time clock, so the status bar intentionally reports measured cadence and may differ slightly from the requested Preview FPS under UI or Runtime load.

### Documentation

- Updated Application use cases, UI product direction, workspace information architecture, and UI interaction contracts.
