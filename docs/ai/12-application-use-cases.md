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

Renders a deterministic frame from explicit dimensions, time, transform, background, and PMA settings.

### `ExportAnimation`

Produces a sequence or encoded output with progress, cancellation, overwrite policy, and diagnostics.

## Error Model

Use structured error codes and diagnostics. Exceptions are internal failure signals, not the public adapter contract.
