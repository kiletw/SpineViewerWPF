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
- v3 prototype: TASK-001 one-Runtime vertical slice completed for project-authored 4.1 JSON input
- Renderer: deterministic CPU spike accepts P3 PPM and bounded 8-bit non-interlaced PNG textures; oversized poses use bounds-aware static fit; official 4.1 JSON and binary `spineboy` renders pass compatibility baselines; production renderer remains undecided
- CLI: 4.1 `inspect` and `render` implemented
- MCP: capability planning only
- UI: TASK-008 shows generated 4.1 PPM and PNG variants as real deterministic static frames; TASK-009 validates official 4.1 JSON and binary assets through the CLI path; TASK-010 fits oversized static poses to the canvas; TASK-011 resolves nearby official atlases without an explicit path; TASK-012 connects WPF playback to frame rendering; TASK-013 saves the current displayed PNG; TASK-014 adds bounded viewport pan/zoom and Fit
- Localization: resource boundary required now; runtime language switching remains deferred
- Editing: TASK-004 adds Inspector editing, Undo/Redo, dirty state, and versioned `*.spineviewer.json` sidecar save; Spine source writing remains forbidden
