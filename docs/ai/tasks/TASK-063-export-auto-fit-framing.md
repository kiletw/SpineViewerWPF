# TASK-063: Export auto-fit framing

Status: Implemented; automated validation and the `run` auto-fit export check passed on 2026-10-02; remaining manual variants pending.

## Objective

Roadmap item 1 from `17-external-feature-reference.md`: PNG sequence export was
fixed at 512 x 512, and the renderer re-fit oversized poses per frame, so
exported frames could rescale and recenter over time. Add an auto-fit mode that
sizes the output to the animated content, with scale and margin, and keep a
fixed-size mode.

## Scope

Continues on `codex/ui-workspace-refinement`; preserve TASK-058 through TASK-062
working-tree changes. No commit, push, or branch switch.

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs` (additive optional fields and records)
- `src/SpineViewerWPF.Application/AssetService.cs`
- `runtimes/SpineRuntime.V41/CpuRenderer.cs` (project-owned shared renderer)
- project-owned `runtimes/SpineRuntime.{Legacy,V40,V41,V42,V43}/Adapter.cs`
  (pass the camera through only)
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`, `MainWindow.xaml`, `App.xaml`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `docs/ai/00-context-index.md`, `12-application-use-cases.md`,
  `16-ui-state-and-interaction-contracts.md`, `17-external-feature-reference.md`,
  this task

## Forbidden Paths

- Official Runtime sources `runtimes/**/src/**`
- CLI commands, GPU viewport, interactive preview framing, sidecar schema
- GIF/video/PSD encoding (roadmap item 5)

## Contract Changes (additive)

- `RenderCamera(CenterX, CenterY, Scale)`: skeleton-space point at the frame
  center and output pixels per skeleton unit.
- `FrameRenderRequest.Camera` (optional): when set, `CpuRenderer` uses it
  instead of the per-frame bounds fit. Null keeps existing behavior, so preview
  and existing exports are unchanged.
- `ExportFraming(Scale = 1, Margin = 0)` and `AnimationExportRequest.Framing`
  (optional). Scale 0.01-16, Margin 0-1024.
- `AnimationExportResult.Width/Height` report the written size (0 when unset by
  older callers).

## Behavior

- Framing null: unchanged paths, including the byte-stable single-layer export.
- Framing set (single or multi-layer):
  1. Bounds pass: `RenderScene` pose bounds per visible layer at every exported
     frame time, unioned per layer.
  2. Layout: each layer's rectangle is scaled by `Scale x ModelScale`, rotated,
     offset by `ModelX/ModelY x Scale`; the union plus `Margin` on each side sets
     the canvas. The union is re-centered on the canvas. If the canvas would
     exceed 4096 px per side or the 256 MiB composite budget, scale is reduced.
  3. Render pass: every frame renders each layer with its fixed camera, then the
     existing compositor applies rotation, flips, opacity, and Z order.
  - Layers without bounds are skipped; if no layer has bounds, frames are
    transparent at the request's width and height.
- WPF: Animation tab > Export (collapsed) holds Export FPS, Size
  (`Auto fit` default / `Fixed size`), Scale and Margin (auto) or Width and
  Height (fixed). Settings are session preferences: no dirty state, no Undo.
  After export the actual size is shown under the settings.

## Validation

```powershell
dotnet build SpineViewerWPF.sln -c Release --no-restore
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v41.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

The full solution build is required because the shared `CpuRenderer.cs` and
`Legacy/Adapter.cs` compile into every Runtime project. Existing baselines must
not change: camera is null on every pre-existing call.

Smoke additions: auto-fit reports one size used by every frame, keeps a
transparent border, renders visible content, doubles content size at scale 2,
rejects invalid framing before creating the output directory, and fixed-size
export reports its requested size.

Manual: export spineboy `run` and `jump` with Auto fit; frames must not jitter or
rescale, the character must never be clipped, and margin must be visible. Try
Scale 0.5 and 2, a two-layer scene with offsets/rotation, and Fixed size 512.

## Validation Results (2026-10-02, Windows, local)

- `dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false`: 0 warnings, 0 errors.
- Application.Smoke (`SPINEVIEWER_SKIP_WINDOW_SMOKE=1`): passed, including the
  auto-fit size, margin, scale, invalid-framing, and fixed-size assertions.
- `scripts/test-v3.ps1`: passed; the single-layer render SHA-256 baseline is unchanged.
- `scripts/test-official-v41.ps1 -Offline`: passed.
- `scripts/test-ui-shell.ps1`: passed.
- `git diff --check`: passed.
- Manual export (2026-10-02, spineboy `run`, Auto fit, scale 1, margin 16,
  30 FPS): 21 frames, all 665 x 714; the smallest transparent margin across
  frames is 17 px and the content union is centered (left/right 20 px), so
  no frame is clipped or rescaled. The UI reported `665 x 714`. Passed.
- Not run: `jump`, Scale 0.5 and 2, multi-layer offsets/rotation, Fixed size 512.
- Observed (pre-existing since `f05f3b9`): Screenshot and Export stay
  disabled after an export completes, because `ExportSequenceAsync` calls
  `RefreshCommands()` before resetting `exportInProgress`.

## Known Gaps

- Bounds come from Runtime `GetBounds` (attachment geometry); effects that draw
  outside attachment bounds are not measured.
- CLI `render` has no framing options yet.
