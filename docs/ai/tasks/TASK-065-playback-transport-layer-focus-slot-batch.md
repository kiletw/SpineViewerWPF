# TASK-065: Playback transport, layer focus, and slot batch actions

Status: Completed on 2026-10-02.

## Objective

Adopt roadmap items 2-4 from `17-external-feature-reference.md`:

1. Playback transport: frame step, fast step, restart, full-screen preview
   (reset view is the existing Fit).
2. Focus and center the selected layer from the Layers panel.
3. Slot batch actions: show all, hide all, clear attachment choices, and a
   slots-only parameter copy scope.

## Context

- `docs/ai/17-external-feature-reference.md` (Adoption Roadmap)
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `TASK-043` (layer list actions and parameter copy/paste),
  `TASK-044` (slot attachment selection), `TASK-060` (viewport navigation),
  `TASK-062` (Properties layout)

## Scope

New branch `codex/playback-transport-and-slot-actions`, stacked on
`codex/ui-workspace-refinement` (PR #54) because `v3` does not yet contain
that work. WPF only.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/**` (ShellViewModel, SceneLayerViewModel,
  SlotDisplayViewModel, GpuViewport, MainWindow, App.xaml text resources,
  new `ViewportMath.cs`)
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-ui-shell.ps1` (AutomationId list)
- `docs/ai/00-context-index.md`, `docs/ai/16-ui-state-and-interaction-contracts.md`,
  `docs/ai/17-external-feature-reference.md`, this task

## Forbidden Paths

- Core, Application, CLI, Runtime adapters, `runtimes/**`, sidecar schema
- new packages, docking or UI frameworks
- Space/F shortcut handling (separate pending decision)

## Required Behavior

### Playback transport

- Previous / next frame move `Position` by one preview frame
  (`1 / PreviewFramesPerSecond`), snapped to that frame grid; back / forward
  10 frames move by ten. All steps pause playback and clamp to
  `[0, Duration]` without wrapping. Shortcuts: Ctrl+Left / Ctrl+Right and
  Ctrl+Shift+Left / Ctrl+Shift+Right (text boxes keep their own handling).
- Restart sets `Position = 0` and starts playback.
- Step and restart commands are enabled only when `CanPlay`.
- Full-screen preview (F11, View menu, playback-bar button) maximizes the
  window without chrome and hides the menu, command bar, docked Browse and
  Inspector panels, and the status bar; the viewport and playback bar remain.
  F11, Esc, or the button restores the previous window state, style, and
  panels. Floating panels are not changed. Full screen is not saved and does
  not dirty the project.

### Layer focus

- Double-clicking a layer, or Layer > Focus layer / the layer context menu,
  pans the viewport so the layer's content center is at the viewport center.
  Zoom is unchanged. The content center follows the GPU preview's framing
  (bounds fit, scale, flips, rotation, translation). Focus does not dirty the
  project or create Undo history.
- Without a GPU preview scene the command reports that focus needs the GPU
  preview and leaves the viewport unchanged (CPU fallback does not apply
  viewport pan; pre-existing TASK-060 gap).

### Slot batch actions

- Show all, Hide all, and All Auto (clear attachment choices) act on the slots currently shown
  by the Slots filter of the selected layer. Each action is one Undo step,
  dirties the project only when something changed, and reports a no-op
  otherwise.
- Copy parameters gains a Slots scope; pasting it applies only slot
  visibility, opacity, and attachment choices.

## Validation

```powershell
dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

Smoke additions: frame step/fast step/clamp/pause, restart, focus math and
viewport centering without dirtying, slot show/hide/clear as single Undo steps
with filter scope, and Slots-scope copy/paste.

Manual: step through spineboy `run`, restart, enter/leave full screen with
F11, Esc, and the button; double-click a moved/rotated layer to center it;
filter slots, hide all, show all, clear attachments, undo each.

## Completion Report

### Changed

- `ShellViewModel`: Restart, Previous/Next frame, Back/Forward 10 frames
  commands; `FocusLayer(layer, locator)`; slot batch commands; Slots copy
  scope command.
- `SceneLayerViewModel`: `LayerParameterScope.Slots`; `ApplySlotBatch` applies
  one action to the filtered slots inside one `changing`/`changed` pair.
- `ViewportMath` (new): shared GPU fit and layer content-center math;
  `GpuViewport.DrawLayer` now uses the shared fit, and
  `GpuViewport.TryGetLayerContentCenter` measures a layer for focus.
- `MainWindow`: Playback menu items and shortcuts, playback-bar Restart /
  Previous / Next / Full screen buttons, View > Full screen (F11), Esc handling
  in `WindowKeyDown` (IME-aware), Layer and context-menu Focus layer, Layers
  double-click focus (ignoring the visibility checkbox), Slots batch button row,
  Slots copy scope in Edit and context menus.
- `App.xaml`: text resources. `test-ui-shell.ps1`: 11 AutomationIds and a
  double-click token. Smoke: transport, focus, and slot batch assertions.

### Preserved

- Existing AutomationIds, shortcuts, and Space/F handling; viewport zoom/pan
  behavior; GPU drawing output (same fit formula, now shared); sidecar schema;
  Undo/Redo semantics for single slot edits.

### Validation (2026-10-02, Windows, local)

- `dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false`: 0 warnings, 0 errors.
- Application.Smoke: passed.
- `scripts/test-ui-shell.ps1`: passed (96 AutomationIds, 27 compact workspace tokens).
- `git diff --check`: passed.
- Manual (spineboy 4.1 `run`, GPU, driven through UI Automation and posted
  input because another application overlapped the screen):
  next/previous frame moved 0 -> 2/30 -> 1/30 s; Ctrl+Shift+Left stepped back
  ten frames; Restart started playback from zero; the full-screen button and
  F11 entered and left full screen, hiding menu, command bar, panels, and status
  bar while keeping viewport and playback bar; Layer > Focus layer centered a
  layer at X -150 and again after rotation 90 and scale 0.5; Hide all hid 52
  slots, Undo restored them, and with the filter `gun` Hide all hid only the gun.

### Known Gaps

- Esc was verified only with a posted `WM_KEYDOWN`: Esc sent through the system
  input queue during the session was intercepted before reaching the app (the
  computer-use overlay reserves Esc), while F11 and Ctrl+arrow keys arrived.
  A trace showed the posted Esc reaching `WindowKeyDown` unhandled and leaving
  full screen. The user confirmed a physical Esc press on 2026-10-02.
- Layer focus is GPU-only; the CPU fallback ignores viewport pan (TASK-060 gap).
- Layers double-click focus was exercised through the same command path as the
  menu; a physical double-click was not performed.
