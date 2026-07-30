# TASK-030: Workspace Panel Correctness

## Status

Completed on 2026-07-30.

## Objective

Keep Browse and Inspector bindings intact while floated, make every Inspector toggle use the same dock-aware path, and remove prototype-only controls from the normal shell.

## Context

- `docs/ai/decisions/ADR-006-dockable-workspace-and-localization-boundary.md`
- `docs/ai/tasks/TASK-029-dockable-workspace-prototype.md`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml.cs`
- `scripts/test-ui-shell.ps1`

## Allowed Paths

- `src/SpineViewerWPF.Wpf/**`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- Core and Application contracts
- renderer and export behavior
- unrelated adapters

## Required Behavior

- Floated Browse and Inspector panels retain the main window view model and current values.
- Header and Window-menu Inspector toggles share the dock-aware behavior.
- The normal shell does not expose the fake-data watermark, prototype state picker, or its keyboard shortcut.
- Startup `--state=...` test scenarios remain available.
- Existing Window-menu automation identities remain stable.
- No docking dependency is added.

## Acceptance Criteria

- Browse and Inspector can float, retain bound content, and redock.
- Toggling Inspector from either entry point cannot leave an empty floating window.
- Prototype-only chrome is absent from the normal UI.
- A runnable UI-shell check fails if floating panels stop inheriting the shell view model.
- Documentation records the corrected prototype boundary.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore
git diff --check
```

Desktop interaction check: `test-ui-shell.ps1` opens the Window menu, floats Browse, verifies bound values, and closes the shell; source assertions cover the shared Inspector route and prototype-only chrome.

## Completion Report

### Changed

- Owned floating panel windows inherit the main window view model.
- The header Inspector button uses the same dock-aware handler as the Window menu.
- Prototype watermark, state picker, shortcut, and unused command were removed from the normal shell.
- UI smoke now floats Browse and verifies its bound `idle` item through native UI Automation.

### Preserved

- Startup `--state=...` scenarios remain available for deterministic shell checks.
- Browse/Inspector hide, float, redock, and reset-layout commands keep their existing automation identities.
- Application, Core, Runtime, renderer, export, sidecar, and source-isolation behavior are unchanged.

### Validation

- WPF Release build passed with 0 warnings and 0 errors.
- UI shell smoke passed with 46 automation IDs, 4 editor shortcuts, compact warning-state launch, and floated Browse bindings.
- Application smoke passed.
- `git diff --check` passed.

### Gaps and Risks

- Only Browse and Inspector are floatable; drag docking, tab groups, and layout persistence remain deferred.
- The smoke covers the shared floating-window binding root through Browse; Inspector uses the same constructor and has a static dock-aware route assertion.

### Documentation

- Updated the context index, migration plan, ADR-006, and UI direction/information architecture.

### Recommended Next Task

Make screenshot and export output represent all visible scene layers and their transforms instead of only the primary asset.
