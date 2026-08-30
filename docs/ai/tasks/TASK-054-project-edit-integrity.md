# TASK-054: Preserve project edit integrity

Status: Completed on 2026-08-31.

## Objective

Ensure saving a Viewer project never changes layer edits and unsupported
structural edits cannot leave Undo/Redo with an incorrect clean state.

## Context

- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/decisions/ADR-007-non-destructive-viewer-project.md`
- Adjacent review of `codex/runtime-4.3` identified save-time primary-layer
  synchronization and structure/history mismatch as data-integrity risks.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `docs/ai/00-context-index.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- this task document

## Forbidden Paths

- Core, Application, Runtime, renderer, export, and project schema changes
- `runtimes/**` and vendored upstream code
- full structural Undo/Redo implementation

## Required Behavior

- Save serializes current layer data without mutating any layer.
- Legacy top-level project fields mirror the current primary layer when one
  exists, while loop, playback speed, and background remain document settings.
- Add, duplicate, remove, reorder, and auto-layout invalidate incompatible
  Undo/Redo history until structural snapshots are supported.
- A prior property Undo cannot mark an unsaved structurally edited project clean.
- Sidecar-only saving and source-asset immutability are preserved.

## Acceptance Criteria

- Regression tests cover save after direct primary-layer edits.
- Regression tests cover a property edit followed by structural mutation and
  verify Undo cannot restore a false clean state.
- WPF Release build, Application smoke, UI shell validation, and diff check pass.
- Documentation records changed and preserved behavior.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

### Changed

- Save now captures the scene-layer documents once and derives legacy top-level
  fields from the current primary layer without modifying the live layer.
- Add, duplicate, remove, reorder, and auto-layout clear Undo/Redo history before
  marking the project dirty because structural snapshots are not implemented.
- Application smoke covers save-time primary-layer preservation and history
  invalidation after layer import, auto-layout, and reorder.

### Preserved

- Viewer schema version 1, sidecar-only saving, source asset immutability,
  property-level Undo/Redo, Runtime adapters, renderers, and export are unchanged.

### Validation

- Isolated WPF Release build: passed with 0 warnings and 0 errors.
- Application smoke: passed, including the new project edit-integrity cases.
- UI shell validation: passed with 85 AutomationIds, 8 editor shortcuts, 24
  compact-workspace tokens, Slots availability, and Duplicate layer interaction.
- `git diff --check`: passed; Git only reported the repository's expected LF to
  CRLF checkout notice.

### Gaps and risks

- Structural operations deliberately cannot be undone yet. They clear existing
  history to prevent stale snapshots from restoring a false clean state.
