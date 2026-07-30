# Dockable Quick-Browse UI Product Direction

## Status

Product direction accepted; TASK-029 validates a bounded WPF docking interaction and TASK-030 corrects its binding and production-chrome boundary. Toolkit selection and final visual styling remain open.

## Primary Goal

Minimize the time and decisions between opening a Spine asset and seeing a useful animation preview, while allowing advanced tools to form a Photoshop-style workspace.

```text
Open/drop asset
-> discover dependencies
-> detect Runtime
-> fit viewport
-> select remembered or first animation
-> preview
```

## Design Principles

1. **Viewport first** - the viewport remains the largest and most stable surface.
2. **Fast default** - first launch uses the validated TASK-002 quick-browse arrangement.
3. **Dock when useful** - supporting panels may dock, tab, float, hide, and return to the default layout.
4. **Progressive disclosure** - advanced panels do not dominate the initial workspace.
5. **Safe defaults** - valid assets preview without configuration dialogs.
6. **Visible compatibility** - selected Runtime and warnings remain discoverable.
7. **Localization-ready** - user-facing text uses resource identities; runtime language switching may arrive later.
8. **Deterministic states** - empty, loading, ready, warning, unsupported, failed, and renderer-unavailable remain visually distinct.

## Default Workspace

```text
+------------------------------------------------------------------+
| Open | Recent | Reload | Screenshot | Export | Window | Settings |
+------------------+-----------------------------------------------+
| Assets           |                                               |
| Animations       |                 Viewport                      |
| Skins            |                                               |
+------------------+-----------------------------------------------+
| Play | Stop | Timeline | Loop | Speed | Fit | Reset              |
+------------------------------------------------------------------+
| Runtime | Load state | Warnings | Renderer state                 |
+------------------------------------------------------------------+
```

Supporting panels may be moved into tab groups or separate owned windows. The command bar, central viewport, playback controls, and status remain available in the default layout.

## Initial Product Scope

- one active asset and viewport
- open, drag/drop, recent files, reload
- animation search and selection
- skin selection
- play/pause/stop/loop/speed/seek
- pan/zoom/fit/reset
- screenshot
- diagnostics summary
- export dialog as a secondary workflow
- dock, float, redock, hide/show, and reset-layout behavior for selected supporting panels
- non-destructive Inspector editing with dirty state, Undo/Redo, and `*.spineviewer.json` project save

## Deferred

- multiple-model scene
- wallpaper mode
- embedded MCP controls
- advanced attachment editor
- final theme library decision
- runtime language switcher and translated resource sets
- writing changes back to Spine JSON or binary source files

## Open Product Decisions

- which additional supporting panels ship after the TASK-029 Browse/Inspector prototype
- whether layout persistence is enabled in the first implementation
- default dark/light/system theme
- exact stop/reset semantics

ADR-004 still defines quick-browse behavior. ADR-006 amends the window model without changing the validated open-to-preview path.
