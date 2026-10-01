# AI Context Index

## Project Identity

This repository is `kiletw/SpineViewerWPF`.

Do not confuse it with:

- `ww-rm/SpineViewer`
- `EsotericSoftware/spine-runtimes`

The external projects may be upstream or architectural references, but they are not the source of truth for this product.

## Modernization Definition

SpineViewerWPF v3 is a progressive rewrite based on v2 observable behavior and compatibility knowledge.

- v2 is the stable behavior reference.
- v3 is the new implementation.
- Fixtures, behavior contracts, and compatibility tests connect them.

## Required Read Order

1. `01-project-identity.md`
2. `02-current-architecture.md`
3. `03-behavior-contracts.md`
4. `04-runtime-matrix.md`
5. `05-technical-constraints.md`
6. `06-target-architecture.md`
7. `07-migration-plan.md`
8. `09-ai-working-rules.md`
9. current task under `tasks/`
10. relevant ADRs under `decisions/`

Read `10-cli-contract.md`, `11-mcp-capability-map.md`, and UI documents only when the task touches those surfaces.

## Sources of Truth

| Subject | Source of truth |
|---|---|
| v2 observable behavior | v2 executable, source, and characterization tests |
| Official Runtime implementation | pinned `EsotericSoftware/spine-runtimes` snapshots |
| Supported export versions | verified fixture results in `04-runtime-matrix.md` |
| v3 architecture | Accepted ADRs |
| Product and UI direction | product-owner input recorded in Accepted ADRs |
| External feature ideas | `17-external-feature-reference.md`; reference only, not a contract |
| Work scope | active `TASK-xxx.md` |
| Build/validation | repository CI and active task |

## Current Status

- Phase 0 baseline: completed on v2 commit `79c6135`; compatibility fixtures remain missing and explicit
- Repository: TASK-047 makes the root solution v3-only, removes the legacy .NET Framework/XNA Viewer, and relocates byte-identical active historical Runtime inputs under their matching `runtimes/SpineRuntime.V*/src` projects
- v3 prototype: TASK-001 one-Runtime vertical slice completed for project-authored 4.1 JSON input; TASK-023 adds 4.0.64, TASK-024 connects every historical line from 2.1.08 through 4.0.31, TASK-026 verifies the official 3.8.55 export, TASK-027/TASK-028 add clipping/blend and multi-page coverage, TASK-041 adds the clean official 4.2 snapshot plus JSON/binary/Physics verification, and TASK-051 adds the clean official 4.3 snapshot plus JSON/binary/Physics and Runtime-neutral scene verification
- Renderer: deterministic CPU rendering remains authoritative for capture/export, channel inspection, and fallback; TASK-036 selects a WPF-only OpenTK GPU viewport for normal RGBA playback, consumes Runtime-neutral textured triangles, validates the first visible framebuffer, and falls back without unloading the asset; TASK-037/TASK-038 carry slot edits through preview and export; TASK-039 fixes slow-frame starvation and exposes live metrics; TASK-040 reuses Runtime pose state; TASK-041 adds 4.2 triangle scenes and bounded deterministic Physics replay; TASK-042 corrects the 3.5.51 Screen/PMA path; TASK-044 applies slot visibility, opacity, and attachment selection once in the shared Runtime pose for GPU and CPU parity; TASK-050 adds one Application-owned CPU compositor so Screenshot and multi-layer PNG sequence output include ordered layer presentation state while the byte-stable default single-layer export path remains unchanged; TASK-055 bounds file-render pixels and retained composite BGRA buffers before allocation
- CLI: version-aware `inspect` and `render` are implemented from 2.1.08 through 4.3
- MCP: capability planning only
- UI: TASK-008 through TASK-035 establish the real quick-browse, deterministic export, editable sidecar, multi-layer, channel-inspection, dockable, compact workspace; TASK-036 moves normal RGBA playback to GPU triangle rendering, identifies the active backend, and retains CPU screenshot/export/fallback behavior; TASK-037 adds recoverable atlas selection, drag/drop opening, first-frame invalidation, slot visibility/opacity sidecar settings, clearer selected controls, and Dark/Light theme switching; TASK-038 keeps slot edits consistent in WPF sequence export; TASK-039 adds low-rate live playback metrics and real-asset startup acceptance; TASK-043 separates Layers from selected-layer Properties and adds reusable layer actions; TASK-044 adds searchable per-slot named attachment selection with sidecar and Undo/Redo coverage; TASK-045 replaces the undersized Slot checkbox with a full-row switch and verifies live click, rerender persistence, Undo, and Redo; TASK-046 makes CPU/GPU Preview FPS independently configurable from 1-240 with a 30 FPS default; TASK-048 adds a conventional command menu, frequent-action toolbar, and panel-focused activity rail; TASK-049 replaces font-dependent activity glyphs with scalable vectors and a clearer selected-panel indicator; TASK-050 makes Screenshot and PNG sequence output honor the visible multi-layer scene rather than silently exporting only the first layer; TASK-052 keeps the activity rail reachable while Layers is hidden or floating and reduces redundant desktop chrome around the viewport; TASK-056 preserves real playback cadence at 1-3 Preview FPS
- UI contrast: TASK-053 corrects shared hover, pressed, check, switch, and disabled-state foreground/background pairing across Dark and Light themes.
- Project edit integrity: TASK-054 makes save serialize the current primary-layer snapshot without mutating it and invalidates Undo/Redo history after structural or batch layer edits that do not yet have structural snapshots.
- Load concurrency: TASK-057 gives each accepted asset/project open a generation so only the latest request can replace the workspace; stale sessions and project layers are disposed.
- UI refinement: TASK-058 aligns the desktop shell with Photoshop/AE/Spine/Unity design references, restructuring command toolbar chrome, grouping the Layers actions inside the scrollable panel, converting the 3x2 property tabs into clean segmented cards, adding search placeholders, and eliminating redundant timing controls.
- TASK-059: setup pose uses empty animation names in render/layer contracts, supports static preview/skin/sidecar/export, and remembers animation/skin per canonical asset path within one workspace session. Explicit sidecar selections take precedence; current validation is recorded in TASK-059.
- TASK-060: cursor-anchored unmodified wheel zoom, left/middle-drag pan, double-click Fit, status zoom, focus-aware Space/F, Layers-scoped Delete, DWM-themed native title bars, and reduced accent noise; automated validation passed 2026-10-02, manual walkthrough found Space/F gaps under IME and list/button focus.
- TASK-061: the Slots list keeps one filtered instance and is re-announced only when slots are added, so rows stay stable and clickable during playback.
- TASK-062: Properties uses Animation / Layer / Slots single-row tabs with one-line slot rows; channel/background/backend move to a viewport overlay and View menu, theme to View > Theme, project identity to Layers asset information; the Layers list fills the panel above a pinned icon footer.
- TASK-064: the Slots list no longer scrolls horizontally, so opacity stays visible; Screenshot/Export re-enable after an export completes.
- TASK-066: GIF, WebP, APNG, and MP4 export encoded by a user-installed FFmpeg (ADR-010), with the FFmpeg path remembered in a user settings file.
- TASK-065: frame step/fast step/restart and F11 full-screen preview, layer focus from the Layers panel, and filtered slot batch actions plus a Slots copy scope.
- TASK-063: optional auto-fit export framing (content bounds over all frames, scale, margin) through a fixed per-layer `RenderCamera`; WPF defaults to Auto fit with Fixed size available; `17-external-feature-reference.md` records the competitor adoption roadmap.
- Localization: resource boundary required now; runtime language switching remains deferred
- Editing: TASK-004 adds Inspector editing, Undo/Redo, dirty state, and versioned `*.spineviewer.json` sidecar save; Spine source writing remains forbidden
