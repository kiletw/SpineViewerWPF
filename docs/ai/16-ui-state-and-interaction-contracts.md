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

TASK-005 keeps the previous document metadata when a replacement fails. During inspection the UI enters Loading, and then maps unsupported input to Unsupported and other expected open failures to Failed.

TASK-007 keeps the prior rendered frame until replacement succeeds. Verified PPM and TASK-008 PNG renders reach Ready; an asset whose metadata loads but whose texture or attachment the CPU renderer rejects reaches RendererUnavailable without discarding metadata or project-save behavior.

## Quick-Browse Defaults

- Fit viewport after successful load.
- Select remembered animation when valid, otherwise first animation.
- Binding refresh cannot clear a valid selected animation or dirty a newly opened document.
- No animations: display setup pose.
- Auto-play the selected animation after successful load, as accepted by ADR-004.
- Skin and advanced settings remain collapsed by default.
- Warnings do not interrupt preview unless blocking.

## Interaction Contracts

- Mouse wheel: viewport zoom.
- Drag: viewport pan.
- Model transform requires explicit controls or a visible mode.
- Fit changes view only.
- Reset semantics must be explicit: viewport reset and model reset are distinct commands if both exist.
- Space: play/pause when focus context permits.
- Search/filter does not alter playback until a selection is made.

## Presentation State

ViewModels may expose primitives, presentation DTOs, commands, and observable collections. They must not expose or own official Runtime types, graphics devices, textures, or render targets. A scene-layer view model may own a disposable Application render-session abstraction and must release it when the layer is removed or replaced.

## Editable Viewer Project

- A project is dirty when its editable snapshot differs from the last loaded or saved snapshot.
- Undo and Redo operate on project settings, not viewport playback time.
- Save and Save As target only `*.spineviewer.json`.
- Successful save clears dirty state; a failed or canceled save preserves it.
- Closing a dirty project offers Save, Discard, and Cancel.
- Spine JSON, binary, atlas, and texture sources remain read-only.

## Command Enablement

| Command | Enabled when |
|---|---|
| Play | asset ready, animation selected, not blocked |
| Pause | playing |
| Seek | duration known and asset ready |
| Screenshot | asset ready and renderer available |
| Export | asset ready, selection valid, no blocking diagnostic |
| Reload | asset identity known and not in unsafe operation |
| Runtime override | not exporting; reload confirmation if needed |
| Save project | asset ready; project is dirty or has no project path |
| Save project as | asset ready |
| Undo | edit history is not empty |
| Redo | redo history is not empty |

## Error Presentation

- Blocking: concise summary, file, Runtime, next action, diagnostics link.
- Warning: status indicator and diagnostics; avoid repeated modal dialogs.
- Export failure: keep current asset loaded.

## AI/Automation Validation

Each UI task specifies deterministic fake data, state list, window size, DPI, automation IDs, keyboard walkthrough, and screenshots.
