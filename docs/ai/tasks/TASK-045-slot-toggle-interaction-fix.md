# TASK-045: Slot Toggle Interaction Fix

## Status

Completed.

## Objective

Reproduce and fix the WPF Slots visibility toggle that the user reports cannot be modified.

## Context

- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/tasks/TASK-044-slot-attachment-selection.md`
- User report on 2026-08-10: clicking a Slot switch does not modify it.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/**`
- `scripts/test-ui-shell.ps1`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- vendored Runtime source
- Core/Application contracts
- project schema changes
- new packages
- unrelated UI or feature work

## Required Behavior

- A real WPF Slot visibility switch accepts mouse input across the Slot name row.
- The ViewModel value remains changed after the preview rerenders.
- The edit updates dirty state, Undo restores it, and Redo reapplies it.
- GPU and CPU preview paths continue to consume the same Slot document.
- Existing Attachment selector, opacity, export, and sidecar behavior remain intact.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore -p:BaseOutputPath=artifacts/application-smoke/bin/
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

### Changed

- Replaced the 14-pixel Slot checkbox with a named, full-row toggle and a visible switch track/thumb.
- Added Application coverage for visibility mutation, GPU rerender persistence, Undo, and Redo.
- Added a live WPF physical-click check that verifies dirty state and Undo/Redo command transitions.
- Stabilized the shell smoke window lookup so an OpenGL helper window is not mistaken for the WPF main window.

### Root cause

The `IsVisible` setter and both 4.2 and 3.5.51 Runtime paths already accepted the edit. The interaction affordance was only a roughly 14-by-14 checkbox inside a dense list row, while the Slot name was not part of the hit target. This made normal clicking appear ineffective. The templated child is also not surfaced as a searchable UI Automation control, so automated attempts that targeted its ID produced false negatives.

### Preserved

- Slot opacity and attachment selection.
- Shared GPU/CPU Runtime pose settings and sidecar schema.
- Read-only source Spine assets.

### Validation

- WPF Release build: passed, 0 warnings / 0 errors.
- Application smoke: passed, including rerender persistence and Undo/Redo.
- WPF UI shell smoke: passed, including physical Slot-row click and Undo/Redo.
- Live 3.5.51 fixture: switch changed, project became dirty, and the rendered Slot disappeared.

### Risks

- The supplied large `116421.json` takes substantially longer than the fixture to load in the automated harness; the same 3.5.51 adapter and Slot contract are covered by the focused fixture, but a long-running visual acceptance pass on that exact asset remains useful.
