# TASK-033: Interactive Viewport Fidelity Baseline

## Status

Completed on 2026-07-31.

## Objective

Remove the fixed 512-by-512 disk-backed WPF playback preview and establish a DPI-aware in-memory viewport frame with interactive texture filtering and explicit RGBA/RGB/Alpha inspection.

## Context

- `docs/ai/decisions/ADR-004-ui-architecture-and-shell.md`
- `docs/ai/decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `docs/ai/decisions/ADR-006-dockable-workspace-and-localization-boundary.md`
- `docs/ai/decisions/ADR-008-in-memory-interactive-preview.md`
- TASK-031 enlarged the WPF preview to a fixed 512-by-512 PNG but retained per-frame PNG encoding, disk writes, and display scaling.
- A maximized 2560-by-1440 desktop check exposed visible scaling, nearest-sampled textures, inaccessible selected-layer controls at smaller sizes, and literal access-key underscores.

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`
- `src/SpineViewerWPF.Wpf/**`
- `runtimes/SpineRuntime.Legacy/Adapter.cs`
- `runtimes/SpineRuntime.V40/Adapter.cs`
- `runtimes/SpineRuntime.V41/Adapter.cs`
- `runtimes/SpineRuntime.V41/CpuRenderer.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**/src/**`
- `SpineViewerWPF/SpineLibrary/**`
- CLI behavior or deterministic file-render baseline changes
- new renderer or UI packages
- GPU backend selection
- multi-track editing
- scene-composited export
- Spine source writing

## Required Behavior

- WPF preview frames are returned as Runtime-neutral BGRA pixels and displayed without temporary PNG files.
- Interactive frames use bilinear texture filtering while deterministic CLI/file rendering retains its verified sampling and hashes.
- The preview render size follows the viewport's physical-pixel aspect and is bounded to protect CPU and memory use.
- The viewport exposes RGBA, RGB, and Alpha inspection without changing the source asset.
- Screenshot capture writes the currently selected viewport channel from the current in-memory frame.
- Button labels do not show literal access-key underscores.
- Browse content scrolls so selected-layer Alpha/PMA controls remain reachable at the supported minimum window size.
- Existing Runtime isolation, per-layer controls, project sidecars, export, and source-file safety remain unchanged.

## Acceptance Criteria

- Application smoke validates in-memory frame dimensions, byte length, refresh, per-layer Alpha response, and channel conversion.
- WPF shell automation includes a stable viewport-channel ID and rejects literal underscore labels.
- Historical, 4.0, 4.1, official 3.8, and official 4.1 deterministic file-render suites retain their baselines.
- Desktop QA loads the user-supplied 4.1 asset, verifies RGBA/Alpha switching, checks the minimum-size panel scroll, and compares the viewport at normal and maximized sizes.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore -p:BaseOutputPath=artifacts/application-smoke/bin/
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v38.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v41.ps1 -Offline
git diff --check
```

## Completion Report

- Interactive WPF playback now consumes bounded in-memory BGRA frames, uses bilinear filtering, follows viewport aspect, and supports RGBA/RGB/Alpha inspection without temporary PNG files.
- Atlas loaders preserve a declared page size and only fall back to decoded texture dimensions when the atlas omits its size. This fixes globally shifted UV sampling for padded textures such as the user-supplied 4.1.14 `xiu` asset, whose atlas declares `2048 x 1962` while its PNG is `2048 x 2048`.
- The same atlas-size rule is applied at the project-owned legacy, 4.0, and 4.1 adapter boundaries; vendored Runtime source remains unchanged.
- Application smoke includes a mismatched declared/physical texture-size regression and verifies all four texture quadrants remain reachable.
- The user asset was rendered through the interactive frame path at `1536 x 1024`; face, hands, clothing, and cape no longer sample unrelated atlas regions.
- Preserved behavior: deterministic CLI/file rendering hashes, PMA/Track Alpha behavior, Runtime selection, multi-page loading, project sidecars, scene layers, and source-file safety.
- Validation passed: WPF Release build, Application smoke, UI shell automation, `test-v3.ps1`, official 3.8 offline smoke, official 4.1 offline smoke, and `git diff --check`; builds completed with zero warnings and zero errors.
- Remaining boundary: the CPU renderer is still the accepted interactive baseline rather than a production GPU-parity decision.
