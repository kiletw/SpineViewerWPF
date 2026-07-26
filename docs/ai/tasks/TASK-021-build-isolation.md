# TASK-021: Isolate Concurrent Build Outputs

## Status

Completed on 2026-07-27.

## Objective

Prevent concurrent validation builds from sharing MSBuild `obj` and output files, which has previously produced intermittent file-lock errors and visible build error dialogs.

## Context

- `../09-ai-working-rules.md`
- `TASK-014-viewport-navigation.md` (records the shared `obj` lock)
- `scripts/test-ui-shell.ps1`
- `scripts/test-v3.ps1`
- `scripts/test-official-v41.ps1`

## Allowed Paths

- `scripts/**`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- product source or package changes

## Required Behavior

- Each script-owned build waits on a shared named MSBuild mutex.
- Concurrent validation scripts do not write the shared `obj` tree at the same time.
- Existing build and render behavior remains unchanged.

## Acceptance Criteria

- Repeated UI and CLI validations pass without shared `obj`/`bin` locks.
- Existing application, v3, official-runtime, and diff checks remain green.
- No Runtime source or package changes.

## Validation

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-official-v41.ps1 -Offline
git diff --check
```

## Completion Report

Changed: `test-ui-shell.ps1`, `test-v3.ps1`, and `test-official-v41.ps1` now serialize their MSBuild phase with the named `SpineViewerWPF.Build` mutex, including abandoned-owner recovery and a five-minute timeout. Concurrent smoke scripts therefore queue instead of touching the shared `obj` tree simultaneously.

Preserved: output locations, UI automation checks, CLI inspect/render contracts, official Runtime fixture checks, and all product/runtime source behavior.

Validation: application smoke passed; WPF shell passed with 38 automation IDs and 4 shortcuts; v3 deterministic render passed with SHA-256 `7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E`; official 4.1 offline checks passed; concurrent UI/v3 execution passed; `git diff --check` passed.

Risks: the lock is process-local to this Windows session and only covers the validation scripts' MSBuild phase; external IDE builds are unaffected and should continue using their own build coordination.
