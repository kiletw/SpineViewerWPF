# TASK-029: Dockable Workspace Prototype

## Status

Completed on 2026-07-28.

## Objective

Validate the smallest Photoshop-style workspace interaction against the existing WPF shell without introducing a docking dependency or changing Application/Runtime contracts.

## Scope

- add a Window menu with stable automation IDs
- hide/show the Browse and Inspector panels
- float each panel in an owned WPF window
- close a floating panel to redock it
- redock both panels and reset the default layout
- keep existing quick-browse, playback, editing, save, and localization resource boundaries intact

## Preserved Behavior

- existing `Main.*` automation IDs remain stable
- the viewport, playback bar, diagnostics, and sidecar save flow are unchanged
- WPF-only window orchestration does not expose Runtime-specific types
- layout state is session-only; no project files are modified by workspace commands

## Validation

```powershell
dotnet build .\src\SpineViewerWPF.Wpf\SpineViewerWPF.Wpf.csproj -c Release --no-restore
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-official-v38.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-official-v41.ps1
```

Results: all passed; WPF shell build has 0 warnings and 0 errors, UI smoke reports 46 automation IDs, v3 reports 12 historical runtimes, and official v3.8/v4.1 inspect/render/PMA checks pass. Manual desktop smoke also verified Window menu, hide/show, float, and redock interactions.

## Known Gaps

- only Browse and Inspector are floatable in this slice
- drag-to-dock, tab groups, saved layout profiles, and toolkit selection remain deferred
- runtime language switching and translated resource sets remain deferred
