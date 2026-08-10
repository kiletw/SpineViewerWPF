# TASK-042: GPU screen blend parity

## Status

Completed on 2026-08-09 after visual comparison and GPU hardware validation.

## Objective

Correct the WPF GPU compositing path so both straight-alpha and PMA texture
inputs produce premultiplied framebuffer colors and correct Screen blending,
while preserving deterministic CPU rendering.

## Context

- `../05-technical-constraints.md`
- `../decisions/ADR-009-opentk-gpu-preview.md`
- `TASK-027-clipping-blend-render-support.md`
- `TASK-034-user-asset-compatibility.md`
- User-supplied Spine 3.5.51 asset `116421.json` (external, read-only)

## Allowed Paths

- `src/SpineViewerWPF.Wpf/GpuViewport.cs`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `docs/ai/**`

## Forbidden Paths

- vendored Runtime source
- Core or Application contracts
- deterministic CPU renderer behavior
- user-supplied source assets
- new dependencies or unrelated UI changes

## Required Behavior

- Straight-alpha texture input is premultiplied by the shader before blending.
- PMA texture input is not premultiplied a second time.
- GPU Normal, Additive, Multiply, and Screen use PMA color factors, with
  independent alpha factors matching source-over behavior.
- The external 3.5.51 diagnostic asset is not copied into tracked fixtures.

## Acceptance Criteria

- Application smoke locks straight-alpha shader premultiplication and the PMA
  Screen color factors.
- WPF Release build and Application smoke pass.
- The connected historical Runtime suite remains green.
- Diagnostic evidence records the external reproduction without storing the
  user's asset in the repository.

## Validation

```powershell
dotnet build .\src\SpineViewerWPF.Wpf\SpineViewerWPF.Wpf.csproj -c Release
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Completion Report

### Changed

- Converted straight-alpha GPU texture samples to premultiplied shader output;
  PMA inputs retain their existing one-time premultiplication.
- Switched GPU blend setup to PMA color factors and independent alpha factors,
  including the correct Screen equation.
- Added focused Application smoke assertions for shader premultiplication and
  Screen PMA factors.
- Recorded the external 3.5.51 reproduction without tracking the user asset.

### Preserved

- Runtime adapters, vendored source, Core/Application contracts, and CPU output
  are unchanged.
- The user source files were read-only and were not modified.

### Validation

- WPF Release build: passed with 0 warnings and 0 errors.
- Application smoke: passed.
- Release WPF hardware run loaded the external asset as Runtime 3.5.51, reported
  GPU rendering, and no longer showed the dark translucent band.
- `scripts/test-v3.ps1`: passed, including all 12 historical lines and 4.2.
- `git diff --check`: passed apart from existing line-ending notices.

### Remaining Risk

- The real-asset GPU validation covers the current machine and driver, not a
  cross-driver pixel baseline. Other clipping and non-normal blend combinations
  still require feature-isolated GPU parity coverage.
