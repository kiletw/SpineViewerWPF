# Runtime Compatibility Matrix

## Status

Inventory verified from v2 commit `79c6135`. Parser/renderer presence is code-verified. A project-authored 4.1 JSON fixture now verifies the v3 adapter and deterministic CPU spike; editor-export compatibility, PMA output, binary input, and multi-page atlas behavior remain unverified.

All snapshots are compiled into the WPF project under version-specific namespaces. License headers identify Esoteric Software Runtime source. Same-name official tags and their current Git object IDs were verified with `git ls-remote` on 2026-07-26, but file-for-file equality has not been proven after local namespace/XNA changes.

| v2 selection | Vendored directory | Official tag candidate | JSON path | Binary path | PMA path | Multi-page | Fixture | v3 priority |
|---|---|---|---:|---:|---:|---:|---|---|
| 2.1.08 | `spine-runtimes-2.1.08` | `2.1.08` / `39ce4b2` | Yes | No | Present | TBD | Missing | Later |
| 2.1.25 | `spine-runtimes-2.1.25` | `2.1.25` / `142e770` | Yes | Yes | Present | TBD | Missing | Later |
| 3.1.07 | `spine-runtimes-3.1.07` | `3.1.07` / `e74b61e` | Yes | Yes | Present | TBD | Missing | Later |
| 3.2.xx | `spine-runtimes-3.2.xx` | TBD; no exact tag identified | Yes | Yes | Present | TBD | Missing | Later |
| 3.4.02 | `spine-runtimes-3.4.02` | `3.4.02` / `ef50131` | Yes | Yes | Present | TBD | Missing | Later |
| 3.5.51 | `spine-runtimes-3.5.51` | `3.5.51` / `2cd9467` | Yes | Yes | Present | TBD | Missing | Later |
| 3.6.32 | `spine-runtimes-3.6.32` | `3.6.32` / `283f63b` | Yes | Yes | Present | TBD | Missing | Later |
| 3.6.39 | `spine-runtimes-3.6.39` | `3.6.39` / `43f37ce` | Yes | Yes | Present | TBD | Missing | Later |
| 3.6.53 | `spine-runtimes-3.6.53` | `3.6.53` / `a4a36d8` | Yes | Yes | Present | TBD | Missing | Later |
| 3.7.94 | `spine-runtimes-3.7.94` | `3.7.94` / `45b8125` | Yes | Yes | Present | TBD | Missing | Later |
| 3.8.95 | `spine-runtimes-3.8.95` | `3.8.95` / `3e93e2d` | Yes | Yes | Present | TBD | Missing | Second |
| 4.0.31 | `spine-runtimes-4.0.31` | `4.0.31` / `8770e31` | Yes | Yes | Present | TBD | Missing | Second |
| 4.0.64 | `spine-runtimes-4.0.64` | `4.0.64` / `01524d4` | Yes | Yes | Present | TBD | Missing | Second |
| 4.1.00 | `spine-runtimes-4.1.00` | `4.1.00` / `ab28b77` | Yes | Yes | Present | TBD | Synthetic JSON verified in v3; editor export TBD | Prototype complete |
| 4.2 | none | none | No | No | No | No | Missing | Future |
| 4.3 | none | current upstream line | No | No | No | No | Missing | Future |

“Present” means the Player assigns the UI PMA/alpha flag to its Runtime renderer. It does not mean visual correctness is fixture-verified.

## Local Introduction History

| Snapshot | First repository commit containing directory |
|---|---|
| 2.1.08, 2.1.25, 3.1.07 | `10d36ca` (2018-01-08) |
| 3.4.02, 3.5.51, 3.6.32, 3.6.39 | `dd8fd32` (2018-01-02) |
| 3.6.53 | `985097c` (2019-01-19) |
| 3.8.95 | `30486a6` (2020-05-18) |
| 3.7.94 | `4c09602` (2020-08-21) |
| 3.2.xx | `1f77494` (2021-01-08) |
| 4.0.31 | `cece904` (2021-09-17) |
| 4.0.64, 4.1.00 | `79c6135` (2022-08-29) |

## Runtime Metadata Baseline

For every vendored line:

```json
{
  "repository": "https://github.com/EsotericSoftware/spine-runtimes",
  "branchOrTag": "same-name tag candidate shown above, or TBD",
  "commit": "tag object shown above; exact local source equivalence TBD",
  "supportedExportLines": ["selected v2 label only; fixture verification pending"],
  "localPatches": ["namespace renamed to a version-specific SpineX_Y_Z namespace", "other differences TBD"]
}
```

Fixture status and expected non-redistributable locations are tracked in [`fixtures/manifest.json`](fixtures/manifest.json).

## v3 4.1 Vertical Slice

- Official source: tag `4.1.00`, commit `ab28b77c70e3aa766be5bdb759d7aedac9fd0bde`
- Source patches: none
- Verified input: project-authored JSON, atlas, and P3 PPM texture under `tests/fixtures/v41-minimal`
- Verified metadata: export `4.1.00`, animation `move` at 1 second, skin `default`
- WPF metadata path: TASK-005 verified the same fixture through the native file-open composition, Application inspect use case, isolated adapter, and presentation mapping
- Verified render: 64 by 64 PNG at 0.5 seconds, SHA-256 `7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E`
- Still unverified: real editor exports, binary input, PMA, clipping, non-normal blend modes, multi-page atlases, and production texture formats

## Rules

- Do not infer compatibility solely from successful compilation or parser presence.
- Do not assume a newer Runtime loads older exports.
- Do not copy these namespace-renamed sources into v3 as provenance-proof snapshots.
- Pin a clean official upstream commit and record any adapter patch before adding a Runtime to v3.
- Patch-level consolidation requires JSON and binary fixture evidence where both formats exist.
