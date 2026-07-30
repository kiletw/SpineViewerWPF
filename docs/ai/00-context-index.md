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
- v3 prototype: TASK-001 one-Runtime vertical slice completed for project-authored 4.1 JSON input; TASK-023 adds 4.0.64, TASK-024 connects every other vendored historical line (2.1.08 through 4.0.31), TASK-026 verifies the official 3.8.55 export through the 3.8.95 adapter, TASK-027 adds bounded clipping/blend handling to the CPU bridge, and TASK-028 verifies a two-page 4.1 atlas fixture
- Renderer: deterministic CPU spike accepts P3 PPM and bounded 8-bit non-interlaced PNG textures, including premultiplied-alpha compositing, runtime clipping where exposed, and bounded normal/additive/multiply/screen slot blending; oversized poses use bounds-aware static fit; official 4.1 and 3.8 JSON/binary `spineboy` renders pass compatibility baselines; TASK-031 reuses loaded Runtime/texture resources during WPF playback and sequence export while preserving CLI baselines; production renderer remains undecided
- CLI: version-aware `inspect` and `render` implemented for every vendored fixture-backed line; 4.2/4.3 remain unsupported without source snapshots
- MCP: capability planning only
- UI: TASK-008 shows generated 4.1 PPM and PNG variants as real deterministic static frames; TASK-009 validates official 4.1 JSON and binary assets through the CLI path; TASK-010 fits oversized static poses to the canvas; TASK-011 resolves nearby official atlases without an explicit path; TASK-012 connects WPF playback to frame rendering; TASK-013 saves the current displayed PNG; TASK-014 adds bounded viewport pan/zoom and Fit; TASK-015 exposes actionable diagnostics in the WPF shell; TASK-016 exports deterministic PNG sequences; TASK-017 adds a customizable export FPS control; TASK-018 adds bounded multi-skeleton scene layers; TASK-019 adds per-layer animation and skin selection; TASK-020 adds batch scene import and deterministic auto layout; TASK-022 reopens saved Viewer sidecars and restores the scene without source writes; TASK-029 validates WPF Browse/Inspector hide, float, redock, and reset-layout behavior; TASK-030 preserves floated-panel bindings and removes prototype-only shell chrome; TASK-031 keeps real animation selection clean/playable and raises the WPF preview/screenshot path to 512 by 512
- Localization: resource boundary required now; runtime language switching remains deferred
- Editing: TASK-004 adds Inspector editing, Undo/Redo, dirty state, and versioned `*.spineviewer.json` sidecar save; Spine source writing remains forbidden
