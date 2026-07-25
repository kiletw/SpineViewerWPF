# TASK-004: Editable Viewer Project Session

## Status

Completed on 2026-07-26 with fake-session editing and a versioned sidecar store.

## Objective

Prove a safe, non-destructive edit-and-save workflow by storing viewer settings in a versioned `*.spineviewer.json` sidecar without modifying Spine JSON, binary, atlas, or texture sources.

## Context

- `../05-technical-constraints.md`
- `../12-application-use-cases.md`
- `../14-ui-product-design.md`
- `../15-ui-information-architecture.md`
- `../decisions/ADR-004-ui-architecture-and-shell.md`
- `../decisions/ADR-006-dockable-workspace-and-localization-boundary.md`

## Allowed Paths

- `src/SpineViewerWPF.Core/**`
- `src/SpineViewerWPF.Application/**`
- `src/SpineViewerWPF.Wpf/**`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- legacy `SpineViewerWPF/**`
- source Spine JSON, binary, atlas, and texture files
- docking package selection or implementation
- animation keyframe, bone, mesh, slot, or attachment source editing

## Required Behavior

- edit fake-session transform, skin, animation, playback, and background settings through an Inspector
- show dirty state and support Undo/Redo
- Save and Save As only to `*.spineviewer.json`
- write sidecars through an Application service using a temporary file in the destination directory
- reject unsupported schema versions and unsafe numeric values when loading
- preserve the TASK-002 quick-browse states and automation identifiers

## Acceptance Criteria

- Ctrl+S, Ctrl+Shift+S, Ctrl+Z, and Ctrl+Y are wired
- successful save clears dirty state; failed save preserves it
- project JSON round-trips all editable fields
- no Runtime-specific type crosses into Core, Application, or WPF
- source Spine files remain untouched
- no new package is added

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
git diff --check
```

## Completion Report

Changed: project-owned document contract, sidecar store, Inspector, preview transforms, dirty state, Undo/Redo, Save/Save As, close confirmation, resource-keyed editor labels, smoke coverage, and UI automation checks.

Preserved: TASK-002 states and automation identifiers, Runtime isolation, CLI behavior, and all Spine source files.

Validation: Application smoke, WPF shell script, CLI/runtime regression script, visual QA, and `git diff --check`.

Known gaps: the shell still uses fake asset data; sidecar opening, autosave/recovery, multi-document tabs, real Runtime session editing, docking, and Spine source writing are not implemented.

Documentation: ADR-007, Application use cases, UI product direction, UI state contracts, and context index updated.
