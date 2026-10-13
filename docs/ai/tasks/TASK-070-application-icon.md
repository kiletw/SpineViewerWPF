# TASK-070: Application icon

Status: Implemented on 2026-10-07.

## Objective

The WPF and CLI executables, the main window, and floating panel windows show a
project-owned application icon instead of the Windows default.

## Context

- Round 2 competitor review, item 1 (`17-external-feature-reference.md`).
- No `.ico`, `ApplicationIcon`, or `Window.Icon` existed.
- `14-ui-product-design.md` forbids icon *packages*; a single project-owned
  `.ico` adds no dependency.
- The mark uses the existing accent `#58C7AD` and a generic bone-chain motif; it
  must not imitate the Esoteric Spine logo.

## Allowed Paths

- `scripts/build-app-icon.ps1`
- `src/SpineViewerWPF.Wpf/Assets/AppIcon.ico`
- `src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj`, `src/SpineViewerWPF.Cli/SpineViewerWPF.Cli.csproj`
- `src/SpineViewerWPF.Wpf/MainWindow.xaml`, `src/SpineViewerWPF.Wpf/MainWindow.xaml.cs` (window icon only)
- `docs/ai/00-context-index.md`, `docs/ai/17-external-feature-reference.md`, this task

## Forbidden Paths

- `runtimes/**`, Core, Application
- new packages

## Required Behavior

- `scripts/build-app-icon.ps1` regenerates `AppIcon.ico` from vector geometry
  with 16, 20, 24, 32, 40, 48, 64, 128, and 256 px PNG entries; 24 px and below
  use a simplified single-bone mark.
- Both executables embed the icon (`ApplicationIcon`).
- The main window uses it through a WPF resource; floating panels inherit the
  owner's icon.

## Acceptance Criteria

- Release build succeeds; the icon extracted from both executables is not the
  Windows default.
- `scripts/test-ui-shell.ps1` still launches the application and passes.
- `git diff --check` passes.

## Validation

```text
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-app-icon.ps1
dotnet build SpineViewerWPF.sln -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

- Changed: icon generator script, generated `AppIcon.ico`, `ApplicationIcon`
  for WPF and CLI, main/floating window icon.
- Preserved: all window behavior, menus, and layouts.
- Validation (2026-10-07): Release build 0 warnings/0 errors; icons extracted
  from `SpineViewerWPF.exe` and `spineviewerwpf.exe` match the new artwork;
  `test-ui-shell.ps1` passed.
- Gaps: artwork is a first draft; replace by editing the script geometry and
  rerunning it.
