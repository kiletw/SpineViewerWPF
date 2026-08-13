# TASK-049: Polish activity rail clarity and scaling

Status: Completed

## Objective

Replace font-dependent activity glyphs with native WPF vector icons and make
the selected panel state clearer without changing workspace behavior.

## Context

- `docs/ai/tasks/TASK-048-desktop-menu-and-activity-rail.md`
- `docs/ai/14-ui-product-design.md`
- `docs/ai/15-ui-information-architecture.md`
- `docs/ai/decisions/ADR-006-dockable-workspace-and-localization-boundary.md`

## Allowed Paths

- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `scripts/test-ui-shell.ps1`
- `docs/ai/00-context-index.md`
- this task document

## Forbidden Paths

- Core, Application, Runtime, renderer, export, and project behavior
- new packages, icon fonts, or docking frameworks
- new panels or layout persistence

## Required Behavior

- Layers, Properties, and Diagnostics use vector icons that scale independently
  of the application font.
- Selected panel targets retain contrast and add a clear accent indicator.
- Tooltips, AutomationIds, commands, and panel state remain unchanged.
- Dark and Light resources remain dynamic.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore -p:BaseOutputPath=artifacts/task-049/bin/
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

Changed:

- Replaced the three font glyphs with native WPF vector paths.
- Added a three-pixel accent edge to the selected activity target.
- Added localized automation help text to each activity target.

Preserved:

- Existing commands, click handlers, AutomationIds, tooltips, panel state, and
  layout behavior.
- No package, font, ViewModel, Runtime, or renderer changes.

Validation:

- Isolated WPF Release build: passed, 0 warnings / 0 errors.
- UI shell: passed with the vector and selected-indicator structure checks.
- Dark, Light, selected Properties, and 760x520 minimum-window layouts were
  visually inspected using the isolated build.
- `git diff --check`: passed.

Gaps:

- The rail remains icon-only; tooltips and automation names provide labels.

Risks:

- None beyond normal manual visual-regression coverage for WPF vector paths.

Documentation:

- Updated the AI context status and this task report.

Next task:

- Resume the multi-layer screenshot/export parity work identified by TASK-048.
