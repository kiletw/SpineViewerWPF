# TASK-053: Correct interactive-state contrast

Status: Completed on 2026-08-31.

## Objective

Make hover, pressed, selected, checked, and disabled controls readable in both
Dark and Light themes without changing commands or application behavior.

## Context

- `docs/ai/14-ui-product-design.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/decisions/ADR-004-ui-architecture-and-shell.md`
- `docs/ai/decisions/ADR-006-dockable-workspace-and-localization-boundary.md`
- User report: highlighted buttons can use foreground and background colors
  that are too similar to distinguish by eye.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/MainWindow.xaml`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml.cs`
- `scripts/test-ui-shell.ps1`
- `docs/ai/00-context-index.md`
- `docs/ai/14-ui-product-design.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- this task document

## Forbidden Paths

- Core, Application, Runtime, renderer, export, and project behavior
- `runtimes/**` and vendored upstream code
- new packages, themes, controls, or interaction modes
- changes to commands, shortcuts, bindings, or AutomationIds

## Required Behavior

- Accent buttons retain a readable label during hover and press.
- Check boxes and slot switches follow semantic Dark/Light theme brushes.
- Selected activity targets remain distinguishable when hovered.
- Disabled controls remain visibly disabled without making their labels
  illegible.
- Existing command behavior, theme selection, and slot interaction are
  preserved.

## Acceptance Criteria

- Interactive state foreground/background pairs are not hard-coded to only one
  theme where semantic theme brushes already exist.
- UI shell validation asserts the contrast-critical state resources.
- WPF Release build, Application smoke, UI shell validation, and diff check pass.
- Documentation records changed and preserved behavior.

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

- Accent buttons now use a theme-specific foreground and switch to the normal
  text brush when hover or pressed changes their surface.
- CheckBox and slot-switch states use semantic theme brushes; the unresolved
  slot thumb resource was corrected.
- Disabled control opacity was raised from 0.4/0.42 to 0.55.
- UI shell static checks reject the previous theme-specific foreground and
  missing slot brush.

### Preserved

- Commands, bindings, shortcuts, AutomationIds, slot behavior, themes, Runtime,
  renderer, export, and project behavior are unchanged.

### Validation

- Isolated WPF Release build: passed with 0 warnings and 0 errors.
- Application smoke: passed.
- UI shell validation passed with 85 AutomationIds, 8 editor shortcuts, 24
  compact-workspace tokens, live Slots availability, and Duplicate layer
  interaction.
- Static contrast regression checks and `git diff --check`: passed.
- Calculated text contrast ranges from 4.96:1 to 12.48:1 for the primary
  Dark/Light normal, hover, and pressed states.
