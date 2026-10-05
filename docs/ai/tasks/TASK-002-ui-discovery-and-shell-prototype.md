# TASK-002: Quick-Browse UI Discovery and Shell Prototype

## Status

Completed on 2026-07-26 with fake presentation state. TASK-001 remained isolated; the shell contains no Runtime, renderer, or export implementation.

## Objective

Validate the shortest understandable path from open/drop to visible animation without putting Runtime, renderer, or export logic in WPF.

## Deliverables

1. annotated v2 quick-view workflow
2. low-fidelity quick-browse wireframe
3. empty, loading, ready, warning, unsupported, failed, and renderer-unavailable states
4. command and shortcut map
5. collapsed behavior at minimum window size
6. WPF shell backed by fake presentation state
7. stable automation identifiers
8. screenshots and interaction-count review
9. decision record for auto-play and remembered per-asset state

## Allowed

- WPF Views
- presentation-only state/ViewModels
- fake Application services
- sample DTOs and diagnostics
- UI automation metadata

## Forbidden

- official Runtime changes
- concrete renderer
- real export pipeline
- duplicate version detection
- GPU/Runtime objects in ViewModels
- product logic in code-behind

## Acceptance Criteria

- viewport is the primary surface
- animation selection is immediately accessible
- valid asset path needs no advanced panel
- primary commands are keyboard accessible
- all required states render from fake data
- open-to-preview interaction count is recorded
- no View/ViewModel references official Runtime types

## Completion Evidence

- Review, wireframe, state table, command map, interaction count, and screenshots: [`../evidence/task-002/review.md`](../evidence/task-002/review.md)
- Shell: `src/SpineViewerWPF.Wpf`
- Validation: `scripts/test-ui-shell.ps1`
- Measured compact window: 756 by 519 pixels with a 54-pixel collapsed browse rail
- Exercised keyboard path: `Ctrl+O` from Empty to visible, fitted, auto-playing Ready state in one interaction

## Validation

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
```

Expected result: build succeeds with zero warnings/errors, 14 stable automation IDs are present, and the compact warning state exposes its main window.

## Known Gaps

- All asset, animation, skin, playback, diagnostics, and export data is intentionally fake.
- Drag/drop, file dialogs, persistence, real playback, and live renderer integration remain outside this task.
- Final production renderer and visual toolkit decisions remain open.
