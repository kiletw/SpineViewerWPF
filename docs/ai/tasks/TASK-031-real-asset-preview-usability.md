# TASK-031: Real Asset Preview Usability

## Status

Completed on 2026-07-30.

## Objective

Make real Spine assets open with a valid animation selection, a clear preview, and reusable Runtime resources during playback and sequence export.

## Context

- `docs/ai/decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `docs/ai/tasks/TASK-012-static-playback.md`
- User-provided compatibility assets under `D:\Spine測試`
- The current WPF path renders 64 by 64 frames and reloads the skeleton, atlas, and textures for every frame.

## Allowed Paths

- `src/SpineViewerWPF.Application/AssetService.cs`
- `src/SpineViewerWPF.Wpf/**`
- `runtimes/SpineRuntime.Legacy/Adapter.cs`
- `runtimes/SpineRuntime.V40/Adapter.cs`
- `runtimes/SpineRuntime.V41/Adapter.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/**`
- `docs/ai/**`

The three project-owned adapter entry points may be changed to own reusable loaded assets. Vendored Runtime source remains read-only.

## Forbidden Paths

- `runtimes/**/src/**`
- Core contracts
- vendored renderer and Runtime implementations
- unrelated CLI, MCP, docking, or source-editing work
- new packages or a production GPU renderer decision

## Required Behavior

- Opening a real asset cannot lose its valid animation selection during WPF binding refresh.
- A successfully opened real asset is not marked dirty until the user edits it.
- Playback controls are enabled when the selected animation has a positive duration.
- WPF previews and screenshots no longer enlarge a 64 by 64 frame with nearest-neighbor scaling.
- Playback and sequence export reuse one loaded Runtime asset instead of reparsing skeleton, atlas, and textures for every frame.
- Runtime resources have explicit, thread-safe disposal.
- The normal empty shell does not advertise fake prototype assets or a fixed Runtime version.
- Existing deterministic CLI rendering, Runtime isolation, sidecar behavior, and source-file safety are preserved.

## Acceptance Criteria

- A runnable Application smoke renders two frames through one reusable session.
- The supplied 3.6 and 4.1 binary assets show a selected animation, enabled playback controls, and a clean 512 by 512 preview without becoming dirty on open.
- Historical, 4.0, and 4.1 adapters compile and pass their existing fixture checks.
- Documentation records the reusable-session boundary and the remaining CPU-renderer limitation.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v38.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v41.ps1 -Offline
git diff --check
```

Desktop interaction check: open the supplied 3.6 and 4.1 binary assets, verify selection/playback state, and inspect the rendered preview.

## Completion Report

### Changed

- Application and all project-owned Runtime adapters now expose a reusable disposable render session.
- WPF owns one session per scene layer, reuses it for playback, and disposes it on layer removal, replacement, project failure, and shutdown.
- PNG sequence export reuses one session for its complete frame loop.
- Real animation/skin binding refresh rejects invalid transient selections instead of dirtying the document.
- WPF preview, screenshot, and primary sequence export dimensions are 512 by 512 with high-quality WPF scaling.
- The normal empty shell no longer shows fake animation, asset, Runtime, or prototype status data.

### Preserved

- CLI single-frame commands and their recorded 64 by 64 deterministic hashes.
- Runtime selection and isolation across all vendored lines.
- Official Runtime source, source Spine assets, sidecar behavior, cancellation, overwrite policy, and bounded eight-layer scenes.
- The deterministic CPU renderer remains a spike; no GPU or package decision was introduced.

### Validation

- WPF Release build passed with 0 warnings and 0 errors.
- Application smoke passed, including two renders through one reusable session and clean playable selection after real open.
- `scripts/test-v3.ps1` passed all 12 historical lines plus 4.0 and 4.1 baselines.
- Offline official 3.8 and 4.1 JSON/binary render suites passed.
- Computer Use desktop validation opened the supplied 3.6.53 and 4.1.14 binary assets: first animation selected, no dirty marker, playback/screenshot/export enabled, 512 by 512 preview visible, and playback time advancing.
- `git diff --check` passed.

### Gaps and Risks

- The CPU renderer still creates pose state and rasterizes PNGs on the CPU for each frame; this is usable evidence, not the production renderer decision.
- Preview size is a fixed 512 by 512. Viewport-driven resolution, adaptive quality, and GPU presentation remain future renderer work.
- The user-supplied assets remain external compatibility evidence and are not copied into the repository.
- The TASK-030 PowerShell UI Automation smoke was not mixed into the same turn as Computer Use; docking code was unchanged.

### Documentation

- Updated the context index, Runtime matrix, migration plan, Application use cases, UI contracts, and ADR-005.

### Recommended Next Task

Make screenshot and export output represent all visible scene layers and their transforms instead of only the primary asset.
