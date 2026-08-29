# TASK-052: Refine the viewport-first desktop workspace

Status: Completed on 2026-08-30.

## Objective

Improve the existing desktop workspace hierarchy without changing Viewer
behavior: keep the activity rail permanently reachable, compact the command
chrome, and make supporting panels read as tools around the viewport.

## Context

- `docs/ai/14-ui-product-design.md`
- `docs/ai/15-ui-information-architecture.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/decisions/ADR-004-ui-architecture-and-shell.md`
- `docs/ai/decisions/ADR-006-dockable-workspace-and-localization-boundary.md`
- Adobe Photoshop workspace and panel guidance:
  `https://helpx.adobe.com/photoshop/desktop/get-started/learn-the-basics/workspace-overview.html`
- Visual Studio tool/document window layout guidance:
  `https://learn.microsoft.com/visualstudio/ide/customizing-window-layouts-in-visual-studio`
- Windows command bar guidance:
  `https://learn.microsoft.com/windows/apps/design/controls/command-bar`
- `ww-rm/SpineViewer` remains a feature-density reference only.

## Design Read

- Redesign mode: preserve and refine.
- Audience: Spine asset viewers and editors using a dense desktop tool.
- Visual language: Photoshop / Visual Studio workspace with restrained Fluent
  hierarchy, not a web dashboard.
- Dials: variance 3, motion 2, density 8.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/App.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/00-context-index.md`
- `docs/ai/14-ui-product-design.md`
- `docs/ai/15-ui-information-architecture.md`
- this task document

## Forbidden Paths

- Core, Application, Runtime, renderer, export, and project behavior
- new packages, icon fonts, UI frameworks, or docking frameworks
- new panels, source-writing behavior, or layout persistence
- changes to established commands, shortcuts, and AutomationIds

## Required Behavior

- The activity rail is structurally independent from the Layers panel and
  remains visible when Layers is hidden or floated.
- Selecting Layers from the activity rail restores or expands the Layers panel.
- The toolbar contains only frequent commands, loses redundant branding, and
  uses compact low-noise chrome.
- The viewport gains vertical space while playback and status information stay
  visible.
- Layers and Properties keep their existing data, commands, tabs, docking,
  floating, theme, and localization boundaries.
- Dark and Light themes preserve readable hover, selected, disabled, and focus
  states.

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

- Reduced toolbar, playback, and status chrome from 46/52/26 to 38/44/24
  device-independent pixels so the viewport receives more vertical space.
- Removed redundant product branding from the toolbar and split frequent
  commands into edit and output groups with quieter ordinary button chrome.
- Kept the activity rail in the main window when Layers content is hidden or
  floated. The Layers activity target now restores, collapses, or activates
  the floating panel through the existing dock-aware code-behind path.
- Removed the repeated `Layers` / `SCENE LAYERS` heading and changed compact
  layer actions to wrapping rows.
- Recalibrated Dark and Light semantic brushes for quieter borders, clearer
  hover/pressed states, and one consistent mint accent.

### Preserved

- Existing commands, shortcuts, AutomationIds, ViewModel behavior, panel data,
  Properties tabs, viewport, playback, status, and native floating windows.
- Runtime, renderer, export, project, slot, theme-selection, and localization
  boundaries are unchanged.
- No package, icon font, UI framework, or docking framework was added.

### Validation

- WPF Release build passed with 0 warnings and 0 errors.
- Application smoke passed without opening its optional manual window.
- `scripts/test-ui-shell.ps1` passed its isolated build and live UI Automation
  walkthrough with 85 AutomationIds, 8 shortcuts, and 24 compact-workspace
  structure tokens. Slots availability and Duplicate layer interaction passed.
- The shell test now rejects floating Layers implementations that remove the
  activity rail from the main workspace.
- `git diff --check` passed.

### Known Gaps

- Native WPF floating remains bounded to Layers and Properties; there are no
  Photoshop-style drag docking guides, tabbed panel groups, saved layouts, or
  resizable dock widths.
- Runtime language switching and additional theme families remain deferred.

### Risks

- Visual density still depends on Windows text scaling and translated label
  lengths; the wrapping Layers actions avoid clipping but broader localization
  screenshots remain future acceptance work.

### Documentation

- Updated the context index, UI product direction, information architecture,
  and this completion report.

### Recommended Next Task

- Validate the refined workspace with the user's real assets and preferred DPI,
  then decide whether dock-width resizing or saved workspace layouts has the
  higher workflow value.
