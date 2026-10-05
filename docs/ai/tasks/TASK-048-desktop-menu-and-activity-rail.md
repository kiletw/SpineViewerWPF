# TASK-048: Add a conventional desktop menu and activity rail

Status: Completed

## Objective

Replace the crowded top command strip with a conventional desktop menu plus a
small frequent-action toolbar, while using the left activity rail only for
workspace/panel navigation.

## Context

- `docs/ai/14-ui-product-design.md`
- `docs/ai/15-ui-information-architecture.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- Product-owner direction: Photoshop-like discoverability with dropdown menus
  for commands and side icons for panels/modes.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/App.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml.cs` only if an existing command
  cannot be wired directly from XAML
- `scripts/test-ui-shell.ps1`
- `docs/ai/00-context-index.md`
- `docs/ai/14-ui-product-design.md`
- `docs/ai/15-ui-information-architecture.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- this task document

## Forbidden Paths

- `src/SpineViewerWPF.Core/**`
- `src/SpineViewerWPF.Application/**`
- `runtimes/**`
- renderer, export, project, and Runtime behavior changes
- new UI frameworks or icon packages

## Required Behavior

- A normal top menu exposes File, Edit, View, Playback, Layer, Export, Window,
  and Help command groups.
- Existing commands, enablement, shortcuts, and Window docking handlers are
  reused; menu items do not duplicate use-case logic.
- The compact toolbar keeps only frequent Open, Save, Undo, Redo, Screenshot,
  and Export actions.
- The collapsed/expanded left activity rail provides clear Layers,
  Properties, and Diagnostics icon targets with tooltips and selected-state
  contrast.
- Existing viewport, panels, playback bar, status bar, themes, floating-panel
  behavior, and automation identities remain functional.
- All new user-facing labels use resource keys. Runtime language switching is
  still deferred.

## Acceptance Criteria

- Menu categories and activity targets have stable AutomationIds.
- The Window menu can still show/hide, float, redock, and reset panels.
- Window commands retain stable automation identities; the Float Inspector
  command is also reachable through `Ctrl+Shift+I`.
- The shell remains usable at the existing minimum size.
- WPF Release build, Application smoke, and UI shell validation pass.
- UI product/IA/interaction documentation is updated.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

Changed:

- Added File, Edit, View, Playback, Layer, Export, Window, and Help menus with
  desktop access keys and existing command shortcuts.
- Reduced the toolbar to Open, Save, Undo, Redo, Screenshot, and Export.
- Added always-visible Layers, Properties, and Diagnostics activity targets.
- Added a Dark/Light resource-driven menu template and `Ctrl+Shift+I` for the
  existing Float Inspector operation.

Preserved:

- Existing ViewModel commands, enablement, panel handlers, automation IDs,
  viewport, playback/status regions, Dark/Light themes, and Runtime behavior.
- Slot presentation edits remain covered by Application smoke; the shell test
  no longer relies on a DPI-sensitive physical click into a virtualized row.

Validation:

- WPF Release build: passed, 0 warnings / 0 errors.
- Application smoke: passed.
- UI shell: passed with 85 AutomationIds, 8 editor shortcuts, and 17 workspace
  structure tokens.
- Dark menu popup was visually inspected with a real WPF window.
- `git diff --check`: passed.

Gaps:

- Runtime language switching and translated resource sets remain deferred.
- WPF Popup submenu children are not consistently discoverable through the
  external UI Automation provider; stable source identities, menu headers,
  command bindings, and the Float Inspector shortcut are validated instead.

Risks:

- Activity icons are built-in text glyphs to avoid a new icon dependency; they
  should be rechecked if the application font changes.

Documentation:

- Updated context index, UI product direction, information architecture, and
  interaction contracts.

Next task:

- Add multi-layer screenshot and PNG-sequence composition parity with the
  viewport before expanding additional workspace panels.
