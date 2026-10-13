# TASK-076: About dialog

Status: Implemented on 2026-10-14.

## Objective

Help > About shows the application version, supported Spine Runtime lines, the
Spine licensing requirement, environment facts, and third-party notices, and can
copy those facts for bug reports.

## Context

- Product-owner request (2026-10-14), ahead of localization so its text is
  resource-based from the start.
- Spine Runtime licensing requires every user to hold a Spine Editor license;
  this was stated only in README and `THIRD-PARTY-NOTICES.md`.
- Release packages ship `THIRD-PARTY-NOTICES.md` and `licenses/` beside the
  executable (TASK-069); versions come from the release tag via `Directory.Build.props`.

## Allowed Paths

- `src/SpineViewerWPF.Application/AssetService.cs` (`RuntimeLines` only)
- `src/SpineViewerWPF.Wpf/**`
- `tests/SpineViewerWPF.Application.Smoke/**` (About checks; settle wait in `SetupPoseSmoke`)
- `docs/ai/00-context-index.md`, `docs/ai/16-ui-state-and-interaction-contracts.md`, this task

## Forbidden Paths

- `runtimes/**`, export and rendering behavior
- new packages

## Required Behavior

- `AssetService.RuntimeLines` lists the registered Runtime lines.
- `AboutInfo` shows the informational version without build metadata, Runtime
  lines in numeric order ("4.0" = 4.0.64 after 4.0.31), the preview renderer,
  and FFmpeg status; `CopyText` adds Windows and .NET versions.
- The About window shares the owner's theme and styles, uses only `Text.About*`
  resources, links to the Spine Editor license, GitHub, and Releases, opens the
  packaged `THIRD-PARTY-NOTICES.md` (or the repository copy in development
  builds), and copies the info text.

## Acceptance Criteria

- Smoke: version normalization, 16 ordered Runtime lines, complete copy text.
- The built application opens Help > About with the themed window.
- Build, Application.Smoke, and `scripts/test-ui-shell.ps1` pass.

## Validation

```text
dotnet build SpineViewerWPF.sln -c Release
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

- Changed: `AboutInfo`, `AboutWindow`, Help > About menu item, `Text.About*`
  resources, `AssetService.RuntimeLines`, `AboutSmoke`.
- Also fixed: a race in `SetupPoseSmoke` introduced with TASK-071, where the
  post-open fit re-render could be captured as the skin-change frame; the test
  now captures settled frames (5 of 5 consecutive runs passed).
- Validation (2026-10-14): build clean; Application.Smoke and
  `test-ui-shell.ps1` passed; a live capture showed the themed About window with
  version 3.0.0-dev, the 16 Runtime lines, and the FFmpeg path.
- Gaps: text is read when the window opens (language changes need a restart, as
  planned for TASK-077).
