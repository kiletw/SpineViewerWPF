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
| Browse | models, browser, canvas, focus and centering | Asset Browser panel candidate |
| Playback | animation and skin groups, multiple tracks, speed and track alpha | post-MVP candidates; no implied commitment |
| Scene | multiple skeletons, ordering and non-overlap layout | deferred |
| Diagnostics | debug rendering and compatibility visibility | retain diagnostics direction |
| Export | still image, GIF, video, PSD layers and FFmpeg options | separate export tasks |
| Media | non-PNG textures and wallpaper mode | separate compatibility/product tasks |
| Localization | Chinese, English and Japanese UI | resource-ready now; language switch later |

The documented competitor layout uses fixed left-side model/browser/canvas areas beside a preview. Docking and floating panels are therefore a product-owner requirement for this project, not behavior copied from the competitor.

## Adoption Rule

This inventory is not a roadmap. Each adopted capability requires a bounded task, an Application-level contract where use-case logic is involved, and validation against this project's supported Runtime matrix.
