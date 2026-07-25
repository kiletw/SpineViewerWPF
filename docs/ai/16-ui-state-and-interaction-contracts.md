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

## Quick-Browse Defaults

- Fit viewport after successful load.
- Select remembered animation when valid, otherwise first animation.
- No animations: display setup pose.
- Auto-play is `TBD`; prototype both policies.
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

ViewModels may expose primitives, presentation DTOs, commands, and observable collections. They must not own official Runtime objects, graphics devices, textures, or render targets.

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

## Error Presentation

- Blocking: concise summary, file, Runtime, next action, diagnostics link.
- Warning: status indicator and diagnostics; avoid repeated modal dialogs.
- Export failure: keep current asset loaded.

## AI/Automation Validation

Each UI task specifies deterministic fake data, state list, window size, DPI, automation IDs, keyboard walkthrough, and screenshots.
