# TASK-072: Export batch, frame range, and Physics warm-up

Status: Implemented on 2026-10-07.

## Objective

Users can export every animation of the selected layer in one command, export a
custom time range of the current animation, and let Spine 4.2+ Physics settle
before the first exported frame.

## Context

- Round 2 competitor review, item 3 (`17-external-feature-reference.md`);
  references are the Spine Editor image/video export options.
- Export previously covered exactly `[0, duration]` of one animation, and
  Physics replay always started from rest at time 0 (TASK-041), so looping
  exports of hair/cloth started with a visible settle.
- Use-case logic belongs in Application, not WPF (AGENTS.md).
- `runtimes/**` is read-only by default; this task permits a patch to the
  project-owned 4.2 and 4.3 adapter files only (`Adapter.cs`), not to official
  Runtime source.

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`
- `runtimes/SpineRuntime.V42/Adapter.cs`, `runtimes/SpineRuntime.V43/Adapter.cs`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`, `MainWindow.xaml`, `App.xaml`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `docs/ai/00-context-index.md`, `docs/ai/16-ui-state-and-interaction-contracts.md`,
  `docs/ai/17-external-feature-reference.md`, this task

## Forbidden Paths

- official Runtime source under `runtimes/*/src/**`
- other adapters, renderers, and the CLI
- new packages

## Required Behavior

- `AnimationExportRequest.StartSeconds` (default 0): frames run from the start
  time to `DurationSeconds` inclusive at the export FPS; auto-fit bounds cover
  only the exported frames. Start must be within `[0, DurationSeconds]`.
- `AssetService.ExportBatch` exports each named animation over its own full
  duration. The animation varies on the single-layer request, or on one scene
  layer chosen by index. Outputs are `<name>-<animation>-0000.png` sequences or
  one `<name>-<animation><ext>` encoded file each, beside the chosen path, with
  invalid file-name characters replaced by `_` and collisions suffixed. Any
  existing target fails the batch before rendering; a failed or canceled batch
  deletes everything it wrote. Progress covers all frames (twice for encoded).
- `PhysicsWarmupLoops` (0-10, default 0) on render, scene, and export requests:
  the 4.2 and 4.3 adapters first play the animation looped that many times
  (60 Hz steps, at most 600), then restart it at time 0 with the physics state
  kept and pose the requested time as before. 0 keeps the previous output byte
  for byte; Runtimes without Physics ignore it.
- WPF Export settings add Animations (Current / All animations), Range (Full /
  Custom start/end seconds, hidden for All animations), and Physics warm-up.
  All are session preferences: they do not dirty the project or enter Undo.

## Acceptance Criteria

- Range frames equal the same times in a full export; invalid starts fail
  before creating the output directory.
- Batch PNG and GIF exports write one output per animation with stable,
  sanitized names; conflicts write nothing; cancellation leaves nothing.
- Scene batch varies only the chosen layer.
- Physics warm-up changes the 4.2/4.3 fixture poses deterministically, is a
  no-op for 4.1, and leaves the official 4.2/4.3 render hashes unchanged at 0.
- Shell batch/range flow works end to end and does not dirty the project.

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

- Changed: export contracts (`StartSeconds`, `PhysicsWarmupLoops`,
  `AnimationBatchExportRequest`), `AssetService.ExportBatch`, 4.2/4.3 adapter
  warm-up replay, WPF export settings, `BatchExportSmoke`.
- Preserved: default export output (range Full, warm-up 0) byte for byte;
  existing export naming, overwrite, cancellation, and encoded behavior.
- Validation (2026-10-07): Release build clean; Application.Smoke passed
  including the new TASK-072 checks and the GIF batch with a local FFmpeg 6.1;
  `test-v3.ps1`, `test-v42.ps1 -Offline`, `test-v43.ps1 -Offline`, and
  `test-ui-shell.ps1` passed.
- Gaps: the custom range uses seconds, not editor frame numbers; All animations
  always exports full durations; the CLI does not expose these options; the new
  export controls were not inspected visually beyond the UI shell launch test.
- Risks: warm-up replays from time 0 for every frame, so long warm-ups multiply
  export time; replay is capped at 600 steps per phase, so warm-ups longer than
  10 s use coarser steps.
