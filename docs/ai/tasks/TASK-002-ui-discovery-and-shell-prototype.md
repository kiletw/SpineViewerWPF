# TASK-002: Quick-Browse UI Discovery and Shell Prototype

## Status

Begin after TASK-000 captures v2 workflows. May run alongside TASK-001 only with fake Application state.

## Objective

Validate the shortest understandable path from open/drop to visible animation without putting Runtime, renderer, or export logic in WPF.

## Deliverables

1. annotated v2 quick-view workflow
2. low-fidelity quick-browse wireframe
3. empty, loading, ready, warning, unsupported, failed, and renderer-unavailable states
4. command and shortcut map
5. collapsed behavior at minimum window size
6. WPF shell backed by fake presentation state
7. stable automation identifiers
8. screenshots and interaction-count review
9. decision record for auto-play and remembered per-asset state

## Allowed

- WPF Views
- presentation-only state/ViewModels
- fake Application services
- sample DTOs and diagnostics
- UI automation metadata

## Forbidden

- official Runtime changes
- concrete renderer
- real export pipeline
- duplicate version detection
- GPU/Runtime objects in ViewModels
- product logic in code-behind

## Acceptance Criteria

- viewport is the primary surface
- animation selection is immediately accessible
- valid asset path needs no advanced panel
- primary commands are keyboard accessible
- all required states render from fake data
- open-to-preview interaction count is recorded
- no View/ViewModel references official Runtime types
