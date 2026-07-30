# TASK-032: Effective Per-Layer Alpha Controls

## Status

Completed on 2026-07-30.

## Objective

Make the visible Track Alpha and PMA controls drive real Runtime rendering per scene layer and survive Viewer-project save/open.

## Context

- `docs/ai/decisions/ADR-002-runtime-isolation.md`
- `docs/ai/decisions/ADR-004-ui-architecture-and-shell.md`
- `docs/ai/decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `docs/ai/decisions/ADR-007-non-destructive-viewer-project.md`
- TASK-031 left WPF Track Alpha as presentation-only state and hard-coded PMA off.
- Historical Runtime lines through 3.4 expose the track weight as `TrackEntry.Mix`; 3.5 and later expose `TrackEntry.Alpha`.

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`
- `src/SpineViewerWPF.Application/ViewerProjectStore.cs`
- `src/SpineViewerWPF.Wpf/App.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/SceneLayerViewModel.cs`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `runtimes/SpineRuntime.Legacy/Adapter.cs`
- `runtimes/SpineRuntime.V40/Adapter.cs`
- `runtimes/SpineRuntime.V41/Adapter.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**/src/**`
- `SpineViewerWPF/SpineLibrary/**`
- `CpuRenderer.cs`
- CLI behavior changes
- Spine source writing
- multi-track editing
- scene-composited export
- new packages or a production renderer decision

## Required Behavior

- Each scene layer owns a Track 0 alpha value from 0 through 1 and a PMA toggle.
- Both controls are visible in the selected-layer UI and changes render without reopening the asset.
- Runtime adapters apply Track 0 alpha through the matching upstream API without exposing Runtime types outside the adapter.
- WPF playback and the primary PNG-sequence export pass the selected layer settings instead of hard-coding PMA off.
- Viewer sidecars round-trip the per-layer values while existing schema-version-1 sidecars still load; the prior top-level Track Alpha remains the primary-layer fallback.
- Invalid Track Alpha values fail before rendering or saving.
- Defaults remain Track Alpha `1` and PMA `false`, preserving existing deterministic hashes.

## Acceptance Criteria

- Application smoke proves Track Alpha changes rendered output, rejects an invalid value, and round-trips independent layer Alpha/PMA values.
- WPF shell automation includes stable IDs for Track Alpha and PMA.
- Historical, 4.0, 4.1, official 3.8, and official 4.1 validations retain their default baselines.
- Documentation records effective per-layer Alpha/PMA behavior and the remaining single-track/CPU-preview limits.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v38.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v41.ps1 -Offline
git diff --check
```

## Completion Report

- Changed behavior:
  - Track 0 alpha and PMA are effective per-layer rendering inputs in every supported Runtime adapter.
  - The selected-layer inspector exposes distinct layer opacity, Track 0 alpha, and PMA controls.
  - Playback, preview refresh, and primary-layer PNG-sequence export use the selected layer's Track 0 alpha and PMA.
  - Viewer-project save/open round-trips both values, with schema-v1 compatibility through the prior top-level Track Alpha fallback.
  - Invalid non-finite or out-of-range Track 0 alpha values fail before rendering or saving.
- Preserved behavior:
  - Defaults remain Track 0 alpha `1` and PMA `false`.
  - Existing deterministic render baselines and Runtime selection behavior are unchanged.
  - Core and Application APIs remain Runtime-type-free; vendored upstream Runtime source is unchanged.
  - CLI behavior and Spine source files are unchanged.
- Validation:
  - Release WPF build passed with 0 warnings and 0 errors.
  - Application smoke, WPF UI Automation, V3 historical/4.0/4.1, official 3.8 offline, and official 4.1 offline suites passed.
  - Desktop QA loaded `D:\Spine測試\spine4.1.0样本\xiu.skel`; Runtime 4.1 rendered successfully, Track 0 alpha `1.00` to `0.00` refreshed the model, and PMA visibly changed blending.
  - `git diff --check` passed.
- Remaining limits:
  - The viewport still displays CPU-rendered PNG frames instead of a persistent live Runtime surface.
  - Only Track 0 is editable.
  - Export remains primary-layer-only rather than scene-composited.
  - Background display modes do not yet provide a dedicated exported-alpha-channel control.
