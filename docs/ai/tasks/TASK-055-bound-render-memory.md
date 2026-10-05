# TASK-055: Bound render memory

Status: Completed on 2026-08-31.

## Objective

Reject render and export dimensions whose pixel buffers can exceed a documented
memory budget before opening Runtime sessions or allocating frame buffers.

## Context

- `docs/ai/05-technical-constraints.md`
- `docs/ai/decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- Adjacent review identified that the per-dimension 16384 limit still permits a
  16384-by-16384 buffer and that composite export retains several layer frames.

## Allowed Paths

- `src/SpineViewerWPF.Application/AssetService.cs`
- `tests/SpineViewerWPF.Application.Smoke/Program.cs`
- `docs/ai/00-context-index.md`
- `docs/ai/05-technical-constraints.md`
- `docs/ai/decisions/ADR-005-deterministic-cpu-renderer-spike.md`
- this task document

## Forbidden Paths

- Core, WPF, CLI, MCP, Runtime adapter, renderer implementation, and project schema changes
- `runtimes/**` and vendored upstream code
- new dependencies or speculative export scheduling

## Required Behavior

- File renders and exports allow at most 16,777,216 output pixels per frame.
- Composite export estimates retained BGRA layer plus output buffers and rejects
  requests above 256 MiB.
- Validation occurs before Runtime sessions, output directories, or files are created.
- Existing dimensions at or below the budget and the 10,000-frame streamed
  sequence limit remain unchanged.

## Acceptance Criteria

- Tests reject a dimension pair that passes the individual-axis limit but
  exceeds the pixel budget.
- Tests reject a composite whose retained frame estimate exceeds the memory budget.
- Application smoke, WPF Release build, UI shell validation, and diff check pass.
- Documentation records the budget and preserved behavior.

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

- File render, scene render, and sequence export reject frames above 16,777,216
  pixels while retaining the existing per-axis limits.
- Composite export counts visible layer BGRA frames plus the output BGRA frame
  and rejects an estimate above 256 MiB before creating sessions or directories.
- Application smoke covers every file-render entry point and a four-layer
  composite at the 4096-by-4096 axis limit.

### Preserved

- Public request types, supported dimensions within budget, deterministic output,
  one-frame-at-a-time sequence processing, and the 10,000-frame limit are unchanged.

### Validation

- Isolated WPF Release build: passed with 0 warnings and 0 errors.
- Application smoke: passed, including all new memory-budget rejection cases.
- UI shell validation: passed with 85 AutomationIds, 8 editor shortcuts, 24
  compact-workspace tokens, Slots availability, and Duplicate layer interaction.
- `git diff --check`: passed; Git only reported the repository's expected LF to
  CRLF checkout notice.

### Gaps and risks

- The 256 MiB value estimates retained output-sized BGRA frames. Runtime texture
  caches and encoder implementation overhead remain outside that estimate.
