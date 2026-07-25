# TASK-003: Dockable Workspace Product Direction

## Status

Completed on 2026-07-26 as a documentation-only product decision.

## Objective

Record the owner-confirmed dockable workspace, external-reference boundaries, and deferred multilingual implementation before further WPF shell work.

## Context

- `../14-ui-product-design.md`
- `../15-ui-information-architecture.md`
- `../17-external-feature-reference.md`
- `../decisions/ADR-004-ui-architecture-and-shell.md`
- `../decisions/ADR-006-dockable-workspace-and-localization-boundary.md`

## Allowed Paths

- `docs/ai/**`

## Forbidden Paths

- `src/**`
- `tests/**`
- `runtimes/**`

## Required Behavior

- preserve the validated quick-browse default
- define dock, float, redock, hide/show, and reset-layout expectations
- preserve a localization boundary without implementing language switching
- separate competitor feature discovery from official Runtime authority

## Acceptance Criteria

- ADR-006 records the accepted direction and open toolkit choice
- UI product and information-architecture documents agree
- competitor features are inventory, not an implicit roadmap
- no production or Runtime code changes

## Validation

```powershell
git diff --check
rg "dock|float|localization|ww-rm/SpineViewer|spine-runtimes" docs/ai
```

## Completion Report

Changed: product direction and source boundaries.

Preserved: TASK-002 quick-browse behavior, Application/Runtime isolation, and all production code.

Known gaps: docking toolkit, layout persistence, translated resources, and runtime language switching remain unimplemented.
