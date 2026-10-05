# Git Strategy

## Long-Lived Lines

```text
master      v3 integration and release line (pull requests merge here; TASK-069)
legacy/v2   v2 maintenance
v3          optional v3 integration branch; CI also runs on pushes to it
```

The v2 reference remains available through the `2.x.0.0` tags.

Before creating branches, inspect existing remote branches and tags; do not assume they already exist.

## Initial Setup

1. verify current stable v2 commit
2. tag the verified release, for example `v2.4.0`
3. create `legacy/v2` from that commit
4. develop v3 on `master` (the former plan of a separate `v3`/`main` line was superseded by TASK-069)
5. protect `master` (require the CI check) and `legacy/v2`

## Task Branches

- `docs/v3-baseline`
- `test/v3-compatibility-fixtures`
- `feat/v3-runtime-41-slice`
- `feat/v3-cli-inspect`
- `feat/v3-ui-shell`
- `fix/v2-*` from `legacy/v2`

## Merge Targets

- v2 hotfix → `legacy/v2`
- v3 feature/refactor → `master` through a pull request that passes CI
- release → SemVer tag on a `master` commit

## Release Tags

```text
v3.0.0-alpha.1
v3.0.0-beta.1
v3.0.0-rc.1
v3.0.0
```

Pushing a tag matching `vMAJOR.MINOR.PATCH[-PRERELEASE]` runs
`.github/workflows/release.yml`: it rejects tags whose commit is not on `master`,
reruns the build and smoke checks, and publishes self-contained `win-x64` WPF and
CLI zips with `SHA256SUMS.txt`. Suffixed tags become prereleases. A manual run of
the workflow is a dry run that uploads the packages as workflow artifacts only.

Do not use a complicated GitFlow unless concurrent release stabilization creates a real need.
