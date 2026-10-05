# TASK-064: Slot row width and post-export command state

Status: Completed on 2026-10-02.

## Objective

Fix two defects found by the 2026-10-02 manual walkthrough (recorded in
TASK-062 and TASK-063): Slots rows clip their opacity column, and Screenshot /
Export stay disabled after an export completes.

## Context

- `TASK-062-properties-consolidation-and-layers-footer.md` (Manual Walkthrough)
- `TASK-063-export-auto-fit-framing.md` (Validation Results)
- `16-ui-state-and-interaction-contracts.md`

## Scope

Continues on `codex/ui-workspace-refinement`. No push or branch switch.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/MainWindow.xaml` (Slots `ListBox` only)
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs` (`ExportSequenceAsync` completion only)
- `tests/SpineViewerWPF.Application.Smoke/Program.cs` (export command assertion)
- `scripts/test-ui-shell.ps1` (static token check)
- this task, `TASK-062`, `TASK-063`, `docs/ai/00-context-index.md`

## Forbidden Paths

- Core, Application, CLI, Runtime, renderer, sidecar schema
- Space/F shortcut handling (separate decision pending)
- other layout, styling, or AutomationId changes

## Required Behavior

- The Slots list never scrolls horizontally. Each row fits the panel width:
  the visibility switch and name take the remaining width with the name
  trimmed, followed by the attachment combo box and the opacity field.
- When an export finishes, is canceled, or fails, the in-progress flag is
  cleared before commands and properties are re-announced, so Screenshot and
  Export are enabled again when their other conditions hold.

## Validation

```powershell
dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

Smoke: the last `CanExecuteChanged` notification of Export and Screenshot after
an export completes must report them enabled (fails before the fix).

Manual: open spineboy, Slots tab: every row shows switch, name, attachment, and
opacity without horizontal scrolling; opening an attachment drop-down does not
shift the list. Export a sequence: Screenshot and Export are enabled afterwards.

## Completion Report

### Changed

- `MainWindow.xaml`: the Slots `ListBox` disables horizontal scrolling, so the
  existing star name column (already trimmed by `SlotVisibilityToggleStyle`)
  shrinks to the panel and the attachment and opacity columns stay visible.
- `ShellViewModel.ExportSequenceAsync`: clears `exportInProgress` before
  restoring state and refreshing commands (pre-existing since `f05f3b9`).
- Smoke: records `CanExecute` at each Export/Screenshot `CanExecuteChanged`
  during a WPF export and requires the last notification to report enabled.
- `test-ui-shell.ps1`: static token for the Slots horizontal-scroll setting.

### Preserved

- AutomationIds (85), slot row controls and bindings, export request, output,
  progress, cancellation, and the export-in-progress guard against re-entry.

### Validation (2026-10-02, Windows, local)

- The new smoke assertion failed before the fix ("Export and Screenshot were
  not re-enabled after export completed") and passes after it.
- `dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false`: 0 warnings, 0 errors.
- Application.Smoke: passed.
- `scripts/test-ui-shell.ps1`: passed (85 AutomationIds, 26 compact workspace tokens).
- `git diff --check`: passed.
- Manual (spineboy 4.1, Dark): every Slots row shows switch, trimmed name,
  attachment, and opacity with no horizontal scrolling; opening an attachment
  drop-down does not shift the list. After an export, UI Automation reports
  `Main.Command.Screenshot` and `Main.Command.Export` enabled.
