# Testing Strategy

## Test Layers

### Unit

Version parsing, registry selection, playback state transitions, validation rules, path discovery, and export configuration.

### Contract

Every Runtime adapter passes the same capability and lifecycle tests, with version-specific exceptions recorded.

### Compatibility

Use representative assets for each supported export line. Verify load result, animation/skin names, duration, warnings, and deterministic rendered baselines where legally distributable.

### Application

Exercise use cases with fake Runtime and Renderer implementations. Ensure WPF/CLI/MCP adapters do not contain duplicated rules.

### Rendering

Golden images require fixed dimensions, background, transform, animation time, Runtime commit, renderer version, DPI, and tolerance policy.

### UI

Use fake presentation state for empty, loading, ready, warning, unsupported, failed, renderer-unavailable, and exporting states. Include keyboard navigation and stable automation IDs.

## Fixture Policy

- Do not commit assets without redistribution rights.
- Maintain a manifest even when fixture files stay local.
- Record source, export version, format, atlas shape, textures, expected animations/skins, and legal status.

Current manifest: [`fixtures/manifest.json`](fixtures/manifest.json). All 14 v2 lines are currently marked missing; therefore build/startup is the only executable baseline and no compatibility or rendered-output claim is verified.

## Baseline Comparison

v2 and v3 should be compared on observable behavior, not internal class structure. Differences must be either fixed or documented as approved changes.
