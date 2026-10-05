# TASK-001: v3 One-Runtime Vertical Slice

## Status

Completed on 2026-07-26. The project-authored 4.1 fixture removed the redistribution blocker, and ADR-005 records the renderer spike decision and deterministic evidence.

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

## Completion Evidence

- `inspect` and `render` run through Core/Application and the isolated 4.1 adapter without WPF.
- `inspect` emits the required `inspect-result.schema.json` fields; render arguments map to `render-request.schema.json`.
- Official Runtime source is pinned to tag `4.1.00`, commit `ab28b77c70e3aa766be5bdb759d7aedac9fd0bde`, with no source patches.
- Atlas ownership is scoped and disposed by the adapter; the CPU renderer owns no persistent graphics resources.
- `scripts/test-v3.ps1` builds, checks metadata, renders twice, validates the PNG signature, and compares SHA-256 output.

## Validation

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
```

Expected render SHA-256:

```text
7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E
```

## Known Gaps

- The committed fixture is valid project-authored Runtime input, not a Spine Editor export characterization asset.
- Binary skeletons, PMA, clipping, non-normal blend modes, multi-page atlases, and production texture formats remain unverified.
- The CPU renderer is prototype evidence, not the production WPF renderer selection.
