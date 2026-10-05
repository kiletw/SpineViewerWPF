# TASK-059: Setup pose and per-asset selection memory

Status: Completed on 2026-10-02 (automated validation; manual GUI walkthrough not performed).

## Scope

User-authorized behavior fixes on `codex/ui-workspace-refinement`, starting at
`c998e96`. Preserve all pre-existing TASK-058 UI and instruction-maintenance work.
No commit, push, branch switch, new project, framework, or UI redesign.

## Allowed paths

- `src/SpineViewerWPF.Core/Contracts.cs` (contract documentation only)
- `src/SpineViewerWPF.Application/**`
- `src/SpineViewerWPF.Wpf/**` (loading, selection, setup-pose enablement and text resources)
- `src/SpineViewerWPF.Cli/Program.cs` (existing render command only)
- Project-owned `runtimes/SpineRuntime.{Legacy,V40,V41,V42,V43}/Adapter.cs`
- `tests/SpineViewerWPF.Application.Smoke/**`, `tests/fixtures/**`, `scripts/test-v3.ps1`, `scripts/test-ui-shell.ps1`
- `README.md`, `README_zhTW.md`, relevant `docs/ai/**`

## Forbidden paths

- Official Runtime `runtimes/**/src/**`, other vendored source and Runtime project structure
- Unrelated instruction changes, GPU architecture, CI, and unrelated cleanup

## Behavior

- Render requests and scene-layer documents use `Animation = ""` for setup pose.
  Shell `SelectedAnimation` uses null for no selection; no synthetic animation is added.
  Null/whitespace render names remain invalid. Named animations still must exist.
- No-animation assets inspect, load, create/duplicate/reload layers, select skins,
  render CPU frames and neutral GPU scenes, and round-trip sidecars normally.
- Static setup pose does not advance Physics. Pure static WPF scenes have disabled
  playback and export one frame (duration zero under the existing export schedule).
  A static layer remains static when composed with animated layers.
- Selection memory lasts for one Shell/workspace instance, keyed by the existing
  canonical skeleton path, compared case-insensitively on Windows. No disk cache.
- Direct open/reload uses valid remembered selection, otherwise first available
  animation/skin (no animation => setup pose). Sidecar values take precedence.
- Only successfully applied layers and their edits update memory; stale load
  results neither publish selection nor mutate memory. Preserve generation cleanup.
- Correct README claims and TASK-058 notes to match evidence, without changing
  the Layers layout. Resourceize only newly introduced visible text.

## Validation

- `dotnet build SpineViewerWPF.sln -c Release --no-restore`
- `dotnet run --project tests/SpineViewerWPF.Application.Smoke -c Release --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1`
- Official fixture scripts: `test-official-v38.ps1`, `test-official-v41.ps1`,
  `test-v42.ps1`, `test-v43.ps1`, with `-Offline`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1`
- `git diff --check`

Add behavioral coverage for setup pose across all registered Runtime lines,
CPU/neutral scene rendering, skins, layer operations, sidecar, capture/export,
A-B-A selection, path aliases, changed inspection, explicit sidecar precedence,
and latest-result races. Report unavailable GUI/cache validation honestly.

## Completion Report

### Changed

- Core `Contracts.cs`: documents empty `Animation` as setup pose (comment only).
- Application: `AssetRenderSession`, scene-layer open, scene export, and
  `ViewerProjectStore` accept `""` as setup pose and still reject null or
  whitespace-only names.
- Project-owned Runtime adapters (Legacy, V40, V41, V42, V43): skip
  `AnimationState` for setup pose; named animations still must exist.
- CLI `render`: `--animation` may be omitted for no-animation assets only.
- WPF Shell: null `SelectedAnimation` for no-animation assets, disabled playback
  for zero-duration layers, one-frame static export, and per-workspace
  animation/skin memory keyed by canonical skeleton path.
- Tests: `SetupPoseSmoke.cs`, `tests/fixtures/v41-setup-pose/`, setup-pose
  checks in `test-v3.ps1` and `test-ui-shell.ps1`.
- Docs: README/README_zhTW claims, `03-behavior-contracts.md`,
  `10-cli-contract.md`, TASK-058 wording corrections.

### Validation (2026-10-02, Windows, local)

- `dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false`: passed, 0 warnings, 0 errors.
- Application.Smoke (`SPINEVIEWER_SKIP_WINDOW_SMOKE=1`): passed, including
  "16 Runtime setup poses, static layer/sidecar/skin/export and asset selection memory".
- `scripts/test-v3.ps1`: passed (SetupPose, Inspect, Render, PMA, Runtime 4.0/4.2/4.3, 12 historical Runtimes).
- `test-official-v38.ps1`, `test-official-v41.ps1`, `test-v42.ps1`, `test-v43.ps1` with `-Offline`: passed against cached official assets.
- `scripts/test-ui-shell.ps1`: passed (SetupPoseUi passed on GPU backend, 85 AutomationIds).
- `git diff --check`: passed.

### Known Gaps

- Manual GUI walkthrough with a real no-animation asset was not performed;
  UI Automation covers the setup-pose Shell state.
