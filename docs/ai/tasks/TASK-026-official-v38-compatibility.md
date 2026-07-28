# TASK-026: Official 3.8.95 Compatibility Smoke

## Status

Completed 2026-07-28.

## Objective

Verify the vendored 3.8.95 adapter against a pinned official Spine Runtime
example cache without committing redistributable binary assets.

## Scope

- pin official Runtime 3.8 commit `8b4844bd4b193ba9e54487ed397a777993cbad56`;
- cache the official spineboy JSON, binary, atlas, PNG, PMA atlas/PNG, and
  license through `scripts/test-official-v38.ps1`;
- verify explicit and automatic 3.8.95 selection, JSON/binary inspect, normal
  PNG render, and PMA PNG render;
- accept the official export patch version `3.8.55` under the 3.8.95 adapter.

## Non-goals

- no vendored Runtime source edits;
- no committed official binary or texture files;
- no claim of full clipping, additive blend, or production renderer parity.

## Completion report

### Changed

- Added an offline-capable official v3.8 cache/download smoke script with blob
  and SHA-256 verification.
- Relaxed only the 3.8 adapter version guard from exact patch matching to the
  3.8 minor line so the official 3.8.55 export is accepted by Runtime 3.8.95.
- Recorded source/file/render hashes in the fixture manifest and compatibility
  matrix.

### Preserved

- all existing historical, 4.0.64, and 4.1 baselines;
- source isolation, Runtime/Application boundaries, and license handling.

### Validation

- `scripts/test-official-v38.ps1 -Offline` passes JSON inspect, binary inspect,
  automatic selection, normal JSON/binary renders, and PMA render.
- CLI and V38 Runtime Release builds pass with zero warnings/errors.
- Existing Application smoke and v3 runtime tests remain passing.

### Known gaps

- The official 3.8 example contains clipping/additive content; the current CPU
  bridge intentionally remains a bounded deterministic spike and does not claim
  full visual parity for those features.
- Official caches stay local under `artifacts/` and are not committed.

### Recommended next task

Use the same pinned-cache pattern for the highest-demand remaining line, then
implement clipping and non-normal blend support in the renderer.
