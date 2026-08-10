# TASK-034: User Asset Compatibility Verification

## Status

Completed on 2026-07-31.

## Objective

Verify that every Spine asset set under `D:\Spine測試` can be inspected and rendered through the existing v3 Application path, and fix only reproducible adapter or renderer defects exposed by those files.

## Context

- `docs/ai/04-runtime-matrix.md`
- `docs/ai/decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `docs/ai/decisions/ADR-008-in-memory-interactive-preview.md`
- TASK-033 fixed declared atlas page dimensions for the user-supplied 4.1.14 `xiu` asset.
- The remaining `illust_r_2110601_marianne01_01` set contains one binary skeleton, one atlas, and five texture pages.

## Allowed Paths

- `runtimes/SpineRuntime.Legacy/Adapter.cs`
- `runtimes/SpineRuntime.V40/Adapter.cs`
- `runtimes/SpineRuntime.V41/Adapter.cs`
- `runtimes/SpineRuntime.V41/CpuRenderer.cs`
- `src/SpineViewerWPF.Application/**`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/**`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**/src/**`
- `SpineViewerWPF/SpineLibrary/**`
- new renderer or UI packages
- UI layout or feature expansion
- Spine source writing
- copying user assets into the repository

## Required Behavior

- Every skeleton under `D:\Spine測試` is inspected using the correct Runtime line.
- Every referenced atlas page resolves without copying or modifying the source files.
- A bounded in-memory frame renders without shifted UVs, missing pages, or adapter exceptions.
- Existing deterministic fixtures and official Runtime baselines remain unchanged.

## Acceptance Criteria

- Record the detected export version, animation/skin/page counts, and render result for each user asset set.
- Any code correction has one smallest runnable regression.
- User files retain their original hashes.
- Documentation records verified behavior and remaining visual-parity limits.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore -p:BaseOutputPath=artifacts/application-smoke/bin/
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v38.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v41.ps1 -Offline
git diff --check
```

## Completion Report

- Changed: no production code was required after TASK-033; this task adds only compatibility evidence and documentation.
- Verified `xiu`: binary export `4.1.14`, selected Runtime `4.1`, one atlas page, two animations (`Idle`, `Touch Idle`), one skin, and a clean `1536 x 1024` interactive frame after declared atlas-size preservation.
- Verified `illust_r_2110601_marianne01_01`: binary export `3.6.53`, selected Runtime `3.6.53`, five referenced atlas pages, five one-second animations (`B1`, `B2`, `F1`, `F2`, `F3`), and three skins (`default`, `DS`, `FS`).
- All five 3.6.53 animations rendered at `1024 x 1024`; all three skin selections executed without missing-page or adapter errors. The in-memory `1536 x 1024` frame contained `6,291,456` BGRA bytes and `986,567` non-transparent pixels.
- The fourth referenced page is intentionally `0.png`; the adjacent `illust_r_2110601_marianne01_014.png` is not referenced by the atlas.
- Preserved: no files under `D:\Spine測試` were written or copied, no vendored Runtime source changed, and existing Runtime/version-selection behavior remained intact.
- Validation: the same production source state passed WPF Release build, Application smoke, `test-v3.ps1`, official 3.8 offline smoke, official 4.1 offline smoke, and `git diff --check`; builds had zero warnings and zero errors.
- Known gap: this confirms load, selection, complete page resolution, bounded frame generation, and visual absence of gross UV/page defects. It does not establish GPU-level blend parity.
