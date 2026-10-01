# TASK-062: Properties consolidation and Layers footer

Status: Implemented; automated validation passed on 2026-10-02; manual walkthrough found a Slots row layout defect, fixed by TASK-064.

## Objective

Second phase of the 2026-09-27 UI review: remove the two-row Properties tab
grid, stop presenting workspace-wide view settings as layer properties, make
Slots scannable, and let the Layers list use the panel height.

## Scope

Continues on `codex/ui-workspace-refinement`; preserve TASK-058 through TASK-061
working-tree changes. No commit, push, or branch switch.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml.cs` (converter, View-menu handler, viewport hit test)
- `src/SpineViewerWPF.Wpf/App.xaml` (text resources)
- `scripts/test-ui-shell.ps1` (AutomationId list and tab-host token)
- `docs/ai/00-context-index.md`, `docs/ai/14-ui-product-design.md`,
  `docs/ai/15-ui-information-architecture.md`, this task

## Forbidden Paths

- ViewModels' behavior, Core, Application, Runtime, renderer, sidecar schema
- new packages, icon fonts, docking or UI frameworks

## Changed Behavior

- Properties tabs: `Animation`, `Layer`, `Slots` in one row with underline
  selection (was six tabs in a 3x2 grid).
  - Animation gains the Skin combo box (was the Appearance tab).
  - Layer combines Transform (Position X/Y on one row, Scale, Rotation, flips)
    and Render (Visible, Layer opacity, Track 0 Alpha, PMA) sections.
  - Slots rows are one line: visibility switch + name (full-row target kept from
    TASK-045), attachment combo box (`Auto` = animation/setup), opacity.
- Viewport tab removed. Display channel, background, backend, and render size
  appear in a compact overlay at the viewport's top-left while an asset is open;
  View menu gains Display channel, Background, and Theme submenus.
  Project name/path move into Layers > Asset information.
- Layers list fills the panel; Add, Duplicate, Reload, Move up/down,
  Auto layout, and Remove move to a pinned icon footer with tooltips and
  accessible names.
- Viewport gestures ignore presses and wheel input that start on combo boxes or
  inside their drop-downs.

## AutomationId Changes

- Removed: `Main.Properties.TransformTab`, `Main.Properties.RenderTab`,
  `Main.Properties.AppearanceTab`, `Main.Properties.ViewportTab`,
  `Main.Inspector.ModelY` (row wrapper only).
- Added: `Main.Properties.LayerTab`, `Main.Viewport.Options`,
  `Main.Scene.ReloadLayer`, `Main.Scene.SlotItem`, `Main.Menu.View.Channel`,
  `Main.Menu.View.Background`.
- Kept on moved controls (identity, not location): `Main.Viewport.Channel`,
  `Main.Inspector.Background`, `Main.Inspector.Theme` (now the View > Theme
  submenu), `Main.Viewport.Backend`, `Main.Inspector.ProjectPath`,
  `Main.Asset.SkinList` (now a combo box), `Main.Scene.LayerSkin`.

## Validation

```powershell
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

Manual (Dark and Light):

1. Properties shows three tabs in one row; the selected tab has an accent underline.
2. Skin combo box switches skins; animation search/list and timing card still work.
3. Layer tab edits X/Y/Scale/Rotation/flips/opacity/Track alpha/PMA; Undo/Redo works.
4. Slots rows are one line; switch, attachment, and opacity edits apply while playing.
5. Viewport overlay changes channel and background; drop-downs open and scroll
   without zooming or panning the viewport. View menu shows the same choices checked.
6. View > Theme switches Dark/Light.
7. Layers list fills the panel; footer icons have tooltips and enable/disable
   with their commands; floating Layers keeps the footer.

## Completion Report

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

- Passed: three single-row tabs with underline selection; Layer tab
  sections; viewport overlay Background drop-down applies and View >
  Background shows the same check; wheel over the overlay and its drop-down
  does not zoom; View > Theme switches Dark/Light; footer icons show tooltips
  and Move/Remove disable with one layer; floating Browse panel keeps the
  footer.
- Failed: Slots rows clip the opacity column. The Slots `ListBox` keeps
  horizontal scrolling enabled, so the `*` name column sizes to the longest
  name and the 46 px opacity column falls outside the panel with no visible
  scroll bar; focusing an attachment combo box scrolls the list sideways and
  clips the switches. Fixed by TASK-064.
- Observed: in Light theme, menu drop-downs show a dark strip along the right
  edge.
- Not run: skin switching (spineboy has only `default`), Dark/Light comparison
  of every tab.

### Follow-up Fix (2026-09-28)

- `test-ui-shell.ps1` setup-pose check still failed with `propertiesTabs=True,
  animationTabSelected=True, animationListPresent=False` and no
  `Main.Asset.NoAnimations`. Cause: the custom `TabControl` template's content
  presenter lacked the required `PART_SelectedContentHost` name, so WPF's
  `TabItemAutomationPeer` exposed no selected-tab content to UI Automation or
  screen readers (pre-existing since the custom template). Named it and added a
  static token check.

### Known Gaps

- Panel widths are still fixed (no splitters).
- Theme brushes still live in code-behind `ApplyTheme`; moving them into
  Dark/Light resource dictionaries remains a follow-up.
