# TASK-001: v3 One-Runtime Vertical Slice

## Status

Blocked until TASK-000 is complete and ADR-001/002/003 are accepted or revised.

## Objective

Prove one complete non-WPF path using one Runtime line, provisionally 4.1:

```text
inspect → load → list metadata → render deterministic PNG
```

## Required Deliverables

- minimal Core/Application contracts
- isolated pinned Runtime project
- one Runtime adapter
- minimal renderer spike selected by explicit ADR/prototype evidence
- CLI `inspect` and `render`
- compatibility fixture and deterministic validation

## Out of Scope

- all historical Runtime lines
- full WPF UI
- MCP
- GIF/video export
- generalized plugin architecture

## Acceptance Criteria

- CLI works without WPF
- JSON result follows schemas
- official Runtime source is unchanged or patch is documented
- Runtime/GPU resources are disposed
- rendered output is reproducible under recorded conditions
