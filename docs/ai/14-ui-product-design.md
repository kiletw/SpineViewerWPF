# Dockable Quick-Browse UI Product Direction

## Status

Product direction accepted; TASK-029 validates a bounded WPF docking interaction, TASK-030 corrects its binding and production-chrome boundary, TASK-035 establishes the compact dark workspace baseline, TASK-037 adds the first built-in Light theme without a theme toolkit, TASK-043 makes scene-layer selection the single Properties editing context, TASK-044 adds named attachment selection to the Slots workflow, TASK-048 separates desktop commands from panel navigation, and TASK-052 refines the resulting workspace hierarchy.

TASK-053 treats foreground and background as one interactive-state contract.
Primary buttons use a theme-specific foreground on the accent surface, then
switch to the normal text brush when their hover or pressed surface changes.
Checks, switches, and disabled states reuse the same semantic Dark/Light
resources instead of fixed dark-theme colors.

## Primary Goal

Minimize the time and decisions between opening a Spine asset and seeing a useful live viewport, while allowing advanced tools to form a Photoshop-style workspace.

```text
Open/drop asset
-> discover dependencies
-> detect Runtime
-> fit viewport
-> select remembered or first animation
-> live viewport
```

## Design Principles

1. **Viewport first** - the viewport remains the largest and most stable surface.
2. **Fast default** - first launch uses the validated TASK-002 quick-browse arrangement.
3. **Dock when useful** - supporting panels may dock, tab, float, hide, and return to the default layout.
4. **Progressive disclosure** - advanced panels do not dominate the initial workspace.
5. **Safe defaults** - valid assets render in the viewport without configuration dialogs.
6. **Visible compatibility** - selected Runtime and warnings remain discoverable.
7. **Localization-ready** - user-facing text uses resource identities; runtime language switching may arrive later.
8. **Deterministic states** - empty, loading, ready, warning, unsupported, failed, and renderer-unavailable remain visually distinct.

## Default Workspace

```text
+------------------------------------------------------------------+
| File | Edit | View | Playback | Layer | Export | Window | Help   |
+------------------------------------------------------------------+
| Open | Save | Undo | Redo | Screenshot | Export                 |
+------------------+-----------------------------+-----------------+
| Activity/Layers  |                             | Animation       |
| visibility       |          Viewport           | Transform       |
| order/actions    |                             | Render/Look     |
| asset identity   |                             | Slots/Viewport  |
+------------------+-----------------------------+-----------------+
| Play | Stop | Timeline | Loop | Speed | Fit | Reset              |
+------------------------------------------------------------------+
| Runtime | Load state | Warnings | Renderer state                 |
+------------------------------------------------------------------+
```

Supporting panels may be moved into tab groups or separate owned windows. The menu bar owns discoverable commands, the small toolbar keeps only frequent actions, and the left activity rail switches Layers, Properties, and Diagnostics. The central viewport, playback controls, and status remain available in the default layout.

## Visual Density Baseline

TASK-035 keeps the existing information architecture while making the viewport visually dominant:

- compact command, playback, status, and panel chrome
- one mint accent over neutral dark surfaces
- dark native WPF inputs, sliders, checks, lists, and scroll tracks
- small corner radii for controls and viewport framing
- scrollable supporting panels rather than shrinking or hiding editor controls

The operating-system title bar remains native. Replacing it requires a separate window-chrome task because that change also owns resize, drag, accessibility, and system-button behavior.

TASK-052 applies a preserve-and-refine pass informed by current Photoshop,
Visual Studio, and Windows command-bar guidance:

- the activity rail remains in the main workspace while Layers content hides or
  floats, so panel recovery is not hidden with the panel
- the toolbar contains only frequent commands and no duplicate product branding
- toolbar, playback, and status chrome use less vertical space
- ordinary toolbar actions use low-noise surfaces; the primary Open action and
  selected panel states keep the single mint accent
- Layers uses one panel title and wraps compact actions instead of clipping a
  fixed horizontal row

The implementation remains native WPF. It does not add Fluent, docking, or icon
packages; the external products are interaction and hierarchy references only.

## Initial Product Scope

- multiple scene layers in one active Viewer project and viewport
- open, drag/drop, recent files, reload
- animation search and selection
- skin selection
- play/pause/stop/loop/speed/seek
- pan/zoom/fit/reset
- screenshot and PNG-sequence output that preserve visible scene-layer order, transforms, opacity, slot presentation, and transparent background
- diagnostics summary
- export dialog as a secondary workflow
- dock, float, redock, hide/show, and reset-layout behavior for selected supporting panels
- non-destructive Inspector editing with dirty state, Undo/Redo, and `*.spineviewer.json` project save
- visible selected-layer opacity, Track 0 Alpha, and PMA controls
- selected-layer slot visibility, opacity, and named attachment controls
- layer duplicate, reload, reorder, and scoped parameter copy/paste actions
- precise finite transform fields without the earlier slider range ceilings
- selected-layer Animation, Transform, Render, Appearance, Slots, and Viewport property categories
- explicit GPU/CPU backend status and recoverable atlas selection
- independently configurable 1-240 Preview FPS with a 30 FPS default
- low-rate published FPS, preview work time, and coalesced-update status

## Deferred

- wallpaper mode
- embedded MCP controls
- attachment transforms, creation, deletion, and source writing
- multi-track animation, Physics controls, and Runtime-neutral debug geometry
- expanded theme library beyond the built-in Dark/Light pair
- runtime language switcher and translated resource sets
- writing changes back to Spine JSON or binary source files

## Open Product Decisions

- which additional supporting panels ship after the TASK-029 Browse/Inspector prototype
- whether layout persistence is enabled in the first implementation
- default dark/light/system theme
- exact stop/reset semantics

ADR-004 still defines quick-browse behavior. ADR-006 amends the window model without changing the validated open-to-preview path.
