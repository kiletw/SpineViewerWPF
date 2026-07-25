# Quick-Browse UI Information Architecture

## Primary Surface

The main window is a single quick-browse workspace, not a persistent export or diagnostics workbench.

## Regions

### Command Bar

- Open asset
- Recent assets
- Reload
- Screenshot
- Export
- Settings
- Diagnostics

### Browse Rail

Always prioritize animation discovery:

- animation filter
- animation list
- collapsible skins
- collapsible asset information

### Viewport

- rendered asset
- loading/error/unsupported overlays
- pan, zoom, fit
- optional checkerboard, color, or image background

### Playback Bar

- play/pause
- stop/reset
- timeline
- current time/duration
- loop
- speed
- fit/reset access where appropriate

### Status Bar

- detected/selected Runtime
- load state
- warning/error count
- renderer/device state
- export progress only while exporting

### Settings Drawer

Opened only when needed:

- model transform
- PMA/render options
- background
- Runtime override
- remembered-state policy

### Export Dialog

Separate from browsing. Contains dimensions, animation, timing, format, output path, overwrite policy, progress, and cancellation.

## Navigation Priority

1. Open/drop/recent
2. Select animation
3. Playback and viewport
4. Skin
5. Screenshot/export
6. Diagnostics and advanced settings

## Responsive Behavior

At minimum supported width:

- viewport remains visible
- browse rail can collapse
- status condenses but keeps Runtime and warning state
- advanced settings remain closed
- export stays a dialog

TASK-002 measured the prototype at 756 by 519 pixels: the rail collapses to 54 pixels, the viewport remains primary, and fake preview content scales without clipping.

## Automation IDs

```text
Main.Command.OpenAsset
Main.Command.Reload
Main.Command.Export
Main.Asset.AnimationSearch
Main.Asset.AnimationList
Main.Asset.SkinList
Main.Viewport.Surface
Main.Playback.Toggle
Main.Playback.Stop
Main.Playback.Timeline
Main.Playback.Loop
Main.Viewport.Fit
Main.Status.Runtime
Main.Status.Diagnostics
```
