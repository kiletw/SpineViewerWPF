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

### `OpenAssetSession`

Creates a disposable session owning the Runtime document and renderer-facing resources through abstractions.

### `SelectAnimation`, `SelectSkins`, `ControlPlayback`, `SeekPlayback`

Modify a session through stable state transitions.

### `RenderFrame`

Renders a deterministic frame from explicit dimensions, time, transform, background, and PMA settings. `RenderScene` applies the same contract to a bounded list of independent scene layers, each with its own animation and skin selection.

### `ExportAnimation`

Produces a deterministic PNG sequence at an explicit bounded FPS with progress, cancellation, overwrite policy, and diagnostics. Encoded GIF/video/PSD output remains deferred.

### `LoadViewerProject`, `SaveViewerProject`

Loads and saves project-owned viewer settings through a versioned `*.spineviewer.json` sidecar. The first schema stores source references, selected animation and skin, model transform, playback settings, and background mode. It never writes Spine JSON, binary, atlas, or texture sources.

## Error Model

Use structured error codes and diagnostics. Exceptions are internal failure signals, not the public adapter contract.
