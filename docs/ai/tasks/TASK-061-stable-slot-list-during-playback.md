# TASK-061: Stable Slots list during playback

Status: Completed on 2026-10-02 (automated validation and manual playback check passed).

## Problem

User report (2026-09-27): while an animation plays, Slots rows flicker
continuously and their visibility switches cannot be toggled.

Cause: every GPU playback frame calls `SceneLayerViewModel.PublishScene`, which
raised `Slots` and `FilteredSlots` unconditionally. `FilteredSlots` returned a
new array on every read, so the Slots `ListBox` replaced its `ItemsSource` and
regenerated every row once per frame. Mouse down and mouse up landed on
different containers, so clicks were lost, and scroll position reset.
Pre-existing since the scene-driven slot discovery; unrelated to TASK-060.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/SceneLayerViewModel.cs`
- this task document

## Forbidden Paths

- Runtime, renderer, Application, Core, sidecar schema, XAML layout

## Required Behavior

- `FilteredSlots` is cached and returns the same instance until the filter
  changes or a slot is added.
- `PublishScene` always raises `PreviewScene`, but raises `Slots` and
  `FilteredSlots` only when the scene introduced new slot names.
- Slot visibility, opacity, attachment, Undo/Redo, filtering, and sidecar
  behavior are unchanged.

## Validation

```powershell
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

Manual: play an animated asset, open Slots, scroll the list, toggle several
switches and change an attachment while playing. Rows must not flicker, the
scroll position must hold, each toggle must apply on the first click and appear
in the viewport, and Undo/Redo must restore it. Typing in Filter slots still
filters immediately.

## Validation Results (2026-10-02, Windows, local)

Automated checks passed on the combined TASK-058 through TASK-063 working tree:

- `dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false`: 0 warnings, 0 errors.
- Application.Smoke (`SPINEVIEWER_SKIP_WINDOW_SMOKE=1`): passed.
- `scripts/test-ui-shell.ps1`: passed (85 AutomationIds, 8 editor shortcuts,
  25 compact workspace tokens, Slots, slot attachment binding, duplicate layer).
- `git diff --check`: passed.
- Manual check (2026-10-02, spineboy `run`, GPU): rows stayed stable while
  playing; `head` and `gun` switches applied on the first click and disappeared
  from the viewport; scroll position held; an attachment change applied while
  playing; Undo restored all three edits. Passed.

## Known Gaps

- No automated test exercises slot toggles during live playback; the manual
  check above is the only evidence for the flicker fix.
