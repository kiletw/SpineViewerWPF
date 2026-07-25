# ADR-004: Quick-Browse WPF Shell

## Status

Accepted on 2026-07-26 after TASK-002 validated all required fake states, a one-interaction open-to-preview path, keyboard commands, stable automation IDs, and the collapsed 756 by 519 layout.

## Context

The primary user need is quick viewing, not a persistent export studio. v2 also couples WPF code-behind with Runtime and renderer state.

## Decision

1. Use a single-window, viewport-first quick-browse shell.
2. Keep animation search/list immediately available.
3. Collapse skins and asset details; place transform, PMA, background, and Runtime override in an on-demand drawer.
4. Keep export in a separate dialog.
5. Treat WPF as an adapter over Application use cases.
6. Use explicit presentation states and stable automation IDs.
7. Do not choose a final visual toolkit before workflow prototype validation.
8. Auto-play the remembered or first animation after a successful load, preserving verified v2 behavior.
9. Remember animation and skin per canonical asset path. Always fit the viewport on open; do not persist pan/zoom in the first implementation.

## Consequences

- The common open-to-preview path is shorter.
- Advanced and export workflows remain available without dominating the main UI.
- Presentation DTO/state mapping is required.
- The fake-state shell proves workflow and presentation only; live Runtime/renderer integration remains separate.
- Per-asset persistence needs a project-owned storage policy when the real session use case is implemented.
