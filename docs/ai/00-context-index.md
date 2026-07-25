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
| Work scope | active `TASK-xxx.md` |
| Build/validation | repository CI and active task |

## Current Status

- Phase 0 baseline: completed on v2 commit `79c6135`; compatibility fixtures remain missing and explicit
- v3 prototype: TASK-001 one-Runtime vertical slice completed for project-authored 4.1 JSON input
- Renderer: deterministic CPU spike accepted by ADR-005; production renderer remains undecided
- CLI: 4.1 `inspect` and `render` implemented
- MCP: capability planning only
- UI: quick-browse direction selected, prototype not started
