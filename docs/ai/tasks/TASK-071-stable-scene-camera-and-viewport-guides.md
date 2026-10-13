# TASK-071: Stable scene camera and viewport guides

Status: Implemented on 2026-10-08. The product owner approved replacing the
per-frame preview fit with a stable camera on 2026-10-07.

## Objective

The preview, Screenshot, and fixed-size export share one stable scene space, so
the viewport can show trustworthy XY axes and the fixed-size export frame.

## Context

- Round 2 competitor review, item 2 (`17-external-feature-reference.md`).
- Before this task the GPU preview (`ViewportMath.ComputeFit`) and fixed-size
  export (Runtime default camera) re-fitted and re-centered poses larger than a
  quarter of the frame on every frame, so no stable world space existed; the CPU
  preview ignored zoom/pan; Screenshot used its own per-frame fit.
- `RenderCamera` (TASK-063) already lets every adapter render a fixed framing.
- The compositor maps a layer frame's center to canvas center + translation with
  clockwise rotation in y-down space, matching WPF and the GPU shader.

## Allowed Paths

- `src/SpineViewerWPF.Application/SceneCamera.cs` (new), `AssetService.cs`
- `src/SpineViewerWPF.Wpf/**`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `docs/ai/00-context-index.md`, `docs/ai/16-ui-state-and-interaction-contracts.md`,
  `docs/ai/17-external-feature-reference.md`, this task

## Forbidden Paths

- `runtimes/**`, renderers, the sidecar schema
- auto-fit export (TASK-063) framing
- new packages

## Required Behavior

- `SceneCamera`: scene space has its origin at the scene center, x right, y
  down, one output pixel (one DIP in the preview) per unit at zoom 1. A layer
  places its skeleton origin at (ModelX, ModelY), scales, flips, and rotates
  clockwise. `Place` returns a per-layer `RenderCamera` centered on the skeleton
  point that lands at the view center, with translation and scale folded in;
  only flips and rotation remain for the compositor or view. Rotations off the
  90-degree grid get a square frame covering the canvas diagonal.
- GPU preview draws scene space at ViewportZoom DIPs per unit: no per-frame fit,
  and layer offsets scale with zoom.
- CPU preview (channel inspection, fallback) and Screenshot render each layer
  through `Place` for the current zoom/pan, so they follow zoom/pan and
  Screenshot equals the CPU viewport. The CPU preview re-renders on viewport
  changes.
- Fit zooms (at most 100%, at least 1%) and pans so the visible layers' current
  content fits with 16 px padding; it runs once automatically after an asset or
  project opens. Layer focus uses the same math and no longer needs the GPU.
- Fixed-size export frames the scene origin at the canvas center 1:1 through
  `Place`: large poses are clipped, not shrunk, and layer offsets move content by
  exactly that many pixels. Fixed size is limited to 4096 px per side.
- View > Show Axes draws the scene X (red) and Y (green) axes and the selected
  layer's origin; View > Show Export Frame outlines the Fixed-size export area
  with the outside dimmed and a size label. Both are remembered in user
  settings, default on, and never written to Screenshot or export output. The
  frame is hidden for Auto fit.

## Acceptance Criteria

- Camera round trip matches scene positions for rotations, flips, and scales.
- Fixed-size export clips a large pose and shifts content by a layer offset.
- Opening a large asset fits the view; Fit restores it after zoom/pan.
- The CPU preview follows pan; Screenshot equals the CPU viewport pixels.
- Guide toggles persist and do not dirty the project.
- Running the application on the official spineboy example shows the fitted
  GPU view with axes through the skeleton origin, and the Alpha (CPU) view with
  the Fixed-size frame.

## Validation

```text
dotnet build SpineViewerWPF.sln -c Release
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v42.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v43.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

- Changed: `SceneCamera`; fixed-size export framing; GPU shader inputs; CPU
  preview, Screenshot, Fit, and focus through the camera; `ViewportGuides`
  overlay and View menu toggles; `ViewportCameraSmoke`; updated focus and
  setup-pose smoke expectations.
- Preserved: auto-fit export output; small-pose fixed-size output (the old fit
  already left poses within a quarter of the frame unscaled at the origin);
  sidecar format; GPU/CPU fallback behavior.
- Behavior change: fixed-size export and the preview no longer shrink or
  re-center large poses each frame. A fixed-size frame is centered on the scene
  origin, usually a character's feet, so layers are positioned to frame them.
- Validation (2026-10-08): all commands above passed; live captures confirmed
  the GPU and CPU views and the guides.
- Gaps: the auto-fit export frame is not drawn (it needs all-frame bounds); the
  CPU preview raster covers the viewport only for unrotated layers, so rotated
  layers may lose corners in channel inspection (Screenshot is not affected);
  auto Fit runs once per open, with the viewport size at that time.
