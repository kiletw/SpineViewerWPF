# TASK-044: Slot Attachment Selection

## Status

Completed on 2026-08-10.

## Objective

Let users select a named attachment for each slot and preserve that non-destructive choice across interactive GPU/CPU preview, deterministic render/export, duplicate/reload, parameter copy/paste, and the `*.spineviewer.json` sidecar.

## Context

- `docs/ai/05-technical-constraints.md`
- `docs/ai/12-application-use-cases.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- `docs/ai/decisions/ADR-007-non-destructive-viewer-project.md`
- `docs/ai/decisions/ADR-009-opentk-gpu-preview.md`
- Competitor Slots workflow supplied on 2026-08-10 shows a slot search, attachment selector, and visibility toggle.

## Design Read

Preserving redesign of a high-density desktop Spine workspace for advanced asset users, using the existing compact Fluent/IDE-style WPF language.

- `DESIGN_VARIANCE: 3`
- `MOTION_INTENSITY: 2`
- `VISUAL_DENSITY: 8`
- Native WPF controls only; no new package or docking framework.

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`
- `src/SpineViewerWPF.Application/ViewerProjectStore.cs`
- `src/SpineViewerWPF.Wpf/**`
- project-owned adapter bridges:
  - `runtimes/SpineRuntime.Legacy/Adapter.cs`
  - `runtimes/SpineRuntime.V40/Adapter.cs`
  - `runtimes/SpineRuntime.V41/Adapter.cs`
  - `runtimes/SpineRuntime.V42/Adapter.cs`
- `tests/SpineViewerWPF.Application.Smoke/**`
- `tests/fixtures/v42-minimal/**`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- vendored official Runtime source under `SpineViewerWPF/SpineLibrary/**` and `runtimes/SpineRuntime.V41/src/**` / `runtimes/SpineRuntime.V42/src/**`
- project schema version changes
- Spine source writes
- new packages
- attachment transforms, attachment creation/deletion, multi-track playback, debug geometry, or new export formats

## Required Behavior

- Inspect metadata exposes each slot, its setup attachment, and the union of named attachments found across skins.
- A slot may keep animation/setup behavior or explicitly select one known attachment.
- Explicit attachment selection is applied after animation posing and before world transform/rendering.
- GPU preview receives the same slot settings as CPU preview and export.
- The sidecar stores an optional attachment name inside the existing schema-version-1 slot document; old files without it retain current behavior.
- Invalid or unavailable saved attachment names fall back to animation/setup behavior without unloading the asset.
- Slot attachment edits dirty the project and participate in the existing undo/redo snapshot.
- Duplicate/reload and scoped appearance copy/paste preserve matching attachment selections.
- New visible strings use resource identities.

## Acceptance Criteria

- The V42 minimal fixture exposes at least two selectable attachments for one slot.
- Selecting the alternate attachment changes deterministic CPU output and Runtime-neutral GPU scene geometry.
- Repeated A-B-A attachment requests remain deterministic in one reusable Runtime session.
- Sidecar round-trip preserves the optional attachment name and older slot JSON remains valid.
- WPF Slots properties show a searchable slot row with attachment selector, visibility, and opacity.
- WPF Release build has zero warnings and zero errors.
- Application smoke and UI shell validation pass.
- `git diff --check` passes.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore -p:BaseOutputPath=artifacts/application-smoke/bin/
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

### Changed

- Added Runtime-neutral slot metadata with slot name, setup attachment, and the union of named attachments across skins.
- Added the backward-compatible optional `attachmentName` field to schema-version-1 slot display settings.
- Project-owned Legacy, 4.0, 4.1, and 4.2 adapters apply valid attachment overrides after animation posing and before rendering; unknown names retain the animated/setup attachment.
- GPU preview now sends the same slot document as CPU preview and export. The obsolete GPU-side visibility/opacity multiplication was removed so opacity is applied exactly once.
- The WPF Slots category now exposes a localized `Animation / setup` choice plus named attachments for every metadata-backed slot.
- Attachment edits dirty the Viewer project and use the existing Undo/Redo commands. The shared layer snapshot also makes other non-structural selected-layer property edits restorable.
- The V42 project fixture now has `square` and `tall` attachments sharing the same tiny test texture.

### Preserved

- Old sidecars without `attachmentName` load with animation/setup behavior.
- Spine JSON, binary, atlas, texture, and vendored Runtime source remain read-only.
- Existing animation, skin, visibility, opacity, Track 0 Alpha, PMA, multi-layer, duplicate/reload, capture/export, GPU fallback, theme, and docking behavior remain on their existing boundaries.
- The V42 setup render baseline remains `9F94B3309ABFC350372E67ED894971BF24A230C9087BF19C4AC628720579C7A9`.
- No package, project schema version, or source-writing capability was added.

### Validation

- WPF Release build: passed with zero warnings and zero errors across every registered Runtime project.
- Application smoke: passed. Coverage includes slot metadata, alternate CPU output, alternate Runtime-neutral scene geometry, exactly-once opacity, invalid-name fallback, A-B-A determinism, schema-version-1 round-trip, old JSON compatibility, dirty state, and Undo.
- UI shell: passed with 73 automation identities, 7 shortcuts, 15 compact-workspace tokens, floating Inspector binding, attachment binding, and real duplicate-layer action.
- Direct V42 CLI setup render: passed and retained the recorded 64 by 64 hash.
- Dark 1280 by 800 Slots workspace inspected at `artifacts/task-044-slot-attachment.png`; selector width, contrast, tab layout, and viewport priority are legible.
- `git diff --check`: passed after implementation; final documentation receives the same check before handoff.

### Known Gaps

- This selects existing named attachments only. Attachment transforms, creation, deletion, and Spine source writing remain unsupported.
- The attachment list is the union across skins. A name unavailable in the currently selected skin safely falls back to animation/setup behavior.
- Selecting a name intentionally overrides attachment animation until `Animation / setup` is chosen again.
- Undo/Redo now covers non-structural layer property snapshots; removing or adding a Runtime-backed layer is still not reconstructed by Undo.

### Risks

- The embedded OpenGL viewport can cause UI Automation to block when programmatically expanding a WPF ComboBox. The smoke test therefore verifies the stable control identity/binding while Application smoke executes the actual attachment selection and render change.
- Large production assets may expose many attachment names per slot; the existing slot search limits which rows are visible, but attachment-name filtering inside an expanded ComboBox remains deferred until measured.

### Documentation

- Updated the context index, Application use cases, UI product design, information architecture, interaction contracts, external reference mapping, ADR-007, and fixture manifest.

### Recommended Next Task

- Add multi-track animation inspection and mixing as a separate Runtime-neutral contract. Attachment authoring should remain deferred until a safe editable source format is explicitly designed.
