# Dockable Workspace Information Architecture

## Primary Surface

The application is a viewport-first workspace. Its default arrangement preserves the TASK-002 quick-browse flow; supporting tools may dock into the main window or float as separate owned windows.

## Stable Regions

These stay in the main workspace:

- conventional command menu
- frequent-action toolbar
- permanently reachable left panel activity rail
- central viewport
- playback bar
- compact status bar

## Command and Activity Separation

- File, Edit, View, Playback, Layer, Export, Window, and Help menus own discoverable commands and shortcuts.
- The toolbar is limited to frequent Open, Save, Undo, Redo, Screenshot, and Export actions.
- The activity rail switches or reveals Layers, Properties, and Diagnostics; it does not become a second command menu or leave the main window when Layers floats.
- Window remains the recovery surface for hidden, floating, and redocked panels.

## Dockable Panels

### Browse / Layers

- ordered scene layers with visibility and Runtime identity
- add, duplicate, reload, remove, reorder, and auto-layout actions
- scoped copy/paste for all, transform, render, or appearance settings
- current asset identity

### Properties

- selected-layer Animation with search, playback speed, loop, preview FPS, and export FPS
- precise Transform values and flips
- Render visibility, layer opacity, Track 0 Alpha, and PMA options
- Appearance skin selection
- searchable selected-layer slot visibility, opacity, and named attachment selection
- Viewport display channel and current physical render dimensions
- active GPU/CPU preview backend
- background
- theme and Viewer project identity
- multi-track animation and attachment authoring remain separate tasks

### Diagnostics

- compatibility warnings
- load and renderer details
- actionable error information

TASK-029 proves dock, float, redock, hide/show, and reset-layout behavior for the Browse and Inspector panels. TASK-030 keeps both panels data-bound while floating and removes prototype-only state controls from the normal shell. TASK-033 adds RGBA/RGB/Alpha viewport inspection. TASK-035 compacts the stable shell regions. TASK-037 adds slot controls and a theme selector. TASK-043 removes the stacked selected-layer editor from Browse, gives Layers a contextual workflow, and groups all selected-layer editing in Properties. TASK-044 adds the attachment selector within the existing Slots category. Additional panels can follow without changing the viewport-first shell contract.

## Secondary Workflows

- Export remains a dialog until a real queued workflow requires a panel.
- Settings remains a dialog or drawer.
- Wallpaper, attachment transforms/authoring, multi-track animation, debug geometry, and additional export formats remain deferred.

## Default Layout Rules

- viewport remains visible and receives the largest area
- the activity rail remains available when Layers is hidden or floating
- animations remain one interaction away
- supporting panels can share tab groups
- closing a panel hides it rather than destroying product state
- Window menu restores hidden panels and resets the default layout
- compact layouts may auto-collapse supporting panels
- supporting panels scroll vertically when their editor controls exceed available height

## Localization Boundary

- user-facing strings use stable resource keys
- state, command, and automation identities do not depend on translated text
- layout must tolerate longer translated labels
- actual language switching and translated resource sets are deferred

## Automation IDs

Existing TASK-002 identifiers remain stable. TASK-029 adds stable Window-menu identifiers for panel operations; new panel chrome should follow the same rule.
