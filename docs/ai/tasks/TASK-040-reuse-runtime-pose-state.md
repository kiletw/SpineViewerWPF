# TASK-040: Reuse Runtime pose state inside an asset session

## Status

Completed

## Objective

Remove repeated Skeleton graph and AnimationStateData construction from
interactive GPU scene sampling while preserving absolute-time deterministic
rendering across every connected Runtime line.

## Context

- TASK-039 measured stable GPU rasterization but identified Runtime pose and
  geometry preparation as the remaining per-frame CPU work.
- `AssetRenderSession` already serializes calls, so one cached pose object per
  Runtime session does not add a new synchronization surface.
- ADR-005, ADR-008, and ADR-009 keep deterministic CPU output authoritative.

## Allowed Paths

- `runtimes/SpineRuntime.Legacy/Adapter.cs`
- `runtimes/SpineRuntime.V40/Adapter.cs`
- `runtimes/SpineRuntime.V41/Adapter.cs`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `scripts/test-user-playback-metrics.ps1`
- `docs/ai/**`

## Forbidden Paths

- vendored official Runtime source under `runtimes/SpineRuntime.V41/src/**`
- vendored v2 Runtime snapshots under `SpineViewerWPF/SpineLibrary/**`
- Core, Application, WPF, CLI, and MCP production code
- deterministic renderer and encoder implementation

## Required Behavior

- A Runtime render session constructs its Skeleton and AnimationStateData once.
- Every request resets skin, setup pose, draw order, constraints, slots, and
  attachments before applying the requested absolute animation time.
- Moving forward or backward in time, switching skin, changing Track Alpha,
  and applying slot display settings do not leak state between requests.
- Capture/export hashes and Runtime selection remain unchanged.

## Acceptance Criteria

- A same-session A-B-A render sequence reproduces the first A frame exactly.
- The project-authored, v3, official 3.8, and official 4.1 suites remain green.
- The 3.6.53 and 4.1.14 user assets remain GPU-backed without sustained
  coalescing (at most one merged timer update per sample window).
- No new dependency or public contract is added.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v38.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v41.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-user-playback-metrics.ps1
git diff --check
```

## Completion Report

### Changed

- Legacy, 4.0, and 4.1 render sessions now construct one Skeleton and one
  AnimationStateData, then reset and reuse them for absolute-time requests.
- Added a same-session A-B-A regression with an intervening time, skin mode,
  Track Alpha, and hidden-slot change.

### Preserved

- A new AnimationState is still used per request, so mixing/event history does
  not become part of deterministic capture or export.
- Core/Application contracts, WPF behavior, Runtime selection, texture lifetime,
  CPU rasterization, encoding, and official Runtime source are unchanged.

### Validation

- WPF Release build and Application smoke: passed, 0 warnings and 0 errors.
- v3 deterministic suite: passed with SHA-256
  `7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E`.
- Official 3.8 and 4.1 offline JSON/binary renders: passed.
- `marianne` 3.6.53 and `xiu` 4.1.14: GPU, 39.7-40.5 FPS,
  1.0-3.6 ms work, and 0-1 coalesced update per sample window.
- `git diff --check`: passed.

### Gaps and Risks

- The sampled work time stayed in the same low single-digit range as TASK-039;
  this change removes known object construction but does not claim a measurable
  wall-clock improvement on these two assets.
- Per-attachment geometry arrays and the separate bounds pass now dominate the
  remaining Runtime-side CPU work.

### Documentation

- Updated the context index, migration plan, and TASK-040 local evidence.

### Recommended Next Task

- Remove the duplicate bounds/world-vertex pass in `BuildPreviewScene` while
  preserving every deterministic and real-asset result.
