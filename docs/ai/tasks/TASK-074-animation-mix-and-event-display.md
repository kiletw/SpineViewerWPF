# TASK-074: Animation mix and Spine event display

Status: Implemented on 2026-10-08. The product owner approved the cross-adapter
contract work on 2026-10-07.

## Objective

Switching animations during playback can crossfade like the official Skeleton
Viewer, and Spine event keys are visible on the timeline and as playback passes
them, on every supported Runtime line.

## Context

- Round 2 competitor review, item 5 (`17-external-feature-reference.md`).
- Adapters pose statelessly per request (`SetAnimation` + `Update(time)`), so a
  mix must be described in the request rather than kept in adapter state.
- Five adapter sources cover all 16 lines: `SpineRuntime.Legacy/Adapter.cs`
  (2.1.08-4.0.31 via conditional compilation), V40, V41, V42, V43. Official
  Runtime sources under `runtimes/*/src/**` stay read-only.
- Layer animation picks keep the global playback position; the primary
  selection resets it to 0.

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs`, `src/SpineViewerWPF.Application/AssetService.cs`
- `runtimes/SpineRuntime.Legacy/Adapter.cs`, `runtimes/SpineRuntime.V40/Adapter.cs`,
  `runtimes/SpineRuntime.V41/Adapter.cs`, `runtimes/SpineRuntime.V42/Adapter.cs`,
  `runtimes/SpineRuntime.V43/Adapter.cs`
- `src/SpineViewerWPF.Wpf/**`, `tests/SpineViewerWPF.Application.Smoke/**`
- `docs/ai/00-context-index.md`, `docs/ai/16-ui-state-and-interaction-contracts.md`,
  `docs/ai/17-external-feature-reference.md`, this task

## Forbidden Paths

- official Runtime source (`runtimes/*/src/**`), renderers
- export output (exports are never mixed)
- new packages

## Required Behavior

- `AnimationDescriptor.Events` lists each animation's event keys (time, name,
  int, float, string) in time order, read from the event timeline on all lines.
- `AnimationMix(FromAnimation, FromTimeSeconds, DurationSeconds, ElapsedSeconds)`
  on frame and scene requests: the source plays from FromTimeSeconds for
  ElapsedSeconds while the requested animation, at the request time, mixes in
  over DurationSeconds through the Runtime's own `AnimationState` mixing
  (`DefaultMix`, then the target track time is set to request time - elapsed).
  Finished, unknown, or empty sources render exactly as without a mix. Physics
  lines replay the source and the elapsed mix with the same 60 Hz stepping.
- Shell: Mix (s) under Timing & Playback, 0-5 s, default 0 (off), remembered in
  user settings. Changing a layer's animation (or the primary selection) while
  playing starts a mix from the previous animation at its current time; mixes
  advance with playback and end when finished, when playback pauses, or when the
  timeline wraps. Preview only.
- The timeline marks the selected layer's event keys (tooltip: time, name,
  values). During playback the last event passed shows briefly in the viewport.
  Neither reaches Screenshot or export.

## Acceptance Criteria

- On all 16 lines: inspect reports an injected event key with its defaults; a
  mixed scene lies strictly between source and target (Physics lines: differs
  from the target); finished/invalid mixes render unchanged; CPU frames mix.
- Shell: markers and labels, default-off and clamped/remembered duration, no mix
  when paused, mix start/advance/finish/wrap/pause behavior.
- Existing output is unchanged when no mix is requested (official 4.2/4.3 hashes).

## Validation

```text
dotnet build SpineViewerWPF.sln -c Release
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v42.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v43.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

- Changed: event and mix contracts; five adapter sources (event extraction,
  stateless mix, 4.2/4.3 `Replay` helper); render session `mix` parameter; Shell
  mix state, timeline markers, event label; `MixEventsSmoke`.
- Preserved: unmixed poses and exports; TASK-072 warm-up stepping.
- Validation (2026-10-08): all commands passed; a live capture of spineboy-pro
  `run` showed its two `footstep` markers on the timeline.
- Gaps: no event audio; markers cover the selected layer only; the mix does not
  replay the target's own Physics history before the switch; mixes end when
  the timeline wraps.
