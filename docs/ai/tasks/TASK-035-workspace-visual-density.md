# TASK-035: Photoshop-Style Workspace Density Pass

## Status

Completed on 2026-07-31.

## Objective

Make the default WPF shell feel like a compact desktop authoring workspace by increasing viewport share and unifying control styling, without changing information architecture or behavior.

## Context

- `docs/ai/14-ui-product-design.md`
- `docs/ai/15-ui-information-architecture.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/decisions/ADR-004-ui-architecture-and-shell.md`
- `docs/ai/decisions/ADR-006-dockable-workspace-and-localization-boundary.md`
- User comparison screenshots show oversized command chrome, visually inconsistent native inputs, and less canvas emphasis than the Photoshop-style direction.

## Design Read

Desktop Spine authoring/viewer for asset users, using a Photoshop-like dark, compact, canvas-first language.

- `DESIGN_VARIANCE: 3`
- `MOTION_INTENSITY: 2`
- `VISUAL_DENSITY: 8`
- Implementation remains native WPF with the existing single mint accent; no new visual package.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/App.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- ViewModel, Application, Core, Runtime, renderer, CLI, or project schema behavior
- new packages
- automation ID changes
- panel docking behavior changes
- new commands or product features
- user asset files

## Required Behavior

- The viewport gains usable space through denser command, playback, status, and panel chrome.
- Buttons, text inputs, combo boxes, sliders, checks, list rows, and scrollbars share one dark control language.
- The top command bar retains all existing commands without wrapping at the supported default size.
- Browse and Inspector content remain scrollable at the minimum supported window size.
- Existing docking, shortcuts, automation IDs, loading/error states, and playback behavior remain unchanged.

## Acceptance Criteria

- Default screenshot has a visually dominant viewport and compact tool chrome.
- No default white input fields or OS-white scroll tracks remain in the main shell.
- One accent color and one compact corner-radius rule are used consistently.
- UI shell automation and Application smoke pass.
- WPF Release build has zero warnings and zero errors.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore -p:BaseOutputPath=artifacts/application-smoke/bin/
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

- Changed `MainWindow.xaml` to use a 48-pixel command bar, 52-pixel playback bar, 26-pixel status bar, 250/260-pixel supporting panels, and tighter viewport/control spacing.
- Added native WPF dark templates for combo boxes, combo items, sliders, check boxes, and vertical scrollbars; visual QA caught and removed the remaining OS-white combo-box selection chrome.
- Kept the existing command set, automation IDs, bindings, shortcuts, docking behavior, loading states, playback behavior, and localization resource boundary unchanged.
- Extended `scripts/test-ui-shell.ps1` with nine compact-workspace/style assertions while retaining the existing live shell and floating-panel checks.
- Validation passed:
  - WPF Release build: zero warnings, zero errors.
  - UI shell smoke: 49 automation IDs, four editor shortcuts, nine compact-workspace tokens, warning-state launch, and floated Browse binding.
  - Application smoke: viewer project, PNG render preview, edit history, validation, and source isolation.
  - `git diff --check`: passed; only existing line-ending conversion notices were reported.
- Visual QA was performed against the built Windows application at the default 1280 by 800 size. The viewport is dominant and the main-shell inputs and scroll tracks remain dark.
- Remaining assumption: the native Windows title bar follows the operating-system theme. Custom window chrome is intentionally outside this density-only task because it would also own window movement, resizing, and system-button behavior.
