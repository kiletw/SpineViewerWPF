# ADR-002: Isolate Official Runtime Snapshots

## Status

Accepted on 2026-07-26 after TASK-000 verified 14 namespace-renamed Runtime snapshots compiled directly into the WPF executable, with incomplete upstream equivalence metadata.

## Context

Historical Spine export formats require matching Runtime lines. v2 compiles many Runtime snapshots directly into the WPF project and duplicates a Player per version.

## Decision

Place each supported pinned Runtime snapshot in an isolated project/assembly and expose it only through project-owned Runtime adapter contracts. Treat upstream source as read-only by default and record exact commit metadata.

## Consequences

- Type/name conflicts are contained.
- Runtime provenance and patches become auditable.
- Application and UI no longer depend on version-specific APIs.
- Adapter and contract tests are required for each Runtime line.
