# TASK-023: Runtime 4.0.64 Compatibility

## Status

Completed on 2026-07-27.

## Objective

Add a project-owned 4.0.64 Runtime adapter beside the verified 4.1 adapter, expose explicit and deterministic Runtime selection through Application/CLI/WPF composition, and verify a project-authored 4.0 JSON fixture without changing Spine source files.

## Context

- `../04-runtime-matrix.md`
- `../06-target-architecture.md`
- `../07-migration-plan.md`
- `../decisions/ADR-002-runtime-isolation.md`
- `runtimes/SpineRuntime.V41/Adapter.cs`
- `src/SpineViewerWPF.Application/AssetService.cs`

## Allowed Paths

- `runtimes/SpineRuntime.V40/**`
- `runtimes/SpineRuntime.V41/CpuRenderer.cs`
- `runtimes/SpineRuntime.V41/PngReader.cs`
- `runtimes/SpineRuntime.V41/PngWriter.cs`
- `src/SpineViewerWPF.Application/**`
- `src/SpineViewerWPF.Cli/**`
- `src/SpineViewerWPF.Wpf/App.xaml.cs`
- `src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj`
- `tests/fixtures/v40-minimal/**`
- `tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `scripts/test-v3.ps1`
- `docs/ai/**`

## Forbidden Paths

- `SpineViewerWPF/SpineLibrary/**` source edits
- `runtimes/SpineRuntime.V41/src/**` source edits
- Spine source writes
- new packages or renderer framework changes

## Required Behavior

- 4.0.64 JSON can be inspected and rendered through the same Core/Application contracts as 4.1.
- Explicit Runtime overrides select the requested verified adapter; omitted overrides resolve by successful inspection.
- WPF and CLI composition include both 4.0 and 4.1 adapters without exposing Runtime-specific types through Application contracts.
- Existing 4.1 deterministic and official compatibility behavior remains unchanged.

## Acceptance Criteria

- Application smoke inspects and renders the 4.0 fixture and asserts the selected Runtime line.
- v3 smoke validates deterministic 4.0 output plus the existing 4.1 hash.
- WPF, v3, official 4.1 offline, and `git diff --check` validations remain green.
- Runtime matrix and context index state the verified 4.0 scope and remaining binary/official-fixture gap.

## Validation

```powershell
dotnet run --project .\tests\SpineViewerWPF.Application.Smoke\SpineViewerWPF.Application.Smoke.csproj -c Release -p:BaseOutputPath=.\artifacts\application-smoke\bin\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui-shell.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-v3.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-official-v41.ps1 -Offline
git diff --check
```

## Completion Report

Implemented `SpineRuntime.V40` as an isolated adapter over the read-only vendored 4.0.64 snapshot. The Application service now resolves adapters deterministically by explicit line or successful inspection; CLI and WPF composition include 4.0.64 and 4.1. The shared CPU renderer/PNG path has small compile-time API bridges for the older Runtime, and a project-owned `System.Text.Json` decoder preserves the historical parser contract without adding a package.

Validation passed:

- Application smoke: 4.0.64 auto-selection, explicit selection, deterministic render, and unsupported override behavior.
- v3 CLI smoke: existing 4.1 hash plus 4.0.64 inspect/render hash `E16719DD53FB8CACED7D8C28E4BDD50DBEB8812483B041EC640913C13C09D28B`.
- WPF shell smoke: 39 automation IDs and startup launch.
- Official 4.1 offline compatibility smoke.
- `git diff --check`.

Known gaps: only the project-authored 4.0.64 JSON fixture is verified; official 4.0.64 JSON/binary exports, 3.8.95, PMA, clipping, non-normal blend modes, and multi-page atlases remain pending.
