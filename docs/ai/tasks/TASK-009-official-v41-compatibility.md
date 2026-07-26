# TASK-009: Official 4.1 Compatibility Smoke

## Status

Completed on 2026-07-27.

## Objective

Run the v3 4.1 adapter against official `spineboy` assets from the exact pinned Runtime commit, covering both JSON and binary skeleton input without committing third-party binaries to the repository.

## Context

- `../04-runtime-matrix.md`
- `../05-technical-constraints.md`
- `../decisions/ADR-002-runtime-isolation.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `TASK-008-png-render-preview.md`

The upstream snapshot-test suite is not present at the pinned 4.1 commit. The test therefore uses the official example export at that same commit, while preserving its license file in the local cache.

## Allowed Paths

- `scripts/test-official-v41.ps1`
- `docs/ai/**`
- gitignored `artifacts/official-v41/**`

## Forbidden Paths

- `runtimes/SpineRuntime.V41/src/**`
- Core, Application, WPF, CLI, and legacy production source
- committing official example binaries or adding packages

## Required Behavior

- fetch or reuse the pinned official assets through the GitHub Contents API
- verify the pinned commit, Git blob SHA-1, and SHA-256 for JSON, binary skeleton, atlas, PNG, and license files
- inspect both `spineboy-pro.json` and `spineboy-pro.skel` with the explicit atlas path
- render the `walk` animation from both formats through the existing CLI/CPU renderer
- support `-Offline` for repeatable validation from an existing cache

## Acceptance Criteria

- both skeleton formats report a successful 4.1 inspect result with 11 animations and the `default` skin
- both formats render a non-empty PNG with a valid PNG signature at 512 by 512 and 0.5 seconds
- the official license is retained beside the cached assets
- official Runtime source and all production layers remain unchanged
- no official binary is committed and no package is added

## Validation

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-official-v41.ps1 -Offline
git diff --check
```

## Completion Report

Changed: added `scripts/test-official-v41.ps1`. The script caches and hashes the official 4.1 `spineboy` JSON, `.skel`, atlas, PNG, and license from commit `ab28b77c70e3aa766be5bdb759d7aedac9fd0bde`, then runs inspect and render for both skeleton formats.

Verified: export version `4.1.23-beta`, 11 animations, `default` skin, and successful JSON/binary PNG renders. The cache is gitignored and can be exercised offline after the first download.

Preserved: production source, official Runtime source, PPM/PNG behavior, and deterministic renderer. The Application smoke and v3 regression pass; the existing WPF shell probe still cannot observe a non-empty window title in this headless session.

Known gaps: this is a compatibility smoke, not a visual golden comparison. PMA, clipping, non-normal blending, multi-page atlases, and bounds-aware fit remain separate renderer work.
