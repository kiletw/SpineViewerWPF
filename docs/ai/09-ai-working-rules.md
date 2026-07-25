# AI Working Rules

## One Active Task

Every change must reference one `TASK-xxx.md`. The task defines allowed paths, forbidden paths, behavior, and validation.

## Before Editing

1. confirm repository and branch
2. confirm clean/dirty working tree
3. read required context and active task
4. identify source of truth
5. state assumptions as `TBD` when unverified

## Editing Rules

- Keep tasks small and independently reviewable.
- Do not mix formatting, dependency upgrades, and product changes.
- Preserve public behavior unless the task approves a change.
- Prefer adapters around legacy/upstream code over mass modification.
- Never modify `runtimes/**` merely to simplify Application code.
- Avoid placeholders in production code unless the task explicitly creates a prototype.

## Git Rules

- v2 fixes branch from `legacy/v2`.
- v3 work branches from `v3`.
- Protected branches are never edited directly.
- Suggested names: `docs/*`, `test/*`, `refactor/*`, `feat/*`, `fix/*`.

## Completion Report

```text
Changed
Preserved
Validation
Known gaps
Risks
Documentation
Recommended next task
```

Do not start the recommended next task without instruction.
