# TASK-024: Complete historical Runtime adapters

## Status

Completed 2026-07-28

## Scope

Add isolated v3 Runtime projects for every vendored historical snapshot not yet
connected to the shared Application, CLI, and WPF composition roots:

- 2.1.08, 2.1.25
- 3.1.07, 3.2.xx, 3.4.02, 3.5.51
- 3.6.32, 3.6.39, 3.6.53, 3.7.94, 3.8.95
- 4.0.31

Each line must load a project-authored JSON fixture, expose metadata through
`IRuntimeAdapter`, and render the minimal region fixture through the existing
deterministic CPU path. Official binary and production-texture parity remains
explicitly unverified when no repository fixture exists.

## Allowed paths

- `runtimes/SpineRuntime.V21*`, `V31`, `V32`, `V34`, `V35`, `V36*`, `V37`, `V38`, `V40_31`
- project-owned shared bridge files under `runtimes/SpineRuntime.V41/`
- Application/CLI/WPF composition files and smoke tests
- `tests/fixtures/`, `scripts/`, and relevant `docs/ai/` files

## Forbidden paths

- `SpineViewerWPF/SpineLibrary/**` (vendored snapshots are read-only)
- `runtimes/SpineRuntime.V41/src/**`
- new NuGet packages or WPF/GPU dependencies in Core/Application

## Acceptance

- every listed vendored line builds in an isolated project;
- automatic and explicit runtime selection are test-covered;
- every fixture passes inspect and deterministic render smoke checks;
- CLI and WPF composition roots include all adapters;
- runtime matrix, fixture manifest, migration/context/CLI docs are updated;
- required repository validation commands pass with no new warnings.

## Completion report

### Changed

- Added isolated projects for 2.1.08, 2.1.25, 3.1.07, 3.2.xx, 3.4.02,
  3.5.51, 3.6.32, 3.6.39, 3.6.53, 3.7.94, 3.8.95, and 4.0.31.
- Added one shared project-owned legacy adapter and conditional CPU/PNG bridge;
  vendored source remains linked read-only.
- Added fixture-backed inspect/render coverage, CLI/WPF composition, fixture
  manifest hashes, runtime matrix status, and stale-process cleanup in the UI
  smoke script.

### Preserved

- Existing 4.0.64 and 4.1 behavior and hashes remain unchanged.
- Core/Application contracts still expose no Runtime-specific types.
- Spine source files and original handoff files were not edited.

### Validation

- all 14 Runtime projects build Release with zero warnings/errors;
- Application smoke passes (automatic + explicit historical selection,
  deterministic renders, WPF/project behavior);
- `scripts/test-v3.ps1` passes 4.1, 4.0.64, and all 12 historical fixtures;
- `scripts/test-ui-shell.ps1` passes (39 automation IDs, 4 shortcuts, launch);
- `scripts/test-official-v41.ps1 -Offline` passes JSON/binary inspect/render;
- `git diff --check` and fixture manifest JSON parse pass.

### Known gaps / risks

- Historical fixtures are project-authored JSON only; official editor exports,
  binary parity, PMA, clipping, non-normal blend modes, multi-page atlases, and
  production texture formats are not claimed.
- 4.2/4.3 remain unsupported because no vendored source snapshot exists.

### Recommended next task

Add legally redistributable official fixture caches and binary/texture parity
tests for the highest-demand historical line, beginning with 3.8.95.

## Validation notes

- The Application smoke keeps async continuations on one WPF Dispatcher and
  waits up to two seconds for an observed playback tick.
