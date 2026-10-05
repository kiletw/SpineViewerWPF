# TASK-043: Layer-Focused Workspace

## Status

Completed on 2026-08-10.

## Objective

Make layer selection the single editing context by separating the Layers and Properties workflows, adding reusable layer actions, and replacing range-limited transform sliders with precise numeric input.

## Context

- `docs/ai/14-ui-product-design.md`
- `docs/ai/15-ui-information-architecture.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/decisions/ADR-004-ui-architecture-and-shell.md`
- `docs/ai/decisions/ADR-006-dockable-workspace-and-localization-boundary.md`
- `docs/ai/decisions/ADR-007-non-destructive-viewer-project.md`
- Competitor screenshots supplied on 2026-08-10 show a model list, context actions, parameter copy/paste, and property categories.

## Design Read

Preserving redesign of a desktop Spine workspace for advanced asset users, using a compact Fluent/IDE-style WPF language.

- `DESIGN_VARIANCE: 3`
- `MOTION_INTENSITY: 2`
- `VISUAL_DENSITY: 8`
- Native WPF only; no new visual or docking package.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/**`
- `scripts/test-ui-shell.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/**`
- `src/SpineViewerWPF.Core/**`
- `src/SpineViewerWPF.Application/**`
- `src/SpineViewerWPF.Cli/**`
- vendored official Runtime source
- project schema changes
- new packages
- attachment editing, multi-track playback, debug geometry, or new export formats

## Required Behavior

- Browse owns the scene layer list and asset identity; selected-layer editors no longer remain stacked beneath it.
- Properties is grouped into clear Animation, Transform, Render, Appearance, Slots, and Viewport categories.
- Transform values use precise numeric fields and accept finite values beyond the old slider limits.
- The selected-layer context menu supports add, duplicate, reload, remove, reorder, parameter copy, and parameter paste.
- Parameter copy/paste supports all, transform, render, and appearance scopes without copying source identity or z-order.
- Duplicate and reload preserve the source layer's editable settings; failures preserve the existing scene and report a diagnostic.
- Existing sidecar save, GPU/CPU rendering, screenshot/export, drag/drop, docking, theme, playback, and Runtime isolation remain intact.
- New visible strings use resource identities.

## Acceptance Criteria

- The default workspace has one clear layer list and one clear selected-layer Properties surface.
- Position, scale, and rotation are editable without the old `-64..64` and `-100..100` slider limits.
- Copy/paste enablement is deterministic and scoped to the selected layer.
- Existing automation identities remain stable where the represented control still exists; new commands receive stable identities.
- WPF Release build has zero warnings and zero errors.
- Application smoke and UI shell validation pass.
- Documentation records shipped and deferred competitor-derived behavior.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore -p:BaseOutputPath=artifacts/application-smoke/bin/
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

### Changed

- Browse now owns the layer list and asset identity while the Inspector is the single selected-layer Properties surface.
- Properties is split into Animation, Transform, Render, Appearance, Slots, and Viewport categories.
- Transform and common render values use precise numeric fields; Enter commits the value and finite transforms are no longer constrained by the former slider ranges.
- Layer actions now include duplicate, reload, remove, reorder, and scoped parameter copy/paste. Duplicate opens an independent render session; reload replaces the existing layer only after a successful open.
- Layer parameter copy/paste supports all, transform, render, and appearance scopes without copying source paths or z-order. Appearance applies only matching animation, skin, and slot identities on the target asset.
- New user-visible strings use WPF resource identities and the implementation adds no package or schema dependency.

### Preserved

- Existing sidecar load/save, undo/redo, GPU/CPU rendering, screenshot/export, drag/drop opening, bounded float/redock panels, Dark/Light theme selection, playback, Runtime selection, and failure diagnostics remain on their existing boundaries.
- No Core, Application, CLI, Runtime, vendored source, project schema, or package change was made for this task.

### Validation

- `dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore`: passed, zero warnings and zero errors.
- Application smoke: passed (`Viewer project, PNG render preview, edit history, validation, and source isolation passed.`).
- `scripts/test-ui-shell.ps1`: passed with 72 automation identities, 7 editor shortcuts, 14 compact-workspace tokens, floating Inspector binding coverage, and a real V42 fixture duplicate-layer action.
- Dark workspace inspected from `artifacts/task-043-workspace-window.png`; tab order, numeric-field contrast, viewport priority, and panel hierarchy are legible.
- `git diff --check`: passed; only existing line-ending conversion warnings were reported by later diff/status inspection.

### Gaps and Risks

- Attachment selection, multi-track mixing, physics controls, debug geometry, asset-folder browsing, and new export formats remain deferred.
- Docking remains the existing bounded float/redock workflow rather than unrestricted Adobe-style drag docking.
- Cross-asset appearance paste intentionally ignores names that do not exist in the target asset.
- Dark mode received direct visual inspection; Light mode continues through the existing dynamic theme resources but was not separately captured for this task.

### Documentation

- Updated `00-context-index.md`, `14-ui-product-design.md`, `15-ui-information-architecture.md`, `16-ui-state-and-interaction-contracts.md`, and `17-external-feature-reference.md` with the delivered workspace contract and deferred competitor-derived features.

### Next Task

- Recommended next slice: attachment selection/editing inside the Slots/Appearance workflow, with explicit sidecar and render/export behavior before adding multi-track or debug overlays.
