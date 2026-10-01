# TASK-060: Viewport interaction and chrome quick wins

Status: Implemented; automated validation passed on 2026-10-02; manual walkthrough found Space/F shortcut gaps (see Manual Walkthrough).

## Objective

First phase of the 2026-09-27 UI review: make viewport navigation match common
desktop viewers, stop unmodified shortcuts from firing while typing, reduce
accent noise, and theme the native title bar, without changing the workspace
information architecture.

## Context

- `docs/ai/14-ui-product-design.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `TASK-014-viewport-navigation.md` (superseded gesture modifiers)
- `TASK-052-workspace-visual-refinement.md`, `TASK-058-photoshop-workspace-refinement.md`
- Design proposal canvas (Before / After / Inspector / Empty state), 2026-09-27

## Scope

Continues on `codex/ui-workspace-refinement`. Preserve all existing TASK-058 and
TASK-059 work in the working tree. No commit, push, or branch switch.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml.cs`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs` (viewport navigation only)
- `src/SpineViewerWPF.Wpf/App.xaml` (new text resources only)
- `tests/SpineViewerWPF.Application.Smoke/Program.cs` (viewport assertions)
- `docs/ai/00-context-index.md`, `docs/ai/14-ui-product-design.md`,
  `docs/ai/16-ui-state-and-interaction-contracts.md`, this task

## Forbidden Paths

- Core, Application, CLI, Runtime adapters, `runtimes/**`, GPU renderer
- new packages, icon fonts, UI or docking frameworks, custom window chrome
- changes to AutomationIds, modified shortcuts, commands, tabs, or panel layout

## Required Behavior

- Mouse wheel zooms without a modifier, anchored at the cursor position.
  Zoom is bounded to 10%-800%; `ZoomViewport(factor)` keeps center zoom for
  existing callers.
- Left-drag (after a 3 DIP threshold) and middle-drag pan without a modifier.
  Presses that start on buttons, text boxes, thumbs, or scroll bars inside the
  viewport keep their normal behavior.
- Left double-click on the viewport runs Fit.
- The status bar shows the current zoom (`Main.Status.Zoom`).
- Space (play/pause) and F (fit) run only when focus is not in a text box,
  password box, or editable combo box and no modifier is held.
- Delete removes the selected layer only while focus is inside the Layers list;
  the Layer menu and context menu keep their Remove commands.
- Viewport zoom/pan raise only viewport-derived property notifications, and the
  window re-applies theme brushes only when the theme mode actually changes.
- The native title bar of the main window and floating panels uses DWM immersive
  dark mode plus caption/text colors that follow Dark/Light. Unsupported Windows
  builds keep the default caption.
- Floating panels share the owner's live `WindowBrush`.
- The status-bar Runtime label, GPU badge, Inspector runtime chip, and Viewport
  backend label use neutral foregrounds; the mint accent stays on primary
  actions, selection, switches, and progress.

## Validation

```powershell
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

Manual walkthrough (Dark and Light, 100% and 150% DPI):

1. Wheel over a point on the model: that point stays under the cursor.
2. Drag with left and middle buttons pans; a plain click does not move the view.
3. Double-click the viewport fits; status zoom returns to 100%.
4. Type `f` and a space into Filter animations / Filter slots: text is entered,
   Fit and play/pause do not fire.
5. Focus a numeric field or slot attachment combo box and press Delete: no layer
   is removed. Focus the Layers list and press Delete: the layer is removed.
6. Title bar is dark in Dark theme, light in Light theme (Windows 11 shows the
   caption color; Windows 10 20H1+ shows dark mode only).
7. Float Layers and Properties: their title bars and backgrounds follow theme.
8. Empty-state Open asset, diagnostics, and export-cancel buttons still click.

## Completion Report

### Changed

- Unmodified wheel zoom anchored at the cursor, 10%-800% bounds, left/middle
  drag pan with a click threshold, double-click Fit, and a status-bar zoom label.
- Space/F moved from window KeyBindings into a focus-aware `KeyDown` handler;
  Delete scoped to the Layers list.
- Targeted viewport notifications and theme re-application only on mode change.
- DWM-themed native title bars for the main and floating windows; floating
  panels share the live window brush.
- Neutral status and runtime labels to reduce accent noise.
- Smoke assertions for anchored zoom, its inverse, the upper bound, and Fit.

### Preserved

- All 85 AutomationIds, the 8 checked editor shortcuts, compact-workspace and
  contrast tokens in `scripts/test-ui-shell.ps1` (verified statically).
- Menu gesture text, commands, tabs, docking/floating, Runtime, renderer,
  capture/export, project, and localization boundaries.

### Validation (2026-10-02, Windows, local)

Automated checks passed on the combined TASK-058 through TASK-063 working tree:

- `dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false`: 0 warnings, 0 errors.
- Application.Smoke (`SPINEVIEWER_SKIP_WINDOW_SMOKE=1`): passed.
- `scripts/test-ui-shell.ps1`: passed (85 AutomationIds, 8 editor shortcuts,
  25 compact workspace tokens, Slots, slot attachment binding, duplicate layer).
- `git diff --check`: passed.
- Manual walkthrough (2026-10-02) recorded below.

### Manual Walkthrough (2026-10-02)

Environment: Windows 11, single monitor at the system DPI in use (DPI variants and Windows 10 not tested), official spineboy 4.1 `spineboy-pro.json`, GPU backend.

- Passed: wheel zoom keeps the point under the cursor (status 133%); a plain
  click does not move the view; left-drag and middle-drag pan; double-click
  fits (status 100%); typing in Filter animations does not fit or toggle
  playback; Delete in the Speed field keeps the layer; Delete in the Layers
  list removes the selected layer; Dark and Light native title bars and the
  floating Browse panel follow the theme.
- Failed: Space/F are not reliable outside text entry.
  - With the Microsoft Bopomofo IME in Chinese mode, letters and Space reach
    `WindowKeyDown` as `Key.ImeProcessed`, so F and Space never fire.
  - With focus on a Layers list item, the ListBox consumes Space; F works only
    with the IME in English mode.
  - After clicking a toolbar button, Space activates that focused button.
  - Clicking the Layer tab moves keyboard focus into Position X, so the next
    keys are typed into that field.
- Not run: 150% DPI, Windows 10, empty-state Open asset, diagnostics button,
  export cancel.

### Known Gaps

- CPU fallback preview does not apply viewport zoom/pan (pre-existing; the CPU
  `Image` layers use only model transforms). GPU preview is unaffected.
- Model translation is added after viewport zoom in the GPU shader, so anchored
  zoom is exact for untranslated layers and approximate for translated ones.
- Windows 10 does not support DWM caption colors; only immersive dark mode applies.

### Recommended Next Task

- Phase 2 from the UI review: move theme brushes into Dark/Light resource
  dictionaries, consolidate Properties into Animation / Layer / Slots, move
  global view settings out of layer properties, compact Slots rows, and pin the
  Layers action footer.
