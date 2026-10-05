# ADR-009: Use OpenTK GLWpfControl for the WPF GPU Preview

## Status

Accepted on 2026-07-31 after TASK-036 hardware, fallback, user-asset framebuffer, UI, and deterministic-regression validation.

## Context

The in-memory CPU preview established by ADR-008 removed PNG encoding and disk I/O, but it still rasterizes every covered pixel in managed code and allocates full frame buffers. Complex assets and multiple layers can exceed the 33-millisecond playback budget.

The legacy v2 XNA host is tied to .NET Framework, x86 XNA assemblies, incomplete resource disposal, and duplicated Runtime-specific Players. Restoring it would violate the v3 dependency and ownership direction.

## Decision

Use `OpenTK.GLWpfControl` `4.3.6` as a WPF-only dependency for the interactive preview.

- Runtime adapters produce Runtime-neutral textured triangle batches.
- Core/Application do not reference OpenTK, OpenGL, DirectX, WPF, or GPU resource types.
- WPF owns the OpenGL context, shaders, buffers, texture cache, draw loop, resize, failure handling, and disposal.
- Normal interactive playback uses GPU rasterization when initialization succeeds.
- CPU rendering remains the fallback and the source of deterministic capture/export.
- GPU failure must not unload the Runtime session or make the asset unusable.
- The first visible GPU scene performs a one-time framebuffer alpha readback; an empty result is treated as a renderer failure and activates the CPU fallback.

The selected package is MIT-licensed, maintained by the OpenTK team, supports .NET Core and later WPF applications, and uses OpenGL/DirectX interop so WPF controls can remain layered over the viewport.

## Rejected Alternatives

- Legacy `WpfXnaControl 1.0`: .NET Framework/XNA x86 boundary and obsolete resource model.
- MonoGame WPF interop: older third-party WPF host and a larger XNA-style framework than this renderer needs.
- Custom Direct3D11/D3D9 `D3DImage` bridge: substantially more interop and device-recovery code before rendering one Spine triangle.
- GPU-looking WPF bitmap composition: still performs CPU rasterization and does not address the reported bottleneck.

## Consequences

- WPF gains one maintained GPU/UI dependency and its transitive OpenTK assemblies.
- Interactive rendering is no longer byte-deterministic; CLI and export remain deterministic on the CPU path.
- GPU/driver compatibility requires explicit runtime fallback and hardware QA.
- The GPU framebuffer is premultiplied. Straight-alpha texture input is
  premultiplied in the fragment shader; PMA input is not multiplied twice.
  Color and alpha factors are set independently, including Screen RGB
  `(One, OneMinusSrcColor)` and source-over alpha (TASK-042).
- Texture and buffer lifetime becomes a WPF renderer responsibility and must be released on asset replacement and window shutdown.
- Windows Graphics Capture may show the OpenGL/DirectX interop surface as a gray placeholder; validation therefore uses the renderer's framebuffer rather than desktop capture pixels.
