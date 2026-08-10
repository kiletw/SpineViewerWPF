# SpineViewerWPF Agent Instructions

## Identity

This project is `kiletw/SpineViewerWPF`. Do not confuse it with `ww-rm/SpineViewer`.

## Required Read Order

1. `docs/ai/00-context-index.md`
2. `docs/ai/05-technical-constraints.md`
3. `docs/ai/09-ai-working-rules.md`
4. the current task under `docs/ai/tasks/`
5. relevant ADRs under `docs/ai/decisions/`

## Default Restrictions

- Treat `runtimes/**` and vendored upstream code as read-only unless the task explicitly permits a patch.
- Do not expose Runtime-specific types through Core or Application APIs.
- Do not add WPF, GPU, CLI, or MCP dependencies to Core.
- Do not duplicate use-case logic in WPF, CLI, or MCP adapters.
- Do not modify protected or integration branches directly.
- Do not broaden scope beyond the active task.
- Do not replace a verified behavior with a cleaner design unless the task explicitly changes the contract.

## Completion Requirements

Before declaring a task complete:

- run the validation commands listed by the task
- report changed files and behavior
- report preserved behavior
- report build and test status
- report unresolved risks and assumptions
- update relevant `docs/ai/` files
