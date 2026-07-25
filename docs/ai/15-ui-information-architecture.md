# Dockable Workspace Information Architecture

## Primary Surface

The application is a viewport-first workspace. Its default arrangement preserves the TASK-002 quick-browse flow; supporting tools may dock into the main window or float as separate owned windows.

## Stable Regions

These stay in the main workspace:

- command bar
- central viewport
- playback bar
- compact status bar

## Dockable Panels

### Asset Browser

- opened assets
- recent assets
- reload and focus actions

### Animations and Skins

- animation filter and list
- skin selection
- later multi-track or grouped selection only through separate tasks

### Properties

- model transform
- PMA/render options
- background
- Runtime override

### Diagnostics

- compatibility warnings
- load and renderer details
- actionable error information

The initial implementation does not need every panel. A docking spike should start with two supporting panels and prove dock, float, redock, hide/show, and reset-layout behavior before broadening scope.

## Secondary Workflows

- Export remains a dialog until a real queued workflow requires a panel.
- Settings remains a dialog or drawer.
- Multiple-model scene, wallpaper, and attachment editing remain deferred.

## Default Layout Rules

- viewport remains visible and receives the largest area
- animations remain one interaction away
- supporting panels can share tab groups
- closing a panel hides it rather than destroying product state
- Window menu restores hidden panels and resets the default layout
- compact layouts may auto-collapse supporting panels

## Localization Boundary

- user-facing strings use stable resource keys
- state, command, and automation identities do not depend on translated text
- layout must tolerate longer translated labels
- actual language switching and translated resource sets are deferred

## Automation IDs

Existing TASK-002 identifiers remain stable. New panel chrome uses identifiers only after the docking prototype selects its controls.
