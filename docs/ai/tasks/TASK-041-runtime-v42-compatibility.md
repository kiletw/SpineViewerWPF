# TASK-041: Runtime 4.2 compatibility

## Status

Completed on 2026-08-08.

## Objective

Add an isolated Spine Runtime 4.2 adapter pinned to the official 4.2 branch
snapshot `b81e5a58ed38704aee4f866f0e0ac672623ce914`, then verify automatic and
explicit selection plus deterministic CPU and Runtime-neutral GPU-scene output
with official 4.2 JSON and binary example exports.

## Context

- `../04-runtime-matrix.md`
- `../05-technical-constraints.md`
- `../06-target-architecture.md`
- `../decisions/ADR-002-runtime-isolation.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `../decisions/ADR-009-opentk-gpu-preview.md`
- `../../runtimes/SpineRuntime.V41/Adapter.cs`
- Official upstream snapshot:
  `https://github.com/EsotericSoftware/spine-runtimes/tree/b81e5a58ed38704aee4f866f0e0ac672623ce914`

## Allowed Paths

- `runtimes/SpineRuntime.V42/**`
- project-owned shared bridges under `runtimes/SpineRuntime.V41/**`, excluding
  its vendored `src/**`
- `src/SpineViewerWPF.Application/**`
- `src/SpineViewerWPF.Cli/**`
- `src/SpineViewerWPF.Wpf/App.xaml.cs`
- `src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `tests/fixtures/v42-*/**`
- `scripts/test-v42.ps1`
- `scripts/test-v3.ps1`
- `docs/ai/**`

## Forbidden Paths

- existing vendored Runtime source under `runtimes/SpineRuntime.V41/src/**`
- legacy snapshots under `SpineViewerWPF/SpineLibrary/**`
- Runtime-specific types in Core or Application contracts
- Spine source writes
- new WPF, GPU, CLI, or MCP dependencies
- Runtime 4.3 implementation

## Required Behavior

- The 4.2 source snapshot is isolated, read-only in practice, license-preserved,
  and documented with its exact upstream commit.
- 4.2 JSON and binary exports can be inspected and rendered through the existing
  Core/Application contracts.
- Explicit `4.2` selection chooses only the 4.2 adapter; automatic selection
  selects it from the embedded export version without weakening mismatch checks.
- Physics constraints use the 4.2 Runtime update contract during absolute-time
  pose sampling.
- WPF and CLI composition include 4.2 without exposing Runtime types.
- Existing 2.1 through 4.1 selection and deterministic results remain unchanged.

## Acceptance Criteria

- Official 4.2 JSON and binary example assets report the expected version,
  animations, skins, and textures.
- Repeated deterministic renders produce identical PNG bytes.
- A 4.2 preview scene contains visible textured triangle geometry suitable for
  the existing GPU viewport.
- Application smoke covers automatic selection, explicit selection, and an
  incompatible override.
- Runtime matrix, context index, migration plan, and fixture evidence are updated.

## Validation

```powershell
dotnet build .\runtimes\SpineRuntime.V42\SpineRuntime.V42.csproj -c Release
dotnet build .\src\SpineViewerWPF.Wpf\SpineViewerWPF.Wpf.csproj -c Release
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v42.ps1 -Offline
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Completion Report

### Changed

- Added `SpineRuntime.V42`, pinned to official commit
  `b81e5a58ed38704aee4f866f0e0ac672623ce914`, with the official Runtime
  license and a source tree that has zero mismatches against upstream.
- Added the isolated 4.2 Adapter to CLI and WPF composition while reusing the
  existing Runtime-neutral CPU frame and GPU-scene contracts.
- Added bounded deterministic Physics replay: 60 Hz through 10 seconds and a
  maximum of 600 evenly distributed updates for longer target times.
- Added a project-authored Physics fixture, Application smoke coverage, the
  official 4.2.22 JSON/binary cache script, and recorded deterministic hashes.

### Preserved

- Core and Application contracts expose no Spine Runtime or GPU types.
- Existing Runtime source snapshots, including `SpineRuntime.V41/src`, were not
  edited; the shared bridge only gained compile-time 4.2 namespace and clipping
  overload selection.
- Existing 2.1 through 4.1 selection, deterministic 4.1 output, official 4.1
  JSON/binary output, and official 3.8 JSON/binary/PMA output remain unchanged.
- Official example binaries and images remain in the gitignored cache.

### Validation

- V42 and WPF Release builds: passed with 0 warnings and 0 errors.
- Application smoke: passed, including auto/explicit selection, incompatible
  override, Physics effect, Runtime-neutral triangles, and A-B-A determinism.
- `scripts/test-v42.ps1 -Offline`: passed for official 4.2.22 JSON and binary;
  render SHA-256 values are recorded under `evidence/task-041`.
- `scripts/test-v3.ps1`: passed all connected lines and the new 4.2 baseline.
- Official 4.1 and 3.8 offline suites: passed.
- Fixture manifest JSON parse and `git diff --check`: passed.

### Gaps and Risks

- Physics sampling after 10 seconds is a bounded approximation. Exact long
  timeline sampling would require cached deterministic checkpoints or a new
  sequential playback contract.
- Official 4.2 PMA, multi-page, and feature-isolated clipping/blend exports have
  not yet been verified; existing shared renderer behavior remains available.

### Documentation

- Updated the context index, Runtime matrix, migration plan, CLI contract,
  fixture manifest, and TASK-041 evidence.

### Recommended Next Task

- Add Runtime 4.3 as its own pinned adapter and compatibility task; do not infer
  4.3 support from the 4.2 adapter.
