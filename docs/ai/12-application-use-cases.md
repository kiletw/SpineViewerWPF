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

### `RenderInteractiveFrame`

Returns a Runtime-neutral bounded BGRA frame from a reusable render session. The CPU fallback may use bilinear texture sampling and bounds-aware viewport framing; it does not encode or write a PNG. WPF owns presentation-only RGBA/RGB/Alpha inspection.

### `RenderInteractiveScene`

Returns Runtime-neutral bounds, decoded RGBA textures, clipped vertices, UVs, triangle indices, tint, blend mode, and PMA intent from a reusable render session. WPF owns GPU resources and presentation transforms; Runtime-specific and OpenGL types do not cross this boundary.

### `ExportAnimation`

Produces a deterministic PNG sequence at an explicit bounded FPS with progress, cancellation, overwrite policy, Track 0 Alpha, PMA, slot visibility/opacity/attachment selection, and diagnostics. The frame loop reuses one loaded render session. Encoded GIF/video/PSD output remains deferred.

### `LoadViewerProject`, `SaveViewerProject`

Loads and saves project-owned viewer settings through a versioned `*.spineviewer.json` sidecar. Schema version 1 stores source references, selected animation and skin, model transform, playback settings, background mode, optional per-layer Track 0 Alpha/PMA fields, and slot visibility/opacity plus optional named attachment settings. Missing per-layer Alpha falls back to the prior top-level primary-layer value; a missing attachment setting retains animation/setup behavior. It never writes Spine JSON, binary, atlas, or texture sources.

## Error Model

Use structured error codes and diagnostics. Exceptions are internal failure signals, not the public adapter contract.
