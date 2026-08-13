# TASK-050: Add multi-layer capture and export parity

Status: Completed

## Objective

Make Screenshot and deterministic PNG-sequence export include the same ordered
scene layers and layer presentation transforms as the viewport.

## Context

- `docs/ai/03-behavior-contracts.md`
- `docs/ai/05-technical-constraints.md`
- `docs/ai/06-target-architecture.md`
- `docs/ai/14-ui-product-design.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `docs/ai/decisions/ADR-008-in-memory-interactive-preview.md`
- `docs/ai/decisions/ADR-009-opentk-gpu-preview.md`

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`
- new project-owned compositor and PNG writer files under
  `src/SpineViewerWPF.Application/`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `docs/ai/00-context-index.md`
- `docs/ai/12-application-use-cases.md`
- `docs/ai/14-ui-product-design.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- this task document

## Forbidden Paths

- `runtimes/**` and vendored Runtime source
- MainWindow XAML, new controls, new packages, GPU readback, or GUI automation
- source Spine JSON/binary writing
- viewport pan/zoom or background baking

## Required Behavior

- Visible layers are composited by ascending ZIndex with stable source-over
  ordering.
- ModelX/ModelY are 96-DPI output-pixel translations. Scale, rotation, flips,
  layer opacity, animation, skin, PMA, Track Alpha, and slot settings apply.
- Screenshot uses the current viewport pixel dimensions and applies
  PreviewChannel after final composition.
- PNG sequence uses ExportSize, raw RGBA, selected-layer timeline duration,
  existing frame naming, overwrite, progress, cancellation, and cleanup rules.
- Checkerboard, Dark, and Light backgrounds remain presentation-only; file
  output stays transparent.
- The default single visible identity layer keeps the existing Runtime PNG
  path and deterministic byte baselines.
- All-hidden scenes produce a transparent composite rather than exporting a
  stale hidden layer.
- Capture/export snapshot layer state before background work. Failures keep the
  active project loaded and surface through existing diagnostics/status paths.

## Acceptance Criteria

- Shared Application compositor is used by Screenshot and multi-layer export.
- No Runtime-specific or WPF types enter Core/Application contracts.
- A red/blue multi-page fixture proves both layers, translation, Z order,
  visibility, opacity, and repeated deterministic output.
- Existing single-layer deterministic hashes remain unchanged.
- No foreground window, mouse, or keyboard automation is used for validation.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore -p:BaseOutputPath=artifacts/task-050/wpf/
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE='1'; dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore -p:BaseOutputPath=artifacts/task-050/smoke/
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
git diff --check
```

## Completion Report

Changed:

- Added an Application-owned Runtime-neutral BGRA scene compositor and
  standard-library composite PNG writer.
- Screenshot now snapshots and renders every visible scene layer before
  applying the selected display channel to the final frame.
- PNG-sequence export now reuses one render session per visible layer and
  includes layer order, visibility, opacity, transform, flips, animation,
  skin, Track 0 Alpha, PMA, and slot presentation.
- Added deterministic red/blue, translation, Z-order, opacity, all-hidden,
  repeated-output, and screenshot/sequence pixel-parity smoke coverage.
- Capture now blocks session-destroying open/remove/reload operations until its
  borrowed layer sessions are released; the composite writer deletes a partial
  file only when it successfully created and owns that file.

Preserved:

- A default single visible identity layer still uses the previous Runtime PNG
  path and retains its recorded byte hash.
- Existing naming, overwrite, progress, cancellation, cleanup, selected-layer
  timeline, and raw-RGBA sequence contracts remain intact.
- Preview backgrounds and viewport pan/zoom remain presentation-only. Runtime
  source and vendored code are unchanged.

Validation:

- Isolated WPF Release build: passed, 0 warnings / 0 errors.
- Application smoke with `SPINEVIEWER_SKIP_WINDOW_SMOKE=1`: passed, including
  the new multi-layer and legacy-hash assertions.
- `scripts/test-v3.ps1`: passed for 4.1, 4.0, 4.2, PMA, and all 12 historical
  Runtime checks; build completed with 0 warnings / 0 errors.
- `git diff --check`: passed.
- No foreground window, mouse, keyboard, or process-control automation was
  used during TASK-050 validation.

Gaps:

- Background baking, viewport pan/zoom capture, and GPU framebuffer readback
  remain intentionally outside this file-output contract.
- The normal compact-window smoke remains available when the non-GUI
  environment switch is omitted; it was deliberately skipped in this run.

Risks:

- Composite capture is CPU-authoritative and uses bilinear sampling, so rare
  driver-specific GPU blend differences are not claimed to be pixel-identical.
- Model translation is deliberately fixed to 96-DPI output pixels; a future
  physical-DPI export mode would require a separate explicit contract.

Documentation:

- Updated the context index, Application use cases, UI product and interaction
  contracts, ADR-005, and this completion report.

Next task:

- Add Runtime 4.3 as a separately pinned official adapter, or begin the safe
  source-authoring contract with JSON Save Copy before any in-place writing.
