# TASK-075: Copy Screenshot and custom background color

Status: Implemented on 2026-10-07.

## Objective

A screenshot can go straight to the clipboard, and the viewport background can
be any color, so assets can be checked against a game's backdrop color.

## Context

- Round 2 competitor review, item 6 (`17-external-feature-reference.md`);
  game-asset viewers offer both.
- Screenshot already renders the visible scene with the inspection channel
  (TASK-050); only its destination changes.
- Background mode is project state (sidecar schema 1, Undo/Redo); Checkerboard,
  Dark, and Light were the only accepted values.

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs` (`ViewerProjectDocument.BackgroundColor`)
- `src/SpineViewerWPF.Application/ViewerProjectStore.cs` (validation)
- `src/SpineViewerWPF.Wpf/**`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `docs/ai/00-context-index.md`, `docs/ai/12-application-use-cases.md`,
  `docs/ai/16-ui-state-and-interaction-contracts.md`,
  `docs/ai/17-external-feature-reference.md`, this task

## Forbidden Paths

- `runtimes/**`, renderers, export output
- a sidecar schema version change
- new packages

## Required Behavior

- Export > Copy Screenshot renders exactly what Screenshot saves (viewport pixel
  size, visible layers, inspection channel) and sets a clipboard bitmap plus a
  `PNG` entry that keeps transparency. It shares Screenshot's enablement and
  busy state, does not dirty the project, and reports failures as
  `CAPTURE_FAILED`. Without a clipboard delegate the command is unavailable.
- Background adds `Custom`. `ViewerProjectDocument.BackgroundColor` (optional,
  `#RRGGBB`) is required when the mode is Custom and validated whenever present.
  The Shell normalizes typed colors (`12ab34` → `#12AB34`), ignores invalid
  entries, records color edits in Undo/Redo, saves the color only for Custom,
  and restores it when a project opens (default `#808080`).
- The viewport options show the color field only for Custom; View >
  Background lists Custom color. The background is never written to output.

## Acceptance Criteria

- Copy Screenshot delivers the viewport-sized capture with the chosen channel
  and re-enables Screenshot; the real clipboard receives bitmap and PNG data.
- Custom color normalizes, rejects invalid input, undoes, saves, and reopens;
  non-custom saves store no color; invalid sidecar colors fail validation.
- Existing projects without the field load unchanged; build, smoke,
  `test-v3.ps1`, and UI shell checks pass.

## Validation

```text
dotnet build SpineViewerWPF.sln -c Release
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

- Changed: Copy Screenshot command and clipboard helper, Custom background mode
  and color (Core field, store validation, Shell state/Undo/save/load, view),
  `ClipboardBackgroundSmoke`.
- Preserved: Screenshot file output; Checkerboard/Dark/Light rendering; sidecar
  schema version 1 and loading of existing projects.
- Validation (2026-10-07): Release build clean; Application.Smoke passed with the
  TASK-075 checks; `test-v3.ps1` and `test-ui-shell.ps1` passed; a scratch STA
  program confirmed the real clipboard receives a bitmap and a valid PNG entry.
- Gaps: no color picker (hex entry only, matching the MP4 background field);
  Copy Screenshot has no keyboard shortcut.
- Risks: a project saved with Custom cannot be opened by builds before this
  task, which reject the unknown background mode.
