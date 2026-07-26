# TASK-008: PNG Render Preview

## Status

Completed on 2026-07-26 for generated PNG texture variants.

## Objective

Allow the existing deterministic CPU renderer and WPF static-preview path to consume common standard PNG atlas textures without adding an image package or changing the official Runtime source.

## Context

- `../04-runtime-matrix.md`
- `../05-technical-constraints.md`
- `TASK-001-v3-vertical-slice.md`
- `TASK-006-png-metadata-inspect.md`
- `TASK-007-wpf-static-preview.md`
- `../decisions/ADR-002-runtime-isolation.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`

## Allowed Paths

- `runtimes/SpineRuntime.V41/Adapter.cs`
- `runtimes/SpineRuntime.V41/CpuRenderer.cs`
- `runtimes/SpineRuntime.V41/PngReader.cs`
- `runtimes/SpineRuntime.V41/SpineRuntime.V41.csproj`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/SpineRuntime.V41/src/**`
- Core, Application, WPF, CLI, and legacy production code
- GPU or production-renderer selection
- new packages

## Required Behavior

- decode 8-bit, non-interlaced grayscale, RGB, indexed, grayscale-alpha, and RGBA PNG textures
- validate signature, chunk lengths/order, CRC, dimensions, compression/filter/interlace modes, palette indexes, decompressed length, and scanline filters
- support multiple IDAT chunks and optional palette transparency
- reject unsupported bit depths, interlacing, malformed chunks, oversized images, and invalid pixel streams deterministically
- keep P3 PPM loading and its recorded render output unchanged
- let the existing WPF static-preview workflow reach `Ready` for the generated PNG fixture

## Acceptance Criteria

- the generated valid RGBA PNG fixture renders through `AssetService`
- WPF loads the rendered preview and retains animation, skin, Runtime, edit, and save behavior
- corrupted PNG data fails without crashing or replacing the prior asset
- PPM render SHA-256 remains `7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E`
- official Runtime `src/**`, Core, Application, WPF, and CLI source remain unchanged
- no package is added

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release -p:BaseOutputPath=.\artifacts\application-smoke\bin\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Completion Report

Changed: the project-owned 4.1 render texture loader now dispatches PNG files to a standard-library reader that validates chunks and reconstructs 8-bit non-interlaced grayscale, RGB, indexed, grayscale-alpha, and RGBA scanlines before the existing CPU rasterizer consumes them.

Preserved: official Runtime `src/**`, Core/Application/WPF/CLI source, P3 PPM loading, deterministic PPM output, project editing/save behavior, and renderer-unavailable fallback for unsupported assets.

Validation: seven generated PNG variants cover every supported color type, palette transparency, filters 0 through 4, equivalent filtered pixels, and split IDAT chunks; direct Application and WPF render paths produce matching output; corrupt CRC, truncated header, and zero dimensions fail deterministically; all regressions pass with zero warnings or errors.

Known gaps: only 8-bit non-interlaced PNG is supported. The reader limits dimensions to 8192 and decoded/encoded working data to 128 MiB; TASK-009 now covers the official 4.1 JSON/binary `spineboy` example, while multi-page atlases, PMA, clipping, and non-normal blending remain unverified.

Risks: decoding allocates compressed, reconstructed, and pixel buffers concurrently. Interactive rendering still needs a production renderer decision.

Documentation: ADR-005, context index, Runtime matrix, migration plan, UI state contract, and earlier task known gaps updated.

Recommended next task: add bounds-aware fit for the static preview before interactive playback.
