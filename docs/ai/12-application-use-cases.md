# Application Use Cases

## Design Rules

- Requests and results use project-owned DTOs.
- No WPF, Runtime-specific, renderer-specific, CLI, or MCP types.
- All long operations accept cancellation and report structured diagnostics.

## Initial Use Cases

### `ListSupportedRuntimes`

Returns registered Runtime descriptors and verified capability flags.

### `DetectSpineVersion`

Input: skeleton path or readable content. Output: detected version string, confidence, parsing method, diagnostics.

### `InspectSpineAsset`

Input: skeleton, optional atlas, optional Runtime override. Output: identity, selected Runtime, textures, animations, skins, duration metadata, diagnostics.

### `ValidateSpineAsset`

Checks files, version support, atlas/textures, parse result, and blocking/non-blocking diagnostics without opening UI.

### `OpenRenderSession`

Creates a disposable, serialized session owning the selected Runtime document, atlas, and decoded textures through Application abstractions. WPF keeps one session per scene layer; sequence export opens one session for the complete frame loop.

### `SelectAnimation`, `SelectSkins`, `ControlPlayback`, `SeekPlayback`

Modify a session through stable state transitions. Interactive preview cadence
is a WPF session preference bounded from 1 through 240 FPS, defaults to 30, and
remains independent from deterministic export FPS and Viewer project state.

### `RenderFrame`

Renders a deterministic frame from explicit dimensions, time, Track 0 Alpha, and PMA settings. `RenderScene` applies the same contract to a bounded list of independent scene layers, each with its own animation, skin, Track 0 Alpha, and PMA selection.

### `ComposeSceneFrame`

Composes already rendered Runtime-neutral BGRA layer frames into one transparent output frame. Visible layers use stable ascending Z order and straight-alpha source-over; layer translation, scale, rotation, flips, and opacity are applied around the canvas center. Translation values are 96-DPI output pixels. Runtime PMA, slot, attachment, and blend work is resolved before this presentation compositor and is not applied a second time.

### `RenderInteractiveFrame`

Returns a Runtime-neutral bounded BGRA frame from a reusable render session. The CPU fallback may use bilinear texture sampling and bounds-aware viewport framing; it does not encode or write a PNG. WPF owns presentation-only RGBA/RGB/Alpha inspection.

### `RenderInteractiveScene`

Returns Runtime-neutral bounds, decoded RGBA textures, clipped vertices, UVs, triangle indices, tint, blend mode, and PMA intent from a reusable render session. WPF owns GPU resources and presentation transforms; Runtime-specific and OpenGL types do not cross this boundary.

### `ExportAnimation`

Produces a deterministic PNG sequence at an explicit bounded FPS with progress, cancellation, overwrite policy, Track 0 Alpha, PMA, slot visibility/opacity/attachment selection, and diagnostics. A multi-layer request reuses one loaded render session per visible layer and writes one composed transparent frame per timeline sample. The default single visible identity layer retains the existing Runtime PNG path and byte baselines. An optional `ExportFraming` (TASK-063) switches to auto-fit: a bounds pass unions each visible layer's pose bounds over every exported frame time, then every frame renders with one fixed `RenderCamera` per layer on a canvas sized to that union at `Scale` pixels per skeleton unit plus `Margin` pixels per side (bounded to 4096 pixels per side and the composite buffer budget, reducing scale when needed); the result reports the chosen width and height. PSD output remains deferred.

### `ExportEncodedAnimation`

TASK-066 / ADR-010: `AssetService.ExportEncoded` renders the same frame request into a private temporary PNG sequence, encodes it with a user-installed FFmpeg into GIF (generated palette, transparency, infinite loop), animated WebP (`libwebp_anim`, alpha), APNG, or MP4 (`libx264`, `yuv420p`, composited over a `#RRGGBB` background and padded to even dimensions), and moves the result to the requested file. An existing output fails before rendering unless overwrite is explicit. Progress reports render plus encode frames; cancellation stops rendering or terminates the FFmpeg process tree. Temporary frames and partial outputs are always removed. `FfmpegLocator` resolves a remembered path when that file exists, otherwise `PATH`.

### `LoadViewerProject`, `SaveViewerProject`

Loads and saves project-owned viewer settings through a versioned `*.spineviewer.json` sidecar. Schema version 1 stores source references, selected animation and skin, model transform, playback settings, background mode (Checkerboard, Dark, Light, or Custom with a required `#RRGGBB` background color, TASK-075), optional per-layer Track 0 Alpha/PMA fields, and slot visibility/opacity plus optional named attachment settings. Missing per-layer Alpha falls back to the prior top-level primary-layer value; a missing attachment setting retains animation/setup behavior. It never writes Spine JSON, binary, atlas, or texture sources.

## Error Model

Use structured error codes and diagnostics. Exceptions are internal failure signals, not the public adapter contract.
