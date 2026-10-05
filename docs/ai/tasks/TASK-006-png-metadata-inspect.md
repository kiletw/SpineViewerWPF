# TASK-006: PNG Metadata Inspect

## Status

Completed on 2026-07-26 for 4.1 PNG metadata inspection.

## Objective

Allow the 4.1 inspect path and WPF metadata workflow to open atlas pages backed by standard PNG files without selecting or implementing the production preview renderer.

## Context

- `../04-runtime-matrix.md`
- `../05-technical-constraints.md`
- `TASK-001-v3-vertical-slice.md`
- `TASK-005-wpf-real-asset-open.md`
- `../decisions/ADR-002-runtime-isolation.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`

`runtimes/SpineRuntime.V41/Adapter.cs` is project-authored adapter code. Official pinned Runtime sources under `runtimes/SpineRuntime.V41/src/**` remain read-only.

## Allowed Paths

- `runtimes/SpineRuntime.V41/Adapter.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/SpineRuntime.V41/src/**`
- `runtimes/SpineRuntime.V41/CpuRenderer.cs`
- Core, Application, WPF, CLI, and legacy production code
- PNG pixel decoding or PNG rendering
- new packages

## Required Behavior

- inspect P3 PPM exactly as before
- inspect PNG atlas pages by validating the PNG signature and IHDR dimensions
- reject truncated, malformed, zero-sized, or unsupported texture metadata
- return canonical PNG texture paths through `InspectResult`
- keep deterministic PPM rendering unchanged

## Acceptance Criteria

- a project-authored 4.1 JSON/atlas asset with a valid PNG page reaches WPF Ready
- animation, skin, Runtime, duration, and PNG path remain correct
- malformed PNG metadata fails deterministically
- existing PPM inspect and render SHA-256 remain unchanged
- official Runtime source is untouched
- no package is added

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Completion Report

Changed: project-authored Adapter inspect loader now validates PNG signature, IHDR length/type, and positive big-endian dimensions while recording canonical texture paths.

Preserved: official Runtime `src/**`, Core/Application/WPF/CLI contracts, P3 PPM inspection, deterministic PPM rendering, and the recorded render hash.

Validation: valid generated PNG reaches WPF Ready with expected metadata; truncated and zero-width PNGs preserve deterministic validation failures; WPF and CLI regressions pass.

Known gaps at completion: PNG pixels were not decoded or rendered; CRC, later chunks, editor-export assets, multi-page atlases, and non-PNG production formats remained unverified. TASK-008 later adds bounded 8-bit non-interlaced PNG decoding and rendering.

Documentation: context index, Runtime matrix, and TASK-005 known gaps updated.
