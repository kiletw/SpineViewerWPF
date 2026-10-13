# UI State and Interaction Contracts

## Workspace States

```text
Empty
Loading
Ready
ReadyWithWarnings
Unsupported
Failed
RendererUnavailable
Exporting (overlay/non-blocking where possible)
```

## Load Flow

```text
Empty/Ready
  → open/drop/recent
  → Loading: read skeleton → detect Runtime → atlas → textures → create state → renderer
  → Ready / ReadyWithWarnings / Unsupported / Failed
```

Preserve the previous Ready asset until replacement succeeds where practical.
When accepted load requests overlap, only the latest request may update the
workspace, diagnostics, or state. Older successful results are disposed.

TASK-005 keeps the previous document metadata when a replacement fails. During inspection the UI enters Loading, and then maps unsupported input to Unsupported and other expected open failures to Failed.

TASK-007 keeps the prior rendered frame until replacement succeeds. Verified PPM and TASK-008 PNG renders reach Ready; an asset whose metadata loads but whose texture or attachment the CPU renderer rejects reaches RendererUnavailable without discarding metadata or project-save behavior. TASK-033 keeps the successful frame in memory and does not create a temporary playback PNG.

## Quick-Browse Defaults

- Fit viewport after successful load.
- Select remembered animation when valid, otherwise first animation (or setup
  pose when none exist). TASK-059 keeps animation/skin memory per full asset path
  within one workspace instance; explicit sidecar selections take precedence.
- Binding refresh cannot clear a valid selected animation or dirty a newly opened document.
- No animations: display setup pose and a resource-backed empty-animation label.
  Playback is disabled; skin selection, layers, screenshot, sidecar and static
  one-frame export remain available (TASK-059).
- Auto-play the selected animation after successful load, as accepted by ADR-004.
- Skin and advanced settings remain collapsed by default; selected-layer opacity, Track 0 Alpha, and PMA remain visible because they directly affect rendering.
- RGBA/RGB/Alpha changes viewport presentation and screenshot capture only; it does not modify source alpha or project state.
- TASK-071: the preview, Screenshot, and fixed-size export share one stable scene camera (`SceneCamera`): one DIP / output pixel per unit at zoom 1, no per-frame fit. Fit zooms (at most 100%) and pans to the visible content and runs once after an asset or project opens. The CPU preview and Screenshot follow zoom and pan, and Screenshot equals the CPU viewport. View > Show Axes (scene X red, Y green, plus the selected layer origin) and View > Show Export Frame (Fixed size only, outside dimmed) are remembered user settings that never reach output.
- TASK-074: Mix (s) (0-5, default 0, remembered) crossfades from the previous animation when a layer's animation changes during playback; mixes end when finished, paused, or when the timeline wraps, and never reach Screenshot or export. The timeline marks the selected layer's Spine event keys, and the last event passed during playback shows briefly in the viewport.
- Copy Screenshot (Export menu) produces the same image as Screenshot and places it on the clipboard as a bitmap plus a transparent PNG entry, without a file dialog (TASK-075).
- Background Custom color shows a `#RRGGBB` field in the viewport options; the color is project state with Undo/Redo, saved only while Custom is selected, and never written to screenshots or exports (TASK-075).
- Screenshot captures the current visible scene-layer composition at the viewport pixel dimensions, then applies RGBA/RGB/Alpha inspection to the final frame. PNG sequence export captures the same ordered layer presentation as raw RGBA and is not changed by the inspection channel. Export size defaults to Auto fit (content bounds across all frames, Scale 1, 16 px margin, fixed framing without per-frame rescaling); Fixed size uses Export width and height and frames the scene origin at the canvas center 1:1 through the shared scene camera, clipping rather than shrinking large poses (TASK-071). Export FPS and size settings are session preferences that neither dirty the project nor enter Undo/Redo (TASK-063). Export Format (PNG sequence default, GIF, WebP, APNG, MP4) and the MP4 background are session preferences too; encoded formats save one file through a format-specific dialog whose overwrite prompt confirms replacement, show "Rendering" then "Encoding" progress, and report a missing FFmpeg as an export diagnostic. The FFmpeg path chosen with Browse is remembered in `%APPDATA%\SpineViewerWPF\settings.json`; Use PATH forgets it (TASK-066, ADR-010). Export Animations (Current default, or All animations), Range (Full default, or Custom start/end seconds of the current animation, clamped to its duration), and Physics warm-up loops (0 default, 0-10) are session preferences too. All animations exports every animation of the selected layer in full, one sequence or file per animation named `<chosen name>-<animation>`; it hides Range, refuses to start when any target exists, and removes everything it wrote when it fails or is canceled (TASK-072).
- Checkerboard, Dark, and Light viewport backgrounds are presentation-only and are not baked into Screenshot or PNG sequence output; all-hidden scenes therefore produce a transparent frame.
- Warnings do not interrupt the viewport unless blocking.

## Interaction Contracts

- File, Edit, View, Playback, Layer, Export, Window, and Help provide the stable desktop command taxonomy.
- The activity rail is reserved for panel navigation. Menu items and toolbar buttons reuse the same commands, enablement, and shortcuts rather than duplicating use-case logic.
- Window menu operations continue to use the dock-aware show, hide, float, redock, and reset handlers. Float Inspector also has the `Ctrl+Shift+I` shortcut.
- Mouse wheel: viewport zoom anchored at the cursor, bounded to 10%-800%, no
  modifier required (TASK-060). The status bar shows the current zoom.
- Left-drag beyond a small threshold or middle-drag: viewport pan, no modifier
  required. Presses that start on buttons, text boxes, thumbs, or scroll bars
  inside the viewport keep their own behavior.
- Double-click on the viewport: Fit.
- Model transform requires explicit controls or a visible mode.
- Layer opacity fades the complete WPF scene layer; Track 0 Alpha changes Runtime animation mixing; PMA changes texture compositing. These controls are not interchangeable.
- Display channel selects RGBA, opaque RGB, or opaque grayscale Alpha inspection from the current in-memory frame.
- The status bar and Inspector identify whether normal RGBA playback is using
  GPU triangles or the CPU fallback renderer.
- Preview FPS defaults to 30, accepts 1 through 240, and controls the common GPU
  and CPU playback cadence. It remains independent from Export FPS and does not
  dirty the Viewer project or enter Undo/Redo history.
  At low rates, the elapsed-time safety clamp permits at least one scheduled
  frame interval while still bounding delayed UI ticks.
- Dropping a skeleton opens it directly; a missing or ambiguous atlas offers a
  manual atlas picker before the load is failed.
- Selected-layer slot visibility, opacity, and named attachment selection are
  presentation edits stored in the Viewer sidecar; source Spine files remain
  read-only. Slot visibility uses a full-row name-and-switch target so the edit
  is not limited to a small checkbox; the switch updates immediately, survives
  preview rerendering, and participates in Undo/Redo. The empty attachment
  choice follows animation/setup behavior.
- Theme selection is a presentation preference with Dark and Light modes.
- Hover, pressed, selected, checked, and disabled states must update foreground
  and background together through theme resources so labels remain readable.
- Fit changes view only.
- Reset semantics must be explicit: viewport reset and model reset are distinct commands if both exist.
- Space: play/pause and F: Fit only without modifiers and when focus is not in
  a text box, password box, or editable combo box (TASK-060).
- Delete removes the selected layer only while focus is in the Layers list; the
  Layer and context menus keep their Remove commands.
- Frame steps (TASK-065): Previous/Next frame move one preview frame
  (`1 / Preview FPS`) on that frame grid; Back/Forward 10 frames move ten.
  Steps pause playback, clamp to the selected layer's duration without
  wrapping, and use Ctrl+Left/Right and Ctrl+Shift+Left/Right outside text
  boxes. Restart seeks to zero and plays. None of these dirty the project.
- Full-screen preview (F11, View menu, playback-bar button) hides the menu,
  command bar, docked Browse and Inspector panels, and status bar, keeping the
  viewport and playback bar. F11, Esc, or the button restores the previous
  window state and panels. It is not saved.
- Layer focus (double-click in Layers, Layer menu, layer context menu) pans the
  viewport so the layer's GPU-framed content center is centered; zoom is kept
  and the project is not dirtied. Without a GPU scene it reports that focus
  needs the GPU preview.
- Slot batch actions (Show all, Hide all, All Auto) act on the slots shown by
  the Slots filter of the selected layer; each is one Undo step and a no-op is
  reported without dirtying.
- Search/filter does not alter playback until a selection is made.
- Selecting a scene layer changes the Properties editing context and the timeline duration reference; it does not reorder the scene.
- Duplicate opens an independent render session and preserves the source layer's editable settings without changing source files.
- Reload replaces only the selected layer after the replacement render session succeeds; failure preserves the existing scene.
- Auto Reload (File menu, on by default, remembered in user settings) watches each layer's skeleton, atlas, and the atlas page images named in the atlas. Once writes have been quiet for 0.6 s, every layer reading a changed file reloads with its current settings; the selection is kept, and a busy workspace (loading, exporting, capturing, or another layer operation) retries after 1 s. Turning it off stops watching (TASK-073).
- Open Recent lists up to 10 successfully opened assets and Viewer projects, most recent first, remembered in user settings; a missing entry is removed with a warning, and Clear Recent Files empties the list. `--settings=<path>` points user settings at another file for tests (TASK-073).
- Parameter copy/paste never copies source paths or z-order. The supported scopes are all, transform, render, appearance, and slots (slot visibility, opacity, and attachment choices only).
- Numeric transform input accepts finite coordinates beyond the earlier slider limits; scale remains finite and greater than zero.
- Slot attachment selection applies after animation posing. An unavailable saved name falls back to animation/setup behavior without unloading the asset.
- Screenshot and PNG sequence export snapshot the layer list before background rendering. Visible layers use stable ascending Z order and include layer opacity, transform, flips, animation, skin, Track 0 Alpha, PMA, and slot settings. Model translation is interpreted as 96-DPI output pixels; viewport pan and zoom remain preview-only.

## Presentation State

ViewModels may expose primitives, presentation DTOs, commands, and observable collections. They must not expose or own official Runtime types, graphics devices, textures, or render targets. A scene-layer view model may own a disposable Application render-session abstraction and must release it when the layer is removed or replaced.

## Editable Viewer Project

- A project is dirty when its editable snapshot differs from the last loaded or saved snapshot.
- Undo and Redo operate on project settings, not viewport playback time.
- Save and Save As target only `*.spineviewer.json`.
- Successful save clears dirty state; a failed or canceled save preserves it.
- Save serializes current scene-layer documents without changing live layer state. Schema-version-1 top-level compatibility fields mirror the current primary layer.
- Add, duplicate, remove, reorder, and auto-layout currently invalidate Undo/Redo history because structural snapshots are not implemented. The project remains dirty until saved or replaced.
- Closing a dirty project offers Save, Discard, and Cancel.
- Spine JSON, binary, atlas, and texture sources remain read-only.
- Per-layer Track 0 Alpha and PMA round-trip in the Viewer sidecar; older schema-version-1 documents use the top-level Track Alpha as the first-layer fallback.
- Optional slot attachment names round-trip inside schema-version-1 slot settings; older files without the field preserve animated/setup attachments.

## Command Enablement

| Command | Enabled when |
|---|---|
| Play | asset ready, animation selected, not blocked |
| Pause | playing |
| Seek | duration known and asset ready |
| Restart, frame steps | playable (same as Play) |
| Focus layer | selected layer |
| Slot batch actions | selected layer |
| Screenshot | asset ready, renderer available, not exporting |
| Export | asset ready, selection valid, no blocking diagnostic |
| Reload | asset identity known and not in unsafe operation |
| Duplicate layer | selected layer, fewer than eight layers, no layer operation in progress |
| Reload layer | selected layer, Runtime service available, no layer operation in progress |
| Copy layer parameters | selected layer |
| Paste layer parameters | selected layer and an in-memory parameter snapshot |
| Runtime override | not exporting; reload confirmation if needed |
| Save project | asset ready; project is dirty or has no project path |
| Save project as | asset ready |
| Undo | edit history is not empty |
| Redo | redo history is not empty |

## Error Presentation

- Blocking: concise summary, file, Runtime, next action, diagnostics link.
- Warning: status indicator and diagnostics; avoid repeated modal dialogs.
- Export failure: keep current asset loaded.
- Screenshot failure: keep the current asset loaded and publish a `CAPTURE_FAILED` diagnostic through the existing status path.

## AI/Automation Validation

Each UI task specifies deterministic fake data, state list, window size, DPI, automation IDs, keyboard walkthrough, and screenshots.
