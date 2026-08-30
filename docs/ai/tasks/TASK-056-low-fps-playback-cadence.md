# TASK-056: Preserve low-FPS playback cadence

Status: Completed on 2026-08-31.

## Objective

Make Preview FPS values from 1 through 3 advance animation at real playback
speed instead of being throttled by the normal high-FPS stall clamp.

## Context

- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/decisions/ADR-009-wpf-gpu-preview.md`
- TASK-046 exposes a 1-through-240 Preview FPS range with a 30 FPS default.
- Adjacent review identified that `AdvancePlayback` clamps every tick to 0.25
  seconds, so a one-second timer advances only one quarter second.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `docs/ai/00-context-index.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- this task document

## Forbidden Paths

- Core, Application, Runtime, renderer, export, and project schema changes
- `runtimes/**` and vendored upstream code
- playback-thread or timer replacement

## Required Behavior

- A scheduled 1, 2, or 3 FPS tick may advance by its real frame interval.
- Preview FPS at normal rates retains the existing 0.25-second stall clamp.
- Delayed ticks remain bounded to avoid large jumps after UI stalls.
- The 30 FPS default and 1-through-240 setting range remain unchanged.

## Acceptance Criteria

- A live 1 FPS smoke case advances substantially more than 0.25 seconds on its
  first scheduled tick.
- Existing playback, GPU/CPU preview, settings, build, and UI shell checks pass.
- Documentation records the adaptive clamp.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

### Changed

- Playback elapsed time is capped at the larger of 0.25 seconds or 1.5 current
  Preview FPS intervals.
- A scheduled 1 FPS tick can now advance a full second instead of 0.25 seconds.
- Application smoke runs the real DispatcherTimer at 1 FPS and rejects the old
  slow-motion behavior.

### Preserved

- The 30 FPS default, 1-through-240 range, GPU/CPU shared cadence, playback
  speed, looping, pause behavior, and export timing are unchanged.

### Validation

- Isolated WPF Release build: passed with 0 warnings and 0 errors.
- Application smoke: passed, including the live 1 FPS DispatcherTimer case.
- UI shell validation: passed with 85 AutomationIds, 8 editor shortcuts, 24
  compact-workspace tokens, Slots availability, and Duplicate layer interaction.
- `git diff --check`: passed; Git only reported the repository's expected LF to
  CRLF checkout notice.

### Gaps and risks

- Playback still intentionally drops excess elapsed time after a delay beyond
  the adaptive safety bound rather than jumping arbitrarily far.
