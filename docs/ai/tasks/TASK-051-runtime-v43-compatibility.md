# TASK-051: Runtime 4.3 compatibility

## Status

Completed on 2026-08-27.

## Objective

Add an isolated Spine Runtime 4.3 adapter pinned to official branch snapshot
`de14116488688c27c01b6e2b61fe1544792af2dd`, then verify automatic and
explicit selection plus deterministic CPU and Runtime-neutral GPU-scene output
with the official 4.3 Spineboy JSON and binary exports.

## Context

- `../04-runtime-matrix.md`
- `../05-technical-constraints.md`
- `../06-target-architecture.md`
- `../decisions/ADR-002-runtime-isolation.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `../decisions/ADR-009-opentk-gpu-preview.md`
- `TASK-041-runtime-v42-compatibility.md`
- `../../runtimes/SpineRuntime.V42/Adapter.cs`
- Official upstream snapshot:
  `https://github.com/EsotericSoftware/spine-runtimes/tree/de14116488688c27c01b6e2b61fe1544792af2dd`

## Allowed Paths

- `runtimes/SpineRuntime.V43/**`
- project-owned shared bridges under `runtimes/SpineRuntime.V41/**`, excluding
  its vendored `src/**`
- `src/SpineViewerWPF.Cli/**`
- `src/SpineViewerWPF.Wpf/App.xaml.cs`
- `src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `tests/fixtures/v43-*/**`
- `scripts/test-v43.ps1`
- `scripts/test-v3.ps1`
- `SpineViewerWPF.sln`
- `docs/ai/**`

## Forbidden Paths

- existing vendored Runtime source under every other `runtimes/**/src/**`
- legacy snapshots under `SpineViewerWPF/SpineLibrary/**`
- Runtime-specific types in Core or Application contracts
- Spine source writes
- new WPF, GPU, CLI, or MCP dependencies
- unrelated UI or export behavior changes

## Required Behavior

- The 4.3 source snapshot is isolated, read-only in practice, license-preserved,
  and documented with its exact upstream commit.
- 4.3 JSON and binary exports can be inspected and rendered through the existing
  Core/Application contracts.
- Explicit `4.3` selection chooses only the 4.3 adapter; automatic selection
  selects it from the embedded export version without weakening mismatch checks.
- Runtime 4.3 pose state, attachment selection, slot visibility/opacity, Track 0
  alpha, clipping, and Physics are translated through project-owned bridges.
- WPF and CLI composition include 4.3 without exposing Runtime types.
- Existing 2.1 through 4.2 selection and deterministic results remain unchanged.

## Acceptance Criteria

- The official 4.3 Spineboy JSON and binary examples report export version
  `4.3.75-beta`, the expected animations and skins, and one atlas texture.
- Repeated deterministic renders produce identical PNG bytes.
- A 4.3 preview scene contains visible textured triangle geometry suitable for
  the existing GPU viewport.
- Application smoke covers automatic selection, explicit selection, an
  incompatible override, slot controls, and same-session deterministic replay.
- The vendored `spine-csharp/src` tree matches the pinned upstream snapshot with
  no project-authored patches.
- Runtime matrix, context index, migration plan, fixture evidence, and task
  completion report are updated.

## Validation

```powershell
dotnet build .\runtimes\SpineRuntime.V43\SpineRuntime.V43.csproj -c Release
dotnet build .\src\SpineViewerWPF.Wpf\SpineViewerWPF.Wpf.csproj -c Release
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v43.ps1 -Offline
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check HEAD -- . ':(exclude)runtimes/SpineRuntime.V43/src/**'
```

## Completion Report

### Changed

- Added `SpineRuntime.V43`, pinned to official commit
  `de14116488688c27c01b6e2b61fe1544792af2dd` (official C# package
  `4.3.39`), with the official Runtime license and all 139 upstream
  `spine-csharp/src` paths.
- Added the isolated 4.3 adapter to CLI and WPF composition while reusing the
  existing Runtime-neutral CPU frame and textured-triangle scene contracts.
- Translated the 4.3 pose APIs, named attachment selection, slot visibility and
  opacity, Track 0 Alpha, clipping, and bounded deterministic Physics replay
  through project-owned adapter and renderer bridges.
- Added a project-authored 4.3 Physics fixture, Application smoke coverage,
  the official 4.3.75-beta JSON/binary cache script, and recorded deterministic
  render hashes.

### Preserved

- Core and Application contracts expose no Spine Runtime, WPF, or GPU types.
- No existing vendored Runtime source was edited; 4.3-specific compile-time
  branches are confined to project-owned bridges outside the upstream source
  directories.
- Existing 2.1 through 4.2 selection and deterministic fixture results remain
  unchanged, including the established 4.1 64 by 64 render baseline.
- CPU rendering remains authoritative for deterministic capture and export;
  the WPF GPU viewport continues to consume Runtime-neutral triangle scenes and
  retain CPU fallback behavior.
- Official example binaries and images remain in the gitignored cache.

### Validation

- V43 and WPF Release builds passed with 0 warnings and 0 errors.
- Application smoke passed, including automatic and explicit selection,
  incompatible 4.2 override, Physics effect, Runtime-neutral textured
  triangles, attachment selection, slot visibility and opacity, Track 0 Alpha,
  and same-session A-B-A determinism.
- `scripts/test-v43.ps1 -Offline` passed for official 4.3.75-beta JSON
  and binary exports, including deterministic `portal` renders that activate
  clipping. Render SHA-256 values are recorded under `evidence/task-051`.
- `scripts/test-v3.ps1` passed all connected Runtime lines and the new 4.3
  baseline (`7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E`).
- The upstream source inventory has 139 paths with no missing or extra paths;
  source text matches the pinned snapshot with no project-authored code patch.
- The project-authored diff passed the whitespace check. Upstream `.meta`
  trailing spaces remain byte-identical to the pinned source and are covered by
  the 139-blob integrity comparison instead of being rewritten locally.

### Gaps and Risks

- The pinned official example reports export `4.3.75-beta` even though the
  selected 4.3 branch and C# package are stable; support is intentionally tied
  to the exact commit rather than inferred from the label.
- Physics sampling after 10 seconds remains a bounded approximation. Exact long
  timeline sampling would require deterministic checkpoints or a sequential
  playback contract.
- Official 4.3 PMA, feature-isolated non-normal blend parity, multi-page exports,
  and broader user-export coverage remain unverified.
- Git checkout line-ending normalization can change the working bytes of two
  upstream metadata files without changing their text; future integrity checks
  should compare Git-normalized blobs or normalized text as documented in the
  evidence page.

### Documentation

- Updated the context index, Runtime matrix, migration plan, CLI contract,
  fixture manifest, TASK-051 evidence, and this completion report.

### Recommended Next Task

- Prioritize user-supplied 4.3 real-asset acceptance; add PMA or multi-page
  feature fixtures only when a real compatibility case requires them.
