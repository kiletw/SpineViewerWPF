# External Feature Reference

## Purpose

External projects inform product discovery but do not define this repository's behavior or architecture.

## Authorities

- Official Runtime implementation: pinned [`EsotericSoftware/spine-runtimes`](https://github.com/EsotericSoftware/spine-runtimes) snapshots.
- Product and UI reference: [`ww-rm/SpineViewer`](https://github.com/ww-rm/SpineViewer).
- v3 behavior and architecture: this repository's verified contracts, tasks, and Accepted ADRs.

## Competitor Feature Inventory

The competitor documents the following useful ideas:

| Area | Referenced capability | v3 treatment |
|---|---|---|
| Import | drag/drop, paste, file and folder loading | retain as product input; implement through Application use cases |
| Browse | models, browser, canvas, focus and centering | TASK-043 layer list and contextual actions; folder browser remains separate |
| Playback | animation and skin groups, multiple tracks, speed and track alpha | TASK-019 per-layer animation/skin; TASK-032 effective Track 0 Alpha/PMA; multiple tracks remain separate |
| Scene | multiple skeletons, ordering, parameter reuse and non-overlap layout | TASK-018/TASK-020 layer preview/layout; TASK-043 duplicate, reload, reorder and scoped parameter copy/paste |
| Properties | transform, render, appearance, slots, animation and debug categories | TASK-043 selected-layer Properties categories; TASK-044 named slot attachment selection; attachment authoring, multi-track and debug geometry remain separate |
| Diagnostics | debug rendering and compatibility visibility | retain diagnostics direction |
| Export | still image, GIF, video, PSD layers and FFmpeg options | separate export tasks |
| Media | non-PNG textures and wallpaper mode | separate compatibility/product tasks |
| Localization | Chinese, English and Japanese UI | resource-ready now; language switch later |

The documented competitor layout uses fixed left-side model/browser/canvas areas beside a preview. Docking and floating panels are therefore a product-owner requirement for this project, not behavior copied from the competitor.

## Adoption Rule

This inventory is not a roadmap. Each adopted capability requires a bounded task, an Application-level contract where use-case logic is involved, and validation against this project's supported Runtime matrix.

## Adoption Roadmap (2026-09-27 review)

Reviewed against `artifacts/competitor-spineviewer` (v0.16.28). Ordered by value
to the quick-browse and export workflow; each item still needs its own task.

| Priority | Capability | Status / notes |
|---|---|---|
| 1 | Export framing: auto-fit to content bounds, margin, scale, fixed size | TASK-063 |
| 2 | Playback transport: frame step, fast step, restart, full screen, reset view | TASK-065 (reset view is Fit) |
| 3 | Focus/center the selected layer (double-click in Layers) | TASK-065 |
| 4 | Slot batch actions: show/hide all, clear attachment choices, scoped copy | TASK-065 |
| 5 | GIF / WebP / APNG / MP4 export through an external FFmpeg | TASK-066 (ADR-010); FFmpeg is an optional user-installed tool |
| 6 | Folder browser with thumbnails, filtering, and batch import | planned |
| later | Debug geometry (bounds, bones first), multi-track animation, skin combinations | needs Runtime-neutral contracts across all adapters |
| later | Direct viewport selection and layer dragging | needs a tool mode; left-drag currently pans |
| later | Non-PNG textures, nearest filtering, PSD layers, UI language switching, file association | separate compatibility/product tasks |
| no | Desktop wallpaper, tray residency, auto-start, WorkerW debugging, hit-slot logging | outside the viewer product scope |

## Adoption Roadmap, Round 2 (2026-10-07 review)

Round 1 reviewed only `ww-rm/SpineViewer`. Round 2 adds the official Spine
Skeleton Viewer, the Spine Web Player, the Spine Editor image/video export
dialog, game-asset Spine viewers, and general GIF/animation tools. Items already
listed above are not repeated. Each item still needs its own task.

| Order | Capability | Reference | Status / notes |
|---|---|---|---|
| 1 | Application icon (exe, windows, floating panels) | every desktop competitor | TASK-070 |
| 2 | Viewport guides: world XY axes and the fixed-size export frame | Spine Editor, export tools | TASK-071; never written to screenshot/export; auto-fit frame later |
| 3 | Export batch (several animations), frame range, Physics pre-roll | Spine Editor export | TASK-072 |
| 4 | Auto reload on source change; recent files | Skeleton Viewer; most viewers | TASK-073; recent files is already in `14-ui-product-design.md` scope |
| 5 | Animation mix duration on switch; Spine event display | Skeleton Viewer, Web Player | TASK-074; needs Runtime-neutral contracts across adapters |
| 6 | Screenshot to system clipboard; custom background color | game-asset viewers | TASK-075 |
| later | Sprite-sheet export, A-B loop range, update check | Spine Editor, GIF tools | separate tasks |
| later | Onion skin, event audio playback | Spine Editor | low value for a viewer |
| no | Web Player HTML export; any Spine source modification | Web Player | redistribution concerns; sources stay read-only |

Existing differentiators to preserve: Runtime 2.1-4.3 coverage, RGBA/RGB/Alpha
inspection, Undo/Redo, non-destructive sidecar projects, visible GPU/CPU
fallback, and per-slot named attachment selection.
