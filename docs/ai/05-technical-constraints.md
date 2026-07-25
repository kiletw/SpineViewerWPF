# Technical Constraints

## Upstream Runtime

- Official Spine Runtime source is read-only by default.
- Pin exact upstream commits.
- Keep local patches isolated, documented, and test-covered.
- Preserve Spine Runtime license notices and distribution requirements.

## Dependency Direction

```text
WPF / CLI / MCP
        ↓
Application
        ↓
Core contracts
        ↑
Runtime adapters / Renderer / Export infrastructure
```

Core and Application must not reference WPF, XNA, MonoGame, SFML, MCP, official Runtime types, or GPU resource types.

## Versioning

- v2 remains the behavior reference while v3 is developed.
- v3 may initially support fewer Runtime lines, but missing support must be explicit.
- Runtime selection is version-aware and test-backed.

## Resource and Performance

- All GPU and Runtime resources require clear ownership and disposal.
- Loading, rendering, and export must not block the WPF UI thread.
- Interactive preview and deterministic export use separate timing concerns.
- Avoid per-frame allocation where measurable.

## Scope Control

- No large framework or renderer choice without an ADR and prototype evidence.
- No MCP implementation before stable Application use cases exist.
- No broad formatting changes mixed with behavior changes.
- No new package without reason, maintenance status, license, and deployment impact.
