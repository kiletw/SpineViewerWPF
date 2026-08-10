# TASK-038: Carry slot display controls into deterministic export

## Status

Completed on 2026-08-08.

## Objective

Ensure slot visibility and opacity edits affect PNG screenshots and animation
sequence export, not only the interactive viewport.

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`
- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- project-owned adapter files under `runtimes/SpineRuntime.V41/`
- project-owned adapter files under `runtimes/SpineRuntime.V40/`
- project-owned adapter files under `runtimes/SpineRuntime.Legacy/`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `docs/ai/**`

## Required Behavior

- `AnimationExportRequest` carries optional slot display settings.
- All Runtime adapters apply those settings through the existing neutral frame
  request before deterministic PNG encoding.
- WPF animation export snapshots the selected layer's slot settings.
- Existing callers without slot settings retain byte-identical output.

## Validation

- WPF Release build
- Application smoke
- v3, official 3.8, and official 4.1 offline suites
- `git diff --check`

## Result

- Slot settings now flow through deterministic single-frame, scene-layer, and
  animation-sequence rendering.
- WPF export snapshots the selected layer settings; callers that omit them keep
  the prior output path and bytes.
- Release build, smoke, UI shell, v3, official 3.8/4.1 offline suites, and
  `git diff --check` passed on 2026-08-08.
