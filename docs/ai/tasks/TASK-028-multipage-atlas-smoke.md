# TASK-028: Multi-page Atlas Smoke

## Status

Completed 2026-07-28.

## Objective

Verify the existing Runtime atlas path with one project-authored 4.1 asset
whose attachments are split across two atlas pages.

## Scope

- add a redistributable text fixture with two P3 texture pages;
- verify metadata exposes both page paths;
- render both page-backed attachments and record a deterministic PNG hash;
- keep the shared Runtime source and Core/Application contracts unchanged.

## Completion report

### Changed

- Added `tests/fixtures/v41-multipage` with a two-page atlas, two PPM pages,
  and a minimal 4.1 JSON skeleton.
- Extended Application smoke to assert both texture paths, deterministic
  rendering, and the recorded 64 by 64 baseline.
- Registered the fixture and hashes in `docs/ai/fixtures/manifest.json`.

### Preserved

- existing 4.1, historical, official cache, PMA, clipping/blend, and WPF
  shell baselines;
- no vendored Runtime edits and no binary assets.

### Validation

- CLI inspect reports two texture pages;
- CLI render produces deterministic output with SHA-256
  `89115E7CC5AA6B1B594C14C9E1F7F74B5C30644F926B1D032420DC7B753ED6AA`;
- Application smoke remains green.

### Known gaps

- This verifies the project-owned 4.1 path only; official multi-page exports
  and page-level visual parity remain unverified.
