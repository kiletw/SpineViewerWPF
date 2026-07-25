# ADR-004: Quick-Browse WPF Shell

## Status

Proposed

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

## Consequences

- The common open-to-preview path is shorter.
- Advanced and export workflows remain available without dominating the main UI.
- Presentation DTO/state mapping is required.
- A fake-state shell prototype can proceed before the live renderer is ready.
