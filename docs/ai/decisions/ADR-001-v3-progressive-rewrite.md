# ADR-001: v3 Is a Progressive Rewrite

## Status

Accepted on 2026-07-26 after TASK-000 verified that UI, Runtime, rendering, export, and process-wide state are mutually coupled in the single v2 project.

## Context

The planned changes affect framework, renderer integration, global state, Runtime isolation, application boundaries, CLI, future MCP, and UI workflow. Treating this as an in-place class cleanup would obscure risk.

## Decision

Develop v3 as a new implementation guided by v2 observable behavior, compatibility fixtures, and documented product knowledge. Keep v2 available as a reference and maintenance line.

## Consequences

- Large amounts of v2 code may not be retained.
- Behavior and fixture documentation become critical assets.
- v3 can use a coherent architecture without destabilizing v2.
- Replacement requires explicit feature and compatibility gates.
