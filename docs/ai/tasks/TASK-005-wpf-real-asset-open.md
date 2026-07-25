# TASK-005: WPF Real Asset Open

## Status

Completed on 2026-07-26 for the project-authored 4.1 fixture.

## Objective

Replace the WPF Open command's fake asset transition with the existing Application inspect use case and isolated 4.1 Runtime adapter, while keeping preview rendering fake and source files read-only.

## Context

- `../03-behavior-contracts.md`
- `../04-runtime-matrix.md`
- `../05-technical-constraints.md`
- `../12-application-use-cases.md`
- `TASK-001-v3-vertical-slice.md`
- `TASK-004-editable-project-session.md`
- `../decisions/ADR-002-runtime-isolation.md`
- `../decisions/ADR-007-non-destructive-viewer-project.md`

## Allowed Paths

- `src/SpineViewerWPF.Wpf/**`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- `src/SpineViewerWPF.Core/**`
- `src/SpineViewerWPF.Application/**`
- legacy `SpineViewerWPF/**`
- production renderer selection or implementation
- Spine source writing

## Required Behavior

- select `.json` or `.skel` with the native Windows file dialog
- discover a same-basename `.atlas` through the shared Application service
- inspect off the WPF UI thread
- map real skeleton path, atlas path, Runtime line, animations, skins, durations, and diagnostics into presentation state
- save real source references into the sidecar
- keep the prior document data until a replacement load succeeds
- require confirmation before discarding dirty project settings
- present unsupported and failed loads without crashing

## Acceptance Criteria

- the committed 4.1 fixture opens through the WPF ViewModel and isolated adapter
- animation `move`, skin `default`, Runtime `4.1`, and duration `1` reach presentation state
- missing and unsupported input become deterministic UI states
- Runtime-specific types remain confined to the WPF composition root
- TASK-002 automation identifiers and TASK-004 editing/save behavior remain valid
- no new package is added

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Completion Report

Changed: native JSON/SKEL picker, dirty-discard guard, off-thread Application inspect, real metadata presentation mapping, real source references in sidecar save, and real reload.

Preserved: fake preview renderer, TASK-002 presentation states and automation identifiers, TASK-004 editing/save behavior, Runtime isolation, and read-only source assets.

Validation: real 4.1 fixture mapping, dirty replacement cancellation, missing/unsupported states, prior-document preservation, WPF build/launch, native picker visual QA, CLI/runtime regression, and `git diff --check`.

Known gaps: TASK-006 later verified PNG metadata inspection and TASK-007 added a deterministic static PPM preview, but PNG rendering, interactive playback, editor-export fixtures, binary input, explicit atlas selection, drag/drop, and sidecar opening remain unverified or unimplemented.

Documentation: context index, Runtime matrix, UI state contracts, and TASK-004 known gaps updated.
