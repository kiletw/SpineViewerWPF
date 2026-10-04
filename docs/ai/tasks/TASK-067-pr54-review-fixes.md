# TASK-067: PR #54 review fixes

Status: Completed on 2026-10-05.

## Objective

Fix four defects reported by the 2026-10-05 external review of
kiletw/SpineViewerWPF#54, each verified against the code before changing it.

## Scope

`codex/ui-workspace-refinement` (PR #54). The fixes are then merged into the
stacked branches of PR #55 and PR #56.

## Allowed Paths

- `src/SpineViewerWPF.Application/AssetService.cs`, `SceneFrameCompositor.cs`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs` (export request), `GpuViewport.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- this task, `docs/ai/00-context-index.md`

## Forbidden Paths

- Runtime adapters, `runtimes/**`, Core contracts, sidecar schema, CLI

## Findings and Fixes

1. Single-layer export after removing the opening layer used the workspace's
   original skeleton, atlas, and Runtime with the remaining layer's animation.
   The single-layer request now takes skeleton, atlas, Runtime, animation, and
   duration from the remaining layer.
2. Auto-fit export clipped rotated layers: every layer was rendered unrotated
   into a canvas sized for the rotated union. Each layer now renders into its
   own frame sized to its unrotated scaled content (plus a one-pixel guard);
   `SceneFrameCompositor` accepts layer frames of any size up to 4096 and maps
   each around its own center (same-size frames keep the previous mapping).
   The layout budget includes the per-layer frames, and canvas rounding ignores
   floating-point noise so a 90-degree rotation does not gain an off-center
   pixel.
3. GPU layer opacity multiplied every draw command, so overlapping slots got
   more alpha than the CPU capture/export (which fades the composed layer). A
   layer with opacity below 1 is now drawn into an offscreen target at full
   opacity and composited once with its opacity; opaque layers keep the direct
   path.
4. The GPU texture cache was keyed by texture path, so a reloaded layer kept the
   old pixels. The cache is now keyed by the texture instance each render
   session creates; pruning releases unused instances.

## Validation (2026-10-05, Windows, local)

- `dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false`: 0 warnings, 0 errors.
- Application.Smoke: passed. New checks: a wide (96 x 8) layer rotated 90
  degrees keeps its total alpha within 5% of the unrotated export (26540 of
  195840 without the fix); after removing the opening layer, a single-layer
  export renders the remaining wide asset (56 x 48, the original asset, without
  the fix). Both new checks were run against the unfixed code and failed.
- `scripts/test-v3.ps1`: passed with the unchanged single-layer SHA-256.
- `scripts/test-ui-shell.ps1`: passed (85 AutomationIds on this branch).
- `git diff --check`: passed.
- Manual (spineboy 4.1, GPU, captured with `PrintWindow`): at layer opacity 0.5
  the character is uniformly translucent with no darker overlaps; after
  inverting the texture file and invoking Reload layer, the preview shows the
  inverted colors.

## Known Gaps

- The GPU opacity and texture-cache fixes have no automated test (they need an
  OpenGL context); they were verified visually.
- The offscreen layer target applies within-layer blend modes against the
  layer's own transparent target, matching the CPU export rather than the
  previous GPU result for translucent layers.
