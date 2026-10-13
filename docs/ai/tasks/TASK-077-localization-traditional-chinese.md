# TASK-077: Localization and Traditional Chinese UI

Status: Implemented on 2026-10-14.

## Objective

All user-visible desktop text is resource-based, the UI is available in English
and Traditional Chinese (Taiwan), and the language is chosen in the app and
applied at the next start.

## Context

- Product-owner request (2026-10-14): localize before further features, so new
  text is written translatable from the start.
- Before this task, XAML labels already used `Text.*` resources in App.xaml, but
  about 80 status, state, and summary strings were hard-coded in C#, 4 strings in
  XAML, and several option lists showed their stored values (background mode,
  export size mode, format, scope, range) directly.
- Runtime (live) language switching stays deferred; a restart is acceptable.

## Allowed Paths

- `src/SpineViewerWPF.Wpf/**` (resources, App startup, Shell/browser text,
  option display, language menu, settings)
- `scripts/test-ui-shell.ps1` (`--language=en` only)
- `tests/SpineViewerWPF.Application.Smoke/**`
- `docs/ai/00-context-index.md`, `docs/ai/16-ui-state-and-interaction-contracts.md`,
  `docs/ai/17-external-feature-reference.md`, `README.md`, `README_zhTW.md`, this task

## Forbidden Paths

- Core, Application, `runtimes/**` (exception and diagnostic messages produced
  there stay English)
- stored values: sidecar fields and settings keep their English identifiers
- new packages

## Required Behavior

- `Localization/Strings.en.xaml` holds the English XAML text and is merged by
  App.xaml; `Localization/Strings.zh-TW.xaml` is merged after it when Traditional
  Chinese is active, so missing keys fall back to English.
- Code text uses `Localized.Text/Format(key, english, ...)`: the English text is
  at the call site, other languages override the key. Without a running
  application (tests) the English text is used.
- Option values stay stable (they are stored in projects and settings);
  `OptionTextConverter` displays them through `Option.<value without spaces>`.
- Workspace state text uses `State.<State>.Title/Detail`; count messages use
  singular/plural keys.
- View > Language: System default / English / 繁體中文, remembered in user
  settings and applied at the next start. System default follows the Windows
  display language (any Chinese → Traditional Chinese, others → English).
  `--language=` overrides it for tests; `test-ui-shell.ps1` uses English.
- The UI culture is set to the active language.

## Acceptance Criteria

- Every English XAML key and every code-side key has a Traditional Chinese entry
  with the same placeholders; every state has title and detail text; no unused
  Chinese keys (smoke test).
- Language resolution and the remembered setting behave as documented.
- The built application shows the Traditional Chinese UI with `--language=zh-TW`.
- Existing English output and tests are unchanged.

## Validation

```text
dotnet build SpineViewerWPF.sln -c Release
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

## Completion Report

- Changed: language dictionaries (201 XAML keys moved from App.xaml; 317
  Traditional Chinese entries), `Localized`/`UiLanguage`/`OptionTextConverter`,
  Shell and browser text through keys, option combo templates, View > Language,
  `UserSettingsStore.Language`, `--language=`, `LocalizationSmoke`.
- Preserved: English text and stored values; test expectations.
- Validation (2026-10-14): all commands passed (277 keys checked); a live
  capture with `--language=zh-TW` showed translated menus, panels, export
  settings, option lists, and status bar.
- Gaps: exception and diagnostic messages from Application and Runtime adapters
  stay English; Runtime and Spine terms (skeleton, atlas, Runtime, PMA) are kept
  in English inside Chinese text; changing the language needs a restart.
- Next: adding a language is a new `Strings.<culture>.xaml` plus a
  `UiLanguage` entry and menu item; the smoke test checks coverage.
