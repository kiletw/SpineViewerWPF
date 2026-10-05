# TASK-025: PMA CPU Render Support

## Status

Completed 2026-07-28.

## Objective

Accept premultiplied-alpha render requests in the shared deterministic CPU
renderer without changing the existing opaque-texture baselines.

## Scope

- keep the existing `RenderRequest.Pma` contract;
- use premultiplied RGB contribution during CPU compositing;
- cover the flag through Application smoke and CLI v3 smoke;
- leave official PMA asset parity as a separate fixture task.

## Non-goals

- no vendored Runtime source edits;
- no new image dependency;
- no claim of official PMA visual parity without an official PMA fixture.

## Completion report

### Changed

- CPU renderer now accepts `--pma` and avoids multiplying premultiplied RGB by
  source alpha a second time.
- Application smoke renders a PMA request and CLI smoke verifies the opaque
  baseline remains unchanged.

### Preserved

- existing 4.0.64, 4.1, and historical render hashes;
- Runtime/Application boundaries and read-only vendored sources.

### Validation

- all Runtime projects build with zero warnings/errors;
- Application smoke passes;
- `scripts/test-v3.ps1` reports `Pma = passed`;
- `git diff --check` passes.

### Known gaps

- An official PMA spineboy cache is still needed to verify real premultiplied
  texture pixels, clipping, and multi-page atlas behavior.

### Recommended next task

Add a pinned official 3.8/3.8.95 fixture cache and binary/PMA smoke using the
existing offline cache pattern.
