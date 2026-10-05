# TASK-007: WPF Static Preview

## Status

Completed on 2026-07-26 for the verified 4.1 PPM fixture.

## Objective

Replace the WPF shell's fake figure with one real, deterministic frame for the verified 4.1 PPM fixture by reusing the existing Application render use case and CPU renderer.

## Context

- `../03-behavior-contracts.md`
- `../05-technical-constraints.md`
- `../14-ui-product-design.md`
- `../16-ui-state-and-interaction-contracts.md`
- `TASK-001-v3-vertical-slice.md`
- `TASK-005-wpf-real-asset-open.md`
- `TASK-006-png-metadata-inspect.md`
- `../decisions/ADR-004-ui-architecture-and-shell.md`
- `../decisions/ADR-005-deterministic-cpu-renderer-spike.md`

## Allowed Paths

- `src/SpineViewerWPF.Wpf/**`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- `src/SpineViewerWPF.Core/**`
- `src/SpineViewerWPF.Application/**`
- `src/SpineViewerWPF.Cli/**`
- official Runtime source
- production renderer or docking toolkit selection
- PNG pixel decoding
- new packages

## Required Behavior

- inspect and render off the WPF UI thread
- render the selected first animation at its midpoint to a unique temporary 64 by 64 PNG
- show the rendered PNG instead of the prototype figure after a successful real open
- preserve metadata and enter `RendererUnavailable` when the existing CPU renderer rejects a texture or attachment
- keep prototype state previews available for UI-state validation
- delete superseded and closing-window preview files

This task explicitly changes the TASK-006 WPF outcome for PNG-backed assets from `Ready` with a fake figure to `RendererUnavailable` with preserved metadata. PNG inspection remains successful; PNG rendering remains unsupported.

## Acceptance Criteria

- the committed 4.1 PPM fixture reaches `Ready` with a real rendered preview path
- preview bytes have a PNG signature and match the existing deterministic render hash
- the generated preview is deleted when the ViewModel is disposed
- the generated PNG metadata fixture reaches `RendererUnavailable` without losing animation, skin, Runtime, or save behavior
- failed replacement preserves the prior document and preview
- Runtime, Core, Application, CLI, and source assets remain unchanged
- no package is added

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release -p:BaseOutputPath=.\artifacts\application-smoke\bin\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Completion Report

Changed: real WPF opens now inspect and render off the UI thread, display the deterministic midpoint PNG through an on-load WPF image converter, retain prototype state visuals, and delete temporary previews when replaced or closed.

Preserved: Core, Application, CLI, Runtime adapter and official source, source assets, project editing/save behavior, fake state validation, and the recorded deterministic render hash.

Validation: the WPF smoke path verifies the real preview path, PNG loading, exact SHA-256, failed-replacement retention, disposal cleanup, and PNG renderer fallback; UI shell and CLI regressions pass with zero warnings or errors.

Known gaps at completion: this was one static 64 by 64 frame. TASK-008 later added bounded PNG pixel decoding; TASK-010 added static fit; TASK-012 added WPF playback; TASK-013 added current-frame PNG capture. Sequence export, unsupported CPU-renderer features, and production renderer selection remain incomplete.

Risks: unverified real assets may clip because the renderer has no bounds-based fit. Preview file deletion is best-effort if another process holds the file.

Documentation: context index, Runtime matrix, migration plan, UI state contract, and prior task known gaps updated.

Recommended next task: add bounds-aware static fit or PNG decoding only after choosing which limitation blocks the first real user fixture.
