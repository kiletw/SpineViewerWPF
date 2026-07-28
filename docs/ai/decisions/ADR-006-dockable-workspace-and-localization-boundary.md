# ADR-006: Dockable Workspace and Localization Boundary

## Status

Accepted on 2026-07-26 from explicit product-owner direction. Docking toolkit selection remains undecided.

## Context

TASK-002 validated a fast, single-window quick-browse flow. The product also needs a Photoshop-style workspace in which selected tools can leave the main window and later dock back into it. The competitor is useful for feature discovery, while official Runtime behavior must continue to come from pinned `EsotericSoftware/spine-runtimes` sources.

Language switching is desired but is not required in the current implementation.

## Decision

1. Preserve ADR-004's viewport-first quick-browse arrangement as the default layout.
2. Allow selected supporting panels to dock, tab, float, hide/show, and reset to the default layout.
3. Keep the central viewport and product use cases independent of docking controls.
4. Do not add a docking package until a bounded prototype records maintenance, license, deployment, accessibility, and layout-persistence evidence.
5. Use resource identities for new user-facing text and keep translated text out of state and command logic.
6. Defer runtime language switching and translated resource sets.
7. Treat `ww-rm/SpineViewer` as a feature reference only; do not copy its architecture or make its behavior a compatibility contract.

## Consequences

- ADR-004 is amended rather than replaced: the default workflow stays fast, but the window model is no longer permanently fixed.
- TASK-029 proves the interaction with the smallest useful panel set (Browse and Inspector) using native WPF windows.
- Layout persistence is optional until the dock/float/redock flow is validated.
- Existing hard-coded prototype text may be migrated when the real presentation shell is implemented; no speculative localization service is required now.
