# TASK-047: Converge the repository on the v3 implementation

## Objective

Make the `feat/v3-playback` branch a merge-ready v3 baseline with one obvious build entry, no legacy v2 Viewer/XNA product source, and no active Runtime project referencing `SpineViewerWPF/SpineLibrary`.

## Context

- `docs/ai/01-project-identity.md`
- `docs/ai/02-current-architecture.md`
- `docs/ai/05-technical-constraints.md`
- `docs/ai/06-target-architecture.md`
- `docs/ai/07-migration-plan.md`
- `docs/ai/decisions/ADR-009-opentk-gpu-preview.md`
- PR #53 currently targets `master` from `feat/v3-playback`.

The v3 projects live under `src/`, `runtimes/`, and `tests/`, but the root solution still opens only the legacy .NET Framework/XNA project. Historical v3 Runtime adapters also compile source from the legacy `SpineViewerWPF/SpineLibrary` tree.

## Allowed Paths

- `SpineViewerWPF.sln`
- `SpineViewerWPF/**`
- `runtimes/**`
- `README.md`
- `README_zhTW.md`
- `docs/ai/**`
- build and validation scripts when required by the new solution entry

This task explicitly permits moving the historical Runtime snapshots out of `SpineViewerWPF/SpineLibrary` and updating project-owned Runtime project files/metadata. Runtime `.cs` contents must otherwise remain byte-for-byte unchanged.

## Forbidden Paths

- Product behavior changes under `src/**`
- Fixture behavior changes under `tests/fixtures/**`
- Runtime API refactors or cleanup inside vendored `.cs` files
- Spine 4.3 implementation
- New dependencies

## Required Behavior

- `SpineViewerWPF.sln` opens and builds the v3 implementation, not the legacy v2 executable.
- The legacy WPF/XNA Viewer, Players, resources, `packages.config`, and old project file are absent from the v3 branch.
- Historical Runtime source remains available under the matching `runtimes/SpineRuntime.*` project and continues to compile through the existing adapters.
- No active project or script references `SpineViewerWPF/SpineLibrary`.
- Existing Runtime selection, deterministic render output, GPU preview, CPU fallback, slot controls, and Preview FPS behavior are preserved.
- The v2 implementation remains recoverable from Git history and `master` before PR #53; no backup copy is added to the v3 tree.

## Acceptance Criteria

- The root contains no `SpineViewerWPF/SpineViewerWPF.csproj` or legacy product source tree.
- `rg "SpineViewerWPF[/\\\\]SpineLibrary|SpineViewerWPF[/\\\\]SpineViewerWPF.csproj"` finds documentation history only, not active build references.
- Vendored Runtime `.cs` source hashes before and after relocation match by Runtime version.
- Root solution Release build succeeds with zero errors.
- Application smoke, `scripts/test-v3.ps1`, `scripts/test-v42.ps1 -Offline`, and UI shell validation pass.
- Repository documentation names the v3 WPF/CLI entry points and current prerequisites.
- PR #53 is updated only after validation succeeds.

## Validation

```text
dotnet build SpineViewerWPF.sln -c Release --no-restore
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v42.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

### Changed

- Replaced the legacy-only root solution with a 20-project v3 solution.
- Removed the 43-file v2 .NET Framework/XNA Viewer and its package/config/resource surface.
- Relocated 484 active historical Runtime `.cs` inputs into the matching
  `runtimes/SpineRuntime.V*/src` projects.
- Removed 61 unreferenced XNA Runtime helpers and the unused 52-file v2 4.1.00
  duplicate after explicit product-owner approval.
- Updated the English/Traditional Chinese README files and stabilized the Slot
  UI automation interaction against the current workspace layout.

### Preserved

- All 13 historical Runtime pre/post manifests match byte-for-byte.
- Runtime selection from 2.1.08 through 4.2, deterministic CPU rendering,
  OpenTK GPU preview, slot editing, Undo/Redo, and Preview FPS behavior remain
  unchanged.
- The v2 implementation remains recoverable from Git history and the pre-PR
  `master` state; no backup copy is carried in the v3 tree.

### Validation

- Root solution Release build: 20 projects, 0 warnings, 0 errors.
- All 13 historical Runtime Release builds: 0 warnings, 0 errors.
- Application smoke: passed.
- `scripts/test-v3.ps1`: passed, including 12 historical Runtime deterministic
  checks plus 4.0, 4.1, 4.2, and PMA coverage.
- `scripts/test-v42.ps1 -Offline`: official 4.2.22 JSON/binary inspect and
  deterministic render passed.
- `scripts/test-ui-shell.ps1`: 74 AutomationIds; floating bindings, Slot
  visibility, Undo/Redo, and Duplicate layer interactions passed; build 0
  warnings, 0 errors.
- Active old-path references: 0. `git diff --check`: passed.

### Gaps

- Runtime 4.3, multi-layer deterministic export, and Spine source write-back
  remain separate future tasks.

### Risks

- Historical Runtime provenance remains less exact than the clean pinned 4.1
  and 4.2 snapshots; relocation proves byte identity, not upstream origin.
- The Slot DataTemplate is not exposed through WPF's UIA tree, so the UI shell
  test anchors its physical click to the selected Slots tab header bounds.

### Documentation

- `README.md`, `README_zhTW.md`
- `docs/ai/00-context-index.md`
- `docs/ai/04-runtime-matrix.md`
- `docs/ai/07-migration-plan.md`
- `docs/ai/evidence/task-047/runtime-relocation.md`

### Next task

Merge PR #53 as the v3 baseline, then start the next independently scoped
feature branch from updated `master`.
