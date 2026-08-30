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
- Select remembered animation when valid, otherwise first animation.
- Binding refresh cannot clear a valid selected animation or dirty a newly opened document.
- No animations: display setup pose.
- Auto-play the selected animation after successful load, as accepted by ADR-004.
- Skin and advanced settings remain collapsed by default; selected-layer opacity, Track 0 Alpha, and PMA remain visible because they directly affect rendering.
- RGBA/RGB/Alpha changes viewport presentation and screenshot capture only; it does not modify source alpha or project state.
- Screenshot captures the current visible scene-layer composition at the viewport pixel dimensions, then applies RGBA/RGB/Alpha inspection to the final frame. PNG sequence export captures the same ordered layer presentation at Export Size as raw RGBA and is not changed by the inspection channel.
- Checkerboard, Dark, and Light viewport backgrounds are presentation-only and are not baked into Screenshot or PNG sequence output; all-hidden scenes therefore produce a transparent frame.
- Warnings do not interrupt the viewport unless blocking.

## Interaction Contracts

- File, Edit, View, Playback, Layer, Export, Window, and Help provide the stable desktop command taxonomy.
- The activity rail is reserved for panel navigation. Menu items and toolbar buttons reuse the same commands, enablement, and shortcuts rather than duplicating use-case logic.
- Window menu operations continue to use the dock-aware show, hide, float, redock, and reset handlers. Float Inspector also has the `Ctrl+Shift+I` shortcut.
- Mouse wheel: viewport zoom.
- Drag: viewport pan.
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
- Space: play/pause when focus context permits.
- Search/filter does not alter playback until a selection is made.
- Selecting a scene layer changes the Properties editing context and the timeline duration reference; it does not reorder the scene.
- Duplicate opens an independent render session and preserves the source layer's editable settings without changing source files.
- Reload replaces only the selected layer after the replacement render session succeeds; failure preserves the existing scene.
- Parameter copy/paste never copies source paths or z-order. The supported scopes are all, transform, render, and appearance.
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
