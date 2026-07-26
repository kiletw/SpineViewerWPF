# TASK-011: Atlas Discovery for Direct Asset Open

## Status

Completed on 2026-07-27.

## Objective

Let the existing Application and WPF open flow resolve a nearby atlas when the caller does not provide one, including official exports whose skeleton filename has a `-pro` or `-ess` suffix.

## Context

- `../04-runtime-matrix.md`
- `../05-technical-constraints.md`
- `../09-ai-working-rules.md`
- `../decisions/ADR-002-runtime-isolation.md`
- `TASK-009-official-v41-compatibility.md`

## Allowed Paths

- `src/SpineViewerWPF.Application/AssetService.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-official-v41.ps1`
- `docs/ai/**`

## Forbidden Paths

- `runtimes/SpineRuntime.V41/src/**`
- Core, WPF, CLI, and legacy production source
- new packages or broad file discovery across the repository

## Required Behavior

- preserve an explicit atlas path exactly
- try the skeleton's same-stem `.atlas` first
- try the common `-pro` / `-ess` stem removal next
- accept a single nearby atlas as the last fallback
- report a clear missing/ambiguous atlas error instead of selecting arbitrarily

## Acceptance Criteria

- the existing minimal fixture still resolves `minimal.atlas`
- official `spineboy-pro.json` and `.skel` inspect successfully without `--atlas`
- explicit atlas selection remains unchanged
- no Runtime source or package changes

## Validation

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-official-v41.ps1 -Offline
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release -p:BaseOutputPath=.\artifacts\application-smoke\bin\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
git diff --check
```

## Completion Report

Changed: `AssetService` now resolves an omitted atlas using deterministic same-stem, `-pro`/`-ess` base-stem, and single-candidate rules. The official compatibility smoke now exercises both JSON and binary input without passing `--atlas`.

Preserved: explicit atlas paths, existing PPM/PNG rendering, source isolation, and all previous deterministic baselines.

Known gaps: multiple candidate atlases still require explicit user selection; drag/drop, recent files, and richer dependency diagnostics remain separate UI work.
