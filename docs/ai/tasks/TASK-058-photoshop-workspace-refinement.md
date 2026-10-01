# TASK-058: Refine workspace layout and visual hierarchy with Photoshop references

Status: Completed on 2026-09-05 (historical report; scope wording corrected by TASK-059).

## Objective

Apply a visual refinement pass to the SpineViewerWPF shell based on proven professional desktop tools (Adobe Photoshop, After Effects, Esoteric Spine Editor, and Unity Inspector):
1. Clean up the top command bar with structured button chrome and logical dividers.
2. Replace the scattered 4-row Layers button layout with an aligned Photoshop-style action footer.
3. Eliminate TabItem visual overlap in the 3x2 Properties grid with crisp segmented button styling.
4. Add clear search placeholders to animation and slot filter boxes.
5. Remove redundant Loop checkbox in the inspector while preserving timing controls.
6. Align status and playback bar elements with clear separators.

## Context

- `docs/ai/14-ui-product-design.md`
- `docs/ai/15-ui-information-architecture.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/decisions/ADR-004-ui-architecture-and-shell.md`
- `docs/ai/decisions/ADR-006-dockable-workspace-and-localization-boundary.md`
- `docs/ai/tasks/TASK-052-workspace-visual-refinement.md`
- `docs/ai/tasks/TASK-053-interactive-state-contrast.md`

## Allowed Paths

- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `docs/ai/**`

## Forbidden Paths

- `src/SpineViewerWPF.Core/**`
- `src/SpineViewerWPF.Application/**`
- `runtimes/**`

## Required Behavior

- All 85 AutomationIds must be preserved.
- All shortcuts, bindings, and dock/float behaviors must remain functional.
- All 24 compact workspace tokens checked by `scripts/test-ui-shell.ps1` must continue to match.
- Shell build and tests must pass.

## Validation

```powershell
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

### Changed

- `docs/ai/14-ui-product-design.md`: No new design-reference section is present in the reviewed working-tree change; its difference was only a UTF-8 BOM. The references below describe design intent, not a delivered section in that file.
- `docs/ai/00-context-index.md`: Added TASK-058 entry under Current Status.
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`:
  - **Top Toolbar**: Updated `ToolbarButton` with subtle 1px border and background chrome; added functional separator bars between Open, Project/History commands, and Export commands.
  - **Layers Action Footer**: Converted the loose 4-row wrap buttons into a unified 2-row Photoshop-style action group following the layer list inside the scrollable Layers content with balanced column widths.
  - **TabControl / TabItems**: Styled the 3x2 grid of tabs as discrete, rounded segmented card buttons with borders and active selection contrast, eliminating visual collision between rows.
  - **Search Inputs**: Added `🔍` search icon and dynamic italic placeholders (`Filter animations`, `Filter slots`) for Animation and Slot search textboxes.
  - **Animation Timing Controls**: Grouped `Speed`, `Preview FPS`, and `Export FPS` into a clean `Timing & Playback` container card; removed the duplicate `Loop` checkbox in the inspector.
  - **Status Bar**: Added discrete vertical separators between Runtime label, Diagnostics button, GPU badge, and performance indicators.

### Preserved

- All 85 AutomationIds, 8 keyboard shortcuts, and 24 compact workspace tokens are preserved.
- Binding contracts, layer duplication/removal, timeline scrubbing, playback, GPU/CPU rendering, export, and project serialization remain identical.

### Historical validation report

The results below were recorded by the original task; they were not independently
reproduced at the time of this wording correction. See TASK-059 for current checks.

- `dotnet build`: Release build succeeded with 0 warnings and 0 errors.
- `SpineViewerWPF.Application.Smoke`: passed.
- `scripts/test-ui-shell.ps1`: passed with 85 AutomationIds, 8 editor shortcuts, 24 compact workspace tokens, live slot attachment binding, and duplicate layer interaction.
- `git diff --check`: passed.
- Visual inspection via `PrintWindow` was reported to show the revised hierarchy. Fixed-bottom docking is not implemented; a non-scrolling footer remains follow-up work, outside TASK-059.

