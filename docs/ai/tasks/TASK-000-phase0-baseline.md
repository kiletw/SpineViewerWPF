# TASK-000: Phase 0 Baseline Inventory

## Status

Completed on 2026-07-26 against v2 commit `79c6135` (`2.4.0.0`).

## Objective

Create a verified v2 architecture, Runtime, behavior, build, and fixture baseline before v3 implementation.

## Allowed Paths

- `docs/ai/**`
- non-redistributable local fixture manifest files
- build scripts that do not alter product behavior

## Forbidden Paths

- official Runtime source
- existing Player behavior
- UI behavior
- export implementation

## Work Items

1. Document clean checkout and build prerequisites.
2. List projects, source directories, dependencies, and large/high-risk classes.
3. Record every Runtime snapshot and likely upstream commit/tag/branch.
4. Compare every `Player_x_x_xx` implementation.
5. Inventory static mutable state and resource ownership.
6. Trace load, playback, render, capture, export, reload, and shutdown flows.
7. Create a fixture manifest for each supported export line.
8. Confirm or correct behavior contracts.
9. Populate Runtime matrix.
10. Produce prioritized risks and candidate first vertical slice.

## Acceptance Criteria

- no product behavior changed
- all unknowns are explicit
- build is reproducible or blockers are documented
- each Runtime line has provenance and fixture status
- flows and resource ownership are documented

## Validation

- clean checkout build attempt
- executable smoke test where available
- documentation path/link check

## Completion Evidence

- Clean-source restore and Debug build succeeded with legacy `nuget.exe` plus Visual Studio 2022 full-framework MSBuild.
- Startup smoke reached a responsive `SpineViewerWPF v2.4.0.0` main window.
- All Markdown local links under `docs/ai/` resolve.
- [`../fixtures/manifest.json`](../fixtures/manifest.json) parses and contains all 14 v2 Runtime selections.
- Product, UI, Player, export, and vendored Runtime source were not changed.

## Explicit Gaps

- No Spine asset fixture is present, so compatibility, PMA, multi-page atlas, and rendered output remain unverified.
- Exact upstream source equivalence of locally namespace-renamed Runtime snapshots is unverified; 13 same-name official tag candidates are recorded and 3.2.xx remains `TBD`.
- Build retains AnyCPU/x86 XNA warnings and known package vulnerability advisories.
- ADR-001, ADR-002, and ADR-003 remain Proposed, so TASK-001 remains blocked until they are accepted or revised.
