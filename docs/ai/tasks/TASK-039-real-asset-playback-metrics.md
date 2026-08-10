# TASK-039: Real-asset playback performance metrics

## Status

Completed

## Objective

Make playback smoothness diagnosable on the user-supplied 3.6.53 and 4.1.14
assets by exposing bounded live cadence, render-time, and coalesced-frame metrics
without changing deterministic capture/export output.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/**`
- `scripts/**`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `docs/ai/**`

Project-owned Runtime adapters, Core, Application, vendored Runtime source, and
deterministic renderer output are read-only for this task.

## Required Behavior

- The workspace identifies GPU or CPU fallback as before.
- Live playback reports an actual published/presented FPS, recent render time,
  and coalesced or dropped work count at a low UI update rate.
- Metrics reset when playback or asset state resets and do not dirty the Viewer
  project.
- Normal GPU playback still avoids CPU pixel rasterization.
- The user-supplied `xiu.skel` 4.1.14 and
  `illust_r_2110601_marianne01_01.skel` 3.6.53 remain loadable.

## Assumptions

- `D:\Spine測試` remains the local non-redistributable acceptance location.
- A successful metadata load does not prove visual or timing parity; timing
  evidence is recorded separately.

## Validation

- WPF Release build
- Application smoke
- UI shell automation
- v3 deterministic suite
- official 3.8 and 4.1 offline suites
- user-asset inspect and bounded playback evidence
- `git diff --check`

## Result

- Fixed preview starvation by retaining one active render plus one pending update
  instead of invalidating every result that exceeded the 16 ms timer interval.
- Fixed the OpenGL viewport uniform type mismatch that forced every real asset
  into CPU fallback, and stopped continuously redrawing unchanged GPU scenes.
- Added low-rate published FPS, average preview work time, and coalesced-update
  status without changing project dirty state or deterministic capture/export.
- Added `--asset=<path>` for deterministic startup acceptance and a bounded
  user-asset playback script.
- Verified `marianne` 3.6.53 and `xiu` 4.1.14 on GPU at approximately 40 FPS,
  1.3-3.2 ms preview work, and zero coalesced updates in the final sample.

## Validation Result

- WPF Release build: passed, 0 warnings and 0 errors.
- Application smoke and UI shell automation: passed.
- v3 deterministic suite: passed; SHA-256 baseline unchanged.
- Official 3.8 and 4.1 offline suites: passed.
- User-asset inspect and three-second GPU playback samples: passed.
- `git diff --check`: passed.

## Remaining Limits

- The metric is published preview cadence, not monitor presentation timing.
- Runtime adapters still rebuild pose/geometry data on the CPU for each GPU
  scene; persistent Runtime state and geometry allocation reduction remain a
  separate optimization task.
