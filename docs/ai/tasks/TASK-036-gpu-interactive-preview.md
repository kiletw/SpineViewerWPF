# TASK-036: GPU Interactive Preview

## Status

Completed on 2026-07-31.

## Objective

Replace per-frame CPU pixel rasterization in the normal interactive WPF viewport with a GPU-backed OpenGL preview while preserving deterministic CPU capture/export and an explicit fallback.

## Context

- `docs/ai/05-technical-constraints.md`
- `docs/ai/06-target-architecture.md`
- `docs/ai/decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `docs/ai/decisions/ADR-008-in-memory-interactive-preview.md`
- `docs/ai/decisions/ADR-009-opentk-gpu-preview.md`
- TASK-033 removed PNG/disk work from playback but retained full CPU rasterization at up to 1536 by 1024.
- Playback is currently capped near 30 Hz and coalesces frames when CPU rendering exceeds its frame budget.

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`
- `src/SpineViewerWPF.Wpf/**`
- project-owned adapter/renderer files directly under `runtimes/SpineRuntime.*`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- vendored Runtime source under `runtimes/**/src/**` or `SpineViewerWPF/SpineLibrary/**`
- deterministic CLI/file-render output or sequence-export behavior
- project sidecar schema
- Runtime-specific or OpenGL types in Core/Application contracts
- removal of the CPU renderer
- unrelated UI redesign

## Required Behavior

- Supported hardware uses GPU triangle rasterization for the normal WPF interactive preview.
- Runtime adapters return Runtime-neutral textures, vertices, indices, tint, clipping output, blend mode, and PMA intent.
- WPF alone owns OpenGL context, shaders, buffers, texture uploads, draw timing, resize, and disposal.
- Visible scene layers retain z-order, transforms, opacity, skin, animation, PMA, and Track 0 Alpha behavior.
- GPU initialization or rendering failure leaves a usable CPU preview with an actionable diagnostic.
- Screenshot and deterministic export continue using the existing CPU render path.
- Playback timing is independent of export FPS and targets display-smooth updates without queuing stale frames.

## Dependency Decision

- `OpenTK.GLWpfControl` `4.3.6`
- direct dependency only in the WPF project
- transitive OpenTK range: `>= 4.9.4` and `< 5.0.0`
- license: MIT
- maintained by the OpenTK team; release `4.3.6` published 2026-02-28
- deployment adds managed OpenTK assemblies and its Windows OpenGL/DirectX interop implementation

## Acceptance Criteria

- Normal RGBA playback does not call CPU `RenderFrame` per playback tick when GPU initialization succeeds.
- The user-supplied 4.1.14 `xiu` asset renders and animates through the GPU viewport.
- CPU fallback remains functional and visibly identified.
- Application smoke verifies Runtime-neutral GPU scene data and CPU-source isolation.
- WPF UI automation retains the existing viewport identity and validates the GPU surface identity.
- Release build and existing deterministic Runtime suites remain green.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v38.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v41.ps1 -Offline
git diff --check
```

## Completion Report

- Added a WPF-only `OpenTK.GLWpfControl` 4.3.6 renderer with GLSL textured triangles, texture reuse, draw-order layering, transform/opacity, Spine blend modes, PMA, resize, and deterministic disposal.
- Runtime adapters expose project-owned scene DTOs and reuse the existing loaded session, pose, clipping, atlas, and decoded texture data.
- Normal RGBA playback publishes scene geometry at a 16-millisecond target; RGB/Alpha inspection, screenshot, deterministic CLI render, and sequence export retain the CPU path.
- GPU startup, draw, OpenGL error, or empty first framebuffer failure switches to an identified CPU fallback and keeps the asset usable.
- The user-supplied 4.1.14 `D:\Spine測試\spine4.1.0样本\xiu.skel` loaded as Runtime 4.1, advanced playback, reported GPU, and produced non-transparent pixels in the OpenGL framebuffer. Windows Graphics Capture did not expose the interop surface pixels and showed a gray placeholder, so framebuffer validation is the recorded visual evidence.
- Application smoke verifies complete Runtime-neutral texture/vertex/index batches and proves GPU playback does not replace the CPU preview frame.
- Validation passed: Release WPF build with zero warnings/errors; Application smoke; 51-ID WPF UI automation; v3 inspect/render/PMA/history suite with unchanged `7178BBF...301E` baseline; official 3.8 JSON/binary/PMA offline suite; official 4.1 JSON/binary offline suite; `git diff --check`.
- Preserved behavior: source files remain read-only; sidecar schema, CPU screenshot/export, export FPS, channel inspection, multi-layer settings, deterministic hashes, and unsupported-version policy are unchanged.
- Remaining risk: OpenGL/DirectX interop remains driver-dependent; the explicit CPU fallback is the supported recovery path.
