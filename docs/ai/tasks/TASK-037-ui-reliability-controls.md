# TASK-037 UI reliability and editor controls

Status: Completed
Date: 2026-08-02

## Objective

Fix the GPU preview's first-frame and playback feedback issues, make asset
loading recoverable when automatic atlas discovery fails, add drag-and-drop,
and expose the requested interactive display/theme controls.

## Allowed paths

- `src/SpineViewerWPF.Wpf/**`
- `src/SpineViewerWPF.Core/Contracts.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`
- `src/SpineViewerWPF.Application/ViewerProjectStore.cs`
- project-owned preview bridge files under `runtimes/SpineRuntime.V41/` that
  are linked by the other runtime adapters
- project-owned adapter files under `runtimes/SpineRuntime.V40/` and
  `runtimes/SpineRuntime.Legacy/`
- `scripts/test-ui-shell.ps1`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `docs/ai/**`

Vendored official Runtime source remains read-only.

## Behaviour

- Missing or ambiguous atlas discovery offers an explicit atlas file picker.
- Files dropped on the window open as an asset (or are offered as layers by
  the existing multi-file picker).
- The initial GPU viewport invalidates after layout and after scene changes;
  the first visible frame does not require a resize.
- The status bar and inspector identify GPU versus CPU fallback.
- Slot visibility and opacity can be changed in the selected layer preview.
- The interface exposes Dark and Light themes with clear selected/hovered
  button contrast.

## Validation

- `dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore`
- `dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v38.ps1 -Offline`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-official-v41.ps1 -Offline`
- `git diff --check`

## Result

- Atlas discovery failures now offer a manual atlas picker, including a
  dropped atlas when one is present.
- Window and viewport drop handlers open `.json`/`.skel` assets.
- GPU scene rendering is queued after a successful asset/project load and the
  GL surface requests a first layout frame, so resizing is not required.
- Status and Inspector expose the active GPU/CPU fallback backend.
- Preview scene commands carry slot names; selected-layer slot visibility and
  opacity are editable and round-trip in the Viewer sidecar.
- Dark/Light theme resources are switchable at runtime and selected controls
  use higher-contrast brushes.

Validation completed on 2026-08-02: WPF Release build, Application smoke,
UI shell automation, v3 compatibility, official 3.8 offline, official 4.1
offline, and `git diff --check` all passed.
