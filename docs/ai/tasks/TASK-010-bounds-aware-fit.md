# TASK-010: Bounds-Aware Static Fit

## Status

Completed on 2026-07-27.

## Objective

Keep the deterministic CPU preview centered and fully visible when an official skeleton pose is larger than the requested canvas, without changing the existing small synthetic fixture baseline.

## Context

- `../04-runtime-matrix.md`
- `../05-technical-constraints.md`
- `../decisions/ADR-002-runtime-isolation.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `TASK-009-official-v41-compatibility.md`

## Allowed Paths

- `runtimes/SpineRuntime.V41/CpuRenderer.cs`
- `scripts/test-official-v41.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/SpineRuntime.V41/src/**`
- Core, Application, WPF, CLI, and legacy production source
- interactive renderer selection or new packages

## Required Behavior

- compute the current pose AABB after animation application with the official Runtime API
- shrink only when the pose exceeds the requested width or height
- center the fitted AABB while preserving the previous origin transform for poses that already fit
- fall back to the previous transform when bounds are empty or non-finite
- keep deterministic output and verify official JSON/binary baselines

## Acceptance Criteria

- the existing 64 by 64 `move` fixture remains SHA-256 `7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E`
- official `spineboy` JSON and `.skel` renders remain valid 512 by 512 PNGs with recorded deterministic hashes
- the official preview is fully visible in the generated frame instead of being clipped by the canvas
- no official Runtime source, Core/Application/WPF/CLI source, or package changes

## Validation

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-official-v41.ps1 -Offline
git diff --check
```

## Completion Report

Changed: `CpuRenderer` now reads the current pose bounds from `Skeleton.GetBounds`, scales oversized poses down to the output canvas, and centers only those fitted poses. Small poses retain the previous coordinate mapping.

Verified: the project-authored 64 by 64 baseline is unchanged; official JSON and binary `spineboy` outputs now pass recorded visual baselines and show the complete character within 512 by 512. The render hashes are JSON `643A19AF580DBA2C79555B59D1EE4ECC73D9CFAECF5477C5FA94DC4BB7EE3731` and binary `6BB0635EFEACE5F2B99B8FF4031E4203D2276D9E2975F8955E8BC55D6695B406`.

Preserved: Runtime isolation, PNG/PPM decoding, CLI contracts, official source, and all existing data-editing behavior.

Known gaps: fit is static-export behavior only; interactive camera pan/zoom, margins, PMA, clipping, and non-normal blending remain separate work.
