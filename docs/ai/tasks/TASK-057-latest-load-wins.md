# TASK-057: Make the latest load win

Status: Completed on 2026-08-31.

## Objective

Prevent a slower earlier asset or project load from replacing a newer user
selection after the newer load has already completed.

## Context

- `docs/ai/16-ui-state-and-interaction-contracts.md`
- `docs/ai/decisions/ADR-004-ui-architecture-and-shell.md`
- Adjacent review identified that concurrent `OpenAssetAsync` and
  `OpenProjectAsync` completions are currently order-dependent.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/ShellViewModel.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `docs/ai/00-context-index.md`
- `docs/ai/16-ui-state-and-interaction-contracts.md`
- this task document

## Forbidden Paths

- Core, Application, Runtime, renderer, export, and project schema changes
- `runtimes/**` and vendored upstream code
- cancellation-token plumbing through every Runtime

## Required Behavior

- Each accepted asset/project open supersedes earlier in-flight opens.
- A stale successful asset load disposes its unused render session.
- A stale successful project load disposes every unused layer session.
- A stale failure cannot replace the newer load's state or diagnostics.
- Disposing the shell invalidates any in-flight load before it can apply.

## Acceptance Criteria

- A deterministic delayed-adapter smoke test starts a slow load, then a fast
  load, and verifies the fast/latest asset remains active.
- Existing replacement-failure preservation and source isolation still pass.
- WPF Release build, Application smoke, UI shell validation, and diff check pass.
- Documentation records the latest-request-wins rule.

## Validation

```text
dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

### Changed

- Each accepted asset or project open increments a load generation; only a
  successful result matching the current generation may update the shell.
- Stale asset sessions are disposed, and stale project results dispose every
  loaded layer session before returning.
- Shell disposal invalidates pending loads before clearing active layers.
- Deterministic smoke delays the first 4.1 load, completes a newer load, and
  verifies the older result cannot replace it.

### Preserved

- Existing atlas recovery, dirty confirmation, failure diagnostics, prior-frame
  preservation, Runtime selection, and source isolation remain unchanged.

### Validation

- Isolated WPF Release build: passed with 0 warnings and 0 errors.
- Application smoke: passed, including the deterministic overlapping-load case.
- UI shell validation: passed with 85 AutomationIds, 8 editor shortcuts, 24
  compact-workspace tokens, Slots availability, and Duplicate layer interaction.
- `git diff --check`: passed; Git only reported the repository's expected LF to
  CRLF checkout notice.

### Gaps and risks

- Earlier Runtime work is invalidated at the WPF boundary but not actively
  canceled; it only loses permission to apply and is disposed on completion.
