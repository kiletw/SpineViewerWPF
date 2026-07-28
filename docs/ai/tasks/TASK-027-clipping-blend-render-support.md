# TASK-027: CPU Clipping and Blend Support

## Status

Completed 2026-07-28.

## Objective

Close the next renderer gap exposed by the official 3.8/4.1 example assets:
consume runtime clipping attachments and map the runtime's normal, additive,
multiply, and screen slot blend modes in the project-owned deterministic CPU
bridge.

## Scope

- keep all vendored Runtime snapshots read-only;
- use each Runtime's existing `SkeletonClipping` API where it exists;
- compile the same bridge across the historical API shapes (including the
  3.6.32/3.6.39 `IsClipping()` method versus later `IsClipping` property);
- preserve premultiplied-alpha handling and the existing PNG baselines.

## Non-goals

- no GPU renderer or production render-backend decision;
- no claim of pixel parity with Spine's platform renderers;
- no clipping implementation for snapshots that do not ship clipping support;
- no committed official binary or texture assets.

## Completion report

### Changed

- `runtimes/SpineRuntime.V41/CpuRenderer.cs` now starts/ends clipping regions,
  clips region and mesh triangles, and maps supported slot blend modes through
  one shared project-owned path.
- The bridge keeps the older no-clipping/no-blend snapshots on their existing
  compile-time paths.
- The straight-alpha 8-bit output remains deterministic; the blend equations
  are intentionally bounded for the CPU spike.

### Preserved

- all historical fixture hashes;
- official 3.8 JSON/binary/PMA render hashes;
- official 4.1 JSON/binary render hashes;
- Runtime/Application boundaries and source isolation.

### Validation

- all 14 Runtime projects build with zero warnings/errors;
- `scripts/test-v3.ps1` passes (`HistoricalRuntimes: 12`, PMA passed);
- `scripts/test-official-v38.ps1 -Offline` passes inspect, automatic
  selection, JSON/binary/PMA renders;
- `scripts/test-official-v41.ps1 -Offline` passes JSON/binary renders;
- Application smoke and `scripts/test-ui-shell.ps1` pass.

### Known gaps

- Full GPU blend-equation parity, multi-page atlases, interlaced/non-8-bit
  textures, and visual parity against Spine's official renderers remain open.
- The official spineboy caches contain clipping and additive definitions, but
  these smoke poses do not serve as isolated per-feature visual fixtures.
