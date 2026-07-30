# Migration Plan

## Phase 0 — v2 Baseline (completed 2026-07-26)

- reproduce build
- inventory Runtime snapshots and dependencies
- compare Players
- document behavior and resource lifetime
- establish fixtures and risks

Exit: no major rewrite begins until unknowns and baseline blockers are documented.

Result: architecture, behavior, Runtime candidates, build, resource lifetime, risks, and missing fixtures are documented. Compatibility claims remain gated on real fixtures.

## UI Discovery Track — Quick Browse Prototype (completed 2026-07-26)

May begin after Phase 0 has captured v2 workflows. Use fake state only.

- validate open/drop → preview flow
- validate animation-first layout
- design empty/loading/ready/warning/unsupported/error states
- measure interactions to first visible animation

Result: a fake-state WPF shell validates a one-interaction open-to-preview path, required states, keyboard commands, stable automation IDs, and the collapsed 756 by 519 layout. Live Application and renderer integration remains deferred.

## Phase 1 — One Vertical Slice (completed 2026-07-26)

Use one Runtime line, provisionally 4.1:

```text
inspect asset → load → list animation/skin → render one PNG
```

Include a minimal machine-readable CLI. Do not add MCP yet.

Result: the isolated official 4.1 Runtime, project-owned contracts, CLI `inspect`/`render`, project-authored fixture, and deterministic CPU renderer spike are implemented. Compatibility remains limited to the recorded fixture conditions.

## Phase 2 — Compatibility Expansion

Add 4.0 and 3.8, then remaining lines based on fixture value and demand. Introduce Runtime registry and version detection only as needed by verified lines.

Progress: TASK-023 adds 4.0.64. TASK-024 adds isolated adapters, fixtures, explicit/automatic selection, and deterministic JSON renders for every remaining vendored line from 2.1.08 through 4.0.31. TASK-026 verifies the pinned official 3.8.55 JSON/binary/PMA example through the 3.8.95 adapter. TASK-027 adds bounded clipping and non-normal blend handling to the shared CPU bridge. TASK-028 verifies a project-authored two-page 4.1 atlas. Remaining feature-isolated and official editor-export parity is explicit; 4.2/4.3 have no vendored source.

## Phase 3 — Quick-Browse WPF Replacement

Connect the validated WPF shell to Application use cases:

- open/drop/recent
- auto-detect and fit
- animation/skin selection
- play/pause/loop/speed/seek
- viewport controls
- diagnostics and capture

Progress: TASK-005 connected real metadata opening, TASK-007 connected one deterministic static PPM frame, TASK-008 added bounded PNG texture decoding, TASK-010 added static fit, TASK-011 added atlas discovery, TASK-012 added WPF playback, TASK-013 added current-frame PNG capture, TASK-014 added bounded viewport pan/zoom and Fit, TASK-015 exposed actionable diagnostics, TASK-016 added deterministic PNG sequence export, TASK-017 added a customizable export FPS control, TASK-018 added bounded multi-skeleton scene layers, TASK-019 added per-layer animation and skin selection, TASK-020 added batch scene import and deterministic auto layout, TASK-029 validated WPF Browse/Inspector hide, float, redock, and reset-layout behavior, and TASK-030 corrected floated-panel bindings and removed prototype-only shell chrome. GIF/video/PSD export remains incomplete.

## Phase 4 — Export Replacement

- deterministic frame capture and bounded PNG sequence export with configurable FPS (TASK-013/TASK-016/TASK-017)
- PNG sequence
- GIF/video policy after v2 comparison
- cancellation, overwrite policy, progress

## Phase 5 — MCP Adapter

Implement local read-only MCP tools only after CLI and Application schemas are stable. Add write/compute tools later with path and resource limits.

## Release Gates

- Alpha: architecture and workflow may change
- Beta: primary functionality complete; compatibility gaps explicit
- RC: only release blockers accepted
- v3.0.0: replacement criteria and migration notes complete
