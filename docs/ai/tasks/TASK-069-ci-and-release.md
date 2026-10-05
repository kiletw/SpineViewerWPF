# TASK-069: CI, release automation, and repository hygiene

Status: Implemented locally on 2026-10-06; GitHub-hosted runs pending (see Gaps).

## Objective

Every pull request and every push to `master`/`v3` is built and smoke-tested on
GitHub Actions, and pushing a `v*` SemVer tag publishes a GitHub Release with
portable Windows packages that carry the required license notices.

## Context

- No CI existed; `.github/` held only the pull request template.
- `origin/master` already receives v3 pull requests (PR #54), so `master` is
  the v3 integration and release line; `13-git-strategy.md` is updated to match.
- FFmpeg stays optional and user-installed (ADR-010); packages must not bundle it.
- Spine Runtime licenses require redistribution to include each license and
  copyright notice and require every user to hold a Spine Editor license.
- Local development uses .NET SDK 9.0.3xx to build `net8.0` targets.

## Allowed Paths

- `.github/**`
- `.gitignore`, `global.json`, `src/Directory.Build.props`
- `scripts/publish-release.ps1`
- `THIRD-PARTY-NOTICES.md`, `README.md`, `README_zhTW.md`
- `docs/ai/13-git-strategy.md`, `docs/ai/00-context-index.md`, this task
- branch-base rule only: `AGENTS.md`, `docs/ai/09-ai-working-rules.md`

## Forbidden Paths

- `runtimes/**` (read-only; licenses are copied or extracted at package time)
- application, CLI, Core, and test source behavior
- bundling FFmpeg or any binary into the repository

## Required Behavior

- CI: Windows runner restores, builds the solution in Release, runs
  Application.Smoke and `scripts/test-v3.ps1`, and checks whitespace on pull
  requests. Workflow permissions are read-only.
- Release: a pushed tag `vMAJOR.MINOR.PATCH[-PRERELEASE]` whose commit is on
  `master` builds, tests, and publishes self-contained `win-x64` zips for the
  WPF application and CLI, plus `SHA256SUMS.txt`. Tags with a prerelease suffix
  become GitHub prereleases. Only the publishing job receives `contents: write`.
- Manual `workflow_dispatch` of the release workflow builds packages as a dry
  run and uploads them as workflow artifacts without creating a release.
- Each package contains README files, `THIRD-PARTY-NOTICES.md`, and one Spine
  license text per Runtime adapter (the upstream `LICENSE` file, or the license
  header of that Runtime's `Animation.cs` when no file exists).
- Assemblies default to version `3.0.0-dev`; release builds take the tag version.
- `.gitignore` excludes IDE state, build/publish/test output, signing material,
  secrets, local AI settings, FFmpeg executables, and a `local-assets/` folder
  for assets without redistribution rights.

## Acceptance Criteria

- `scripts/publish-release.ps1 -Version 3.0.0-local.1` produces both zips and
  checksums; the WPF and CLI executables report the requested file version;
  the CLI from the package runs `inspect` on a fixture.
- Solution build, Application.Smoke, and `scripts/test-v3.ps1` still pass.
- Workflow YAML parses; `git diff --check` passes.
- Existing build commands and fixture paths are unchanged.

## Validation

```text
dotnet build SpineViewerWPF.sln -c Release
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/publish-release.ps1 -Version 3.0.0-local.1
git diff --check
```

## Completion Report

### Validation (2026-10-06, Windows, local, SDK 9.0.306)

- `dotnet build SpineViewerWPF.sln -c Release` with `global.json` and
  `src/Directory.Build.props`: 0 warnings, 0 errors.
- Application.Smoke: passed.
- `scripts/test-v3.ps1`: passed; render SHA-256 unchanged
  (`7178BBFA...301E`, as in `04-runtime-matrix.md`).
- `scripts/publish-release.ps1 -Version 3.0.0-local.1`: WPF zip 70.2 MB, CLI zip
  33.7 MB, `SHA256SUMS.txt`; zip entries use `/` separators; both executables
  report FileVersion 3.0.0.0 and ProductVersion `3.0.0-local.1+<commit>`; 16
  Spine license texts per package; the packaged CLI ran `inspect` on
  `tests/fixtures/v41-minimal`; the extracted WPF package started without an
  installed SDK path and showed its main window.
- Workflow and config YAML parse; `git diff --check` passed.

### Gaps and risks

- TBD: workflows have not yet run on GitHub-hosted runners. Application.Smoke
  shows a WPF window; if that is unstable on the runner, set
  `SPINEVIEWER_SKIP_WINDOW_SMOKE=1` for that step.
- `actionlint` was not available locally.
- Online compatibility scripts (`test-v42`/`test-v43`/official caches) and
  `test-ui-shell.ps1` are not in CI yet.
- Packages are unsigned. The repository has no project license file of its own;
  choosing one is the owner's decision.
- Branch protection, secret scanning, and push protection are GitHub settings
  the owner must enable.
