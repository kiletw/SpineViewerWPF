# SpineViewerWPF Agent Instructions

## Identity

This project is `kiletw/SpineViewerWPF`. Do not confuse it with `ww-rm/SpineViewer`.

## Scope and Rule Resolution

These instructions apply throughout this repository. Follow applicable ancestor rules and more specific directory instructions; this file cannot override higher-priority session instructions or tool permissions. Explicit user instructions define the authorized task and take precedence over repository workflow defaults.

For repository workflow, this file is the entry point. Its reading, task-record, and completion conditions qualify the unconditional workflow wording in `docs/ai/00-context-index.md` and `docs/ai/09-ai-working-rules.md`. Those documents remain context and supporting guidance. Technical constraints, Accepted ADRs, behavior contracts, and task-specific acceptance criteria remain binding for work that touches them. If applicable contracts conflict and the user has not resolved the conflict, ask only about the affected decision and continue independent authorized work.

## Reading and Task Scope

For project work, read in this order, reusing context already read in the current task:

1. `docs/ai/00-context-index.md` as an index. Its longer read list is orientation for broad implementation or architecture work, not a requirement to read every document for every request.
2. `docs/ai/05-technical-constraints.md`.
3. `docs/ai/09-ai-working-rules.md`, subject to the workflow conditions here.
4. The task under `docs/ai/tasks/` that actually matches the user's request, if one exists. Do not resume a completed task merely because it is the latest numbered file or has uncommitted changes.
5. Only the behavior, surface-specific documents, and Accepted ADRs relevant to the requested work. Read broader architecture context when making cross-layer or architectural changes.

Implementation that changes product behavior, public contracts, architecture, dependencies, or Runtime compatibility must reference a `TASK-xxx.md` defining allowed and forbidden paths, behavior, and validation. If the request authorizes that implementation but no task exists, create a focused task record within the authorized scope; missing paperwork alone is not a reason to stop for approval. Explanations, read-only diagnosis/review, and bounded documentation or instruction maintenance can use the user's request as the task without creating a numbered record. Do not expand a task's scope merely to accommodate unrelated work.

## Authorization and Execution

- Explanation, diagnosis, review, or planning alone does not authorize repairs. Return findings unless the user also requests implementation.
- For authorized implementation, inspect repository identity, branch, and working-tree status before editing. Here and in `09-ai-working-rules.md`, "confirm" means verify locally, not ask the user to reconfirm known facts.
- Preserve existing staged, unstaged, and untracked work. Do not reset, overwrite, discard, or incorporate unrelated changes into this task.
- Complete clear, authorized work and appropriate validation rather than stopping after a plan. Resolve routine reversible choices locally. Ask only when missing information materially affects correctness or the next action exceeds authorization; continue independent work while awaiting an answer.
- Mark consequential unverified assumptions as `TBD`. Report environmental blockers and failed checks honestly; do not claim completion when required work remains.
- Do not infer authorization to deploy, publish, send messages, access or disclose credentials, or delete important data from a request to edit or validate. Preserve existing permissions and require authorization covering those actions. Do not read credentials, session histories, or unrelated caches for ordinary project work.
- No default requirement for multiple models, sub-agents, exhaustive research, or full-suite testing. Use a Skill only when explicitly requested or applicable to the task; its specialized workflow does not become a requirement for unrelated tasks. Preserve explicit model routing and user preferences.

## Default Restrictions

- Treat `runtimes/**` and vendored upstream code as read-only unless the task explicitly permits a patch.
- Do not expose Runtime-specific types through Core or Application APIs.
- Do not add WPF, GPU, CLI, or MCP dependencies to Core.
- Do not duplicate use-case logic in WPF, CLI, or MCP adapters.
- Do not modify protected or integration branches directly.
- Do not broaden scope beyond the active task.
- Do not replace a verified behavior with a cleaner design unless the task explicitly changes the contract.

For branch selection, preserve the v2/v3 base rules in `09-ai-working-rules.md`: v2 fixes start from `legacy/v2`, and new v3 work starts from `master` (the v3 integration and release line). An existing suitable task branch can be continued. Do not recreate or switch it merely to follow a naming suggestion. If a new branch is needed, follow the user's/session's branch naming preference; the prefixes in that document are suggestions. Never discard working-tree changes to change branches.

## Completion Requirements

For implementation, run the active task's required validation commands. Do not silently waive applicable acceptance checks. Add focused checks when the actual change introduces a risk those commands do not cover; broaden or repeat testing only for new evidence, changes, failures, or unresolved concerns. Do not import checks from unrelated or completed tasks.

For bounded documentation/instruction edits without a numbered task, review the diff, check referenced paths and consistency, and run `git diff --check`. Builds, UI smoke tests, and full suites are unnecessary unless the edit affects executable behavior or a relevant acceptance criterion requires them. Read-only explanations and diagnoses do not require builds or documentation changes merely to finish a response.

Update relevant `docs/ai/` files when the work changes a documented contract, workflow, architecture, task status, or known limitation. Do not create unrelated documentation churn for every task.

Report changed files and behavior, preserved behavior, validation results (including build/test status or why not run), and unresolved risks or assumptions. Include documentation updates when made. Scale the report to the task; the headings in `09-ai-working-rules.md` are a checklist, not a mandatory template. Recommend a next task only when useful, and do not start it without instruction.
