# Quick-Browse UI Product Direction

## Status

Product direction selected; visual styling remains open.

## Primary Goal

Minimize the time and decisions between opening a Spine asset and seeing a useful animation preview.

```text
Open/drop asset
→ discover dependencies
→ detect Runtime
→ fit viewport
→ select remembered or first animation
→ preview
```

## Design Principles

1. **Viewport first** — viewport receives the largest area.
2. **Animation first** — animation search/list remains immediately accessible.
3. **Progressive disclosure** — skin, asset details, transform, PMA, background, Runtime override, and diagnostics are collapsible or on demand.
4. **Safe defaults** — valid assets should preview without configuration dialogs.
5. **Visible compatibility** — selected Runtime and warnings remain discoverable.
6. **Fast keyboard path** — Open, play/pause, animation navigation, fit, reload, screenshot, and diagnostics have commands.
7. **Deterministic states** — empty, loading, ready, warning, unsupported, failed, and renderer-unavailable are visually distinct.

## Proposed Main Window

```text
┌─────────────────────────────────────────────────────────────┐
│ Open | Recent | Reload | Screenshot | Export | Settings    │
├────────────────┬────────────────────────────────────────────┤
│ Animation find │                                            │
│ Animation list │                 Viewport                   │
│                │                                            │
│ Skins ▸        │                                            │
│ Asset info ▸   │                                            │
├────────────────┴────────────────────────────────────────────┤
│ Play | Stop | Timeline | Loop | Speed | Fit | Reset         │
├─────────────────────────────────────────────────────────────┤
│ Runtime | Load state | Warnings | Renderer state            │
└─────────────────────────────────────────────────────────────┘
```

## Initial Scope

- one active asset and viewport
- open, drag/drop, recent files, reload
- animation search and selection
- skin selection
- play/pause/stop/loop/speed/seek
- pan/zoom/fit/reset
- screenshot
- diagnostics summary
- export dialog as a secondary workflow

## Deferred

- multiple model scene
- tabs and docking plugins
- wallpaper mode
- embedded MCP controls
- advanced attachment editor
- final theme library decision

## Open Product Decisions

- default dark/light/system theme
- exact stop/reset semantics

ADR-004 resolves auto-play and remembered state: auto-play the remembered or first animation, remember animation and skin per asset, and fit instead of restoring viewport pan/zoom on open.
