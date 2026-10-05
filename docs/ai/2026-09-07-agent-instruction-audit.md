# Repository instruction audit — 2026-09-07

Scope: user-authorized audit and optimization of repository AGENTS.md. This is instruction maintenance, not continuation of TASK-058. Root AGENTS.md is the workflow entry point; its conditional reading, task-record, and completion rules qualify the older unconditional wording in the supporting documents.

## Inspected inheritance

- `CODEX_HOME` was unset; `C:/Users/kiletw/.codex/AGENTS.md` exists and is empty (0 bytes). Its `AGENTS.override.md` is absent.
- `F:/AGENTS.md`, `F:/AGENTS.override.md`, `F:/開發/AGENTS.md`, and `F:/開發/AGENTS.override.md` are absent.
- Repository `AGENTS.md` exists; repository `AGENTS.override.md` is absent. No AGENTS.md or AGENTS.override.md was found below `docs/`.
- Session instructions and tool permissions still apply independently; this audit does not change them. No configuration, credentials, session history, or unrelated cache contents were read.

## Evidence and resolutions

Line references to supporting documents below refer to their unchanged working-tree contents at audit time.

- Original root AGENTS.md lines 7–13 prescribed five reading steps; `00-context-index.md:22–35` prescribed ten. Root AGENTS.md now defines one conditional reading entry point: retain core constraints and working rules, use the index to select task-relevant context, and use broader reading for broad implementation/architecture work.
- `09-ai-working-rules.md:5` said "Every change must reference one TASK-xxx.md". Root AGENTS.md now retains records for product/contract/architecture/dependency/compatibility implementation, permits bounded instruction/documentation maintenance without a numbered task, and permits creating necessary records within an already authorized implementation.
- `09-ai-working-rules.md:9–10` said "confirm" repository, branch, and working tree. Clarified as local verification, not repeated user approval. Added explicit separation of diagnosis from authorized repair and preservation of existing changes.
- Original root AGENTS.md lines 27–36 imposed completion requirements without task conditions; `09-ai-working-rules.md:31–43` supplied a fixed report including a next task. Added proportionate validation/reporting, conditional documentation updates, and optional recommendations while retaining the prohibition on starting follow-on work without instruction.
- `09-ai-working-rules.md:26–29` preserves legacy/v2 and v3 bases, protected branches, and suggested names. Root AGENTS.md retains the base/protection rules, allows continuation on a suitable existing task branch, and defers naming to explicit user/session preferences.
- `tasks/TASK-058-photoshop-workspace-refinement.md:3` marks that UI task completed. Its UI build/smoke commands do not apply to this separate documentation request. None of its existing changes were included in this audit's edits.
- Original root AGENTS.md lines 17–23, `05-technical-constraints.md`, and Accepted `decisions/ADR-001-v3-progressive-rewrite.md` justify retaining Runtime isolation, dependency direction, branch protection, narrow scope, and verified behavior. No technical constraint or existing validation command was removed.
- No inspected repository/ancestor rule required multiple models or sub-agents. The new wording prevents making them a default; it does not remove any observed model routing or language preference.

## External Skill recommendation (not applied)

Inspected only the explicitly relevant `C:/Users/kiletw/.codex/skills/.system/openai-docs/SKILL.md`. Lines 12–14 require official web research before local inspection, with only a broad setup exception. That overextends a documentation lookup workflow to a user-directed local instruction audit and conflicts with this request's explicit local-first order. Keep official-source requirements for actual product claims and add the following exception before "First substantive action":

```diff
--- C:/Users/kiletw/.codex/skills/.system/openai-docs/SKILL.md
+++ C:/Users/kiletw/.codex/skills/.system/openai-docs/SKILL.md (proposed only)
@@
 Provide current, cited OpenAI product, API, model, and Codex guidance. Read zero or one primary reference.
+
+For a user-requested audit or edit of local instruction/configuration files, inspect the explicitly scoped local sources first. Do not require web research merely because those files govern Codex. Apply the official-source workflow below when the task needs external product facts or behavior that local evidence does not establish. Explicit user source-order instructions take precedence.
```

No global or ancestor rule edits are proposed: the global file is empty and parent files are absent. Whether to apply this Skill exception outside the repository remains the user's decision; the Skill was not modified.

## Validation and preservation

- Reviewed root AGENTS.md diff for retained unique restrictions and conditional workflow consistency; verified referenced documents/directories exist.
- `git diff --check` passed. Builds and executable tests were not run because this task changes only instructions/documentation.
- The five pre-existing modified/untracked files were checked by SHA-256 before and after editing; all remained identical. Branch remained `codex/ui-workspace-refinement`; no commit or publication was performed.
- Supporting index and working-rules files were left intact, including the user's index edits. Their old workflow wording must be read under root AGENTS.md's explicit conditions; standalone consumers of those documents should also load root AGENTS.md.
- This audit validates text and scope, not measured future model behavior. It does not assert that model capability makes any previous constraint obsolete.

## Follow-up: user adopted the Skill recommendation

After the initial audit, the user explicitly approved applying the external Skill proposal. The local OpenAI Docs SKILL.md now contains the proposed local-audit exception. The existing "Only exception" label was changed to "Additional exception" so it does not contradict the new exception. The earlier "not applied" statements describe the initial audit, before this approval.

Verified that these are the only changes to that Skill's text. Official-source requirements and all other instructions were retained. The skill-creator quick_validate.py check passed using `python -X utf8`; the first run failed because Windows defaulted to cp950 while reading the existing UTF-8 content. No validator or runtime configuration was changed.
