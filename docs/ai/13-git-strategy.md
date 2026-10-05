# Git Strategy

## Long-Lived Lines

```text
master      v2 reference until repository migration is decided
legacy/v2   v2 maintenance
v3          v3 integration during development
main        optional future default after v3 release
```

Before creating branches, inspect existing remote branches and tags; do not assume they already exist.

## Initial Setup

1. verify current stable v2 commit
2. tag the verified release, for example `v2.4.0`
3. create `legacy/v2` from that commit
4. create `v3` from the chosen base commit
5. protect `legacy/v2`, `v3`, and later `main`

## Task Branches

- `docs/v3-baseline`
- `test/v3-compatibility-fixtures`
- `feat/v3-runtime-41-slice`
- `feat/v3-cli-inspect`
- `feat/v3-ui-shell`
- `fix/v2-*` from `legacy/v2`

## Merge Targets

- v2 hotfix → `legacy/v2`
- v3 feature/refactor → `v3`
- release candidate → future default branch

## Release Tags

```text
v3.0.0-alpha.1
v3.0.0-beta.1
v3.0.0-rc.1
v3.0.0
```

Do not use a complicated GitFlow unless concurrent release stabilization creates a real need.
