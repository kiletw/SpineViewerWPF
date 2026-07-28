# Runtime Compatibility Matrix

## Status

Inventory verified from v2 commit `79c6135`. Parser/renderer presence is code-verified. Project-authored JSON fixtures for every vendored line from 2.1.08 through 4.1 verify the isolated v3 adapters, deterministic CPU spike, and WPF static-preview integration. Test-generated PNG variants verify bounded 8-bit non-interlaced texture decoding and static rendering. TASK-009 verifies the official 4.1 `spineboy` JSON and binary example export, and TASK-026 verifies the official 3.8.55 `spineboy` JSON/binary/PMA assets through the 3.8.95 adapter. Official multi-page atlas behavior remains unverified.

All snapshots are compiled into the WPF project under version-specific namespaces. License headers identify Esoteric Software Runtime source. Same-name official tags and their current Git object IDs were verified with `git ls-remote` on 2026-07-26, but file-for-file equality has not been proven after local namespace/XNA changes.

| v2 selection | Vendored directory | Official tag candidate | JSON path | Binary path | PMA path | Multi-page | Fixture | v3 priority |
|---|---|---|---:|---:|---:|---:|---|---|
| 2.1.08 | `spine-runtimes-2.1.08` | `2.1.08` / `39ce4b2` | Yes | No | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 2.1.25 | `spine-runtimes-2.1.25` | `2.1.25` / `142e770` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.1.07 | `spine-runtimes-3.1.07` | `3.1.07` / `e74b61e` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.2.xx | `spine-runtimes-3.2.xx` | TBD; no exact tag identified | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.4.02 | `spine-runtimes-3.4.02` | `3.4.02` / `ef50131` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.5.51 | `spine-runtimes-3.5.51` | `3.5.51` / `2cd9467` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.6.32 | `spine-runtimes-3.6.32` | `3.6.32` / `283f63b` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.6.39 | `spine-runtimes-3.6.39` | `3.6.39` / `43f37ce` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.6.53 | `spine-runtimes-3.6.53` | `3.6.53` / `a4a36d8` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.7.94 | `spine-runtimes-3.7.94` | `3.7.94` / `45b8125` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.8.95 | `spine-runtimes-3.8.95` | `3.8.95` / `3e93e2d` | Yes | Yes | Present | TBD | Official 3.8.55 cache plus project-authored fixture | Official cache verified (JSON/Binary/PMA) |
| 4.0.31 | `spine-runtimes-4.0.31` | `4.0.31` / `8770e31` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 4.0.64 | `spine-runtimes-4.0.64` | `4.0.64` / `01524d4` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 4.1.00 | `spine-runtimes-4.1.00` | `4.1.00` / `ab28b77` | Yes | Yes | Present | TBD | Synthetic fixture plus official `spineboy` JSON/binary export smoke | Prototype complete |
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
- Verified PNG metadata: TASK-006 uses a test-generated PNG page with the same project-authored skeleton and atlas data
- Verified PNG texture decoding: TASK-008 covers 8-bit non-interlaced grayscale, RGB, indexed with palette transparency, grayscale-alpha, and RGBA data, scanline filters 0 through 4, CRC validation, and deterministic static rendering
- Verified official example compatibility: TASK-009 loads `examples/spineboy/export/spineboy-pro.json` and `.skel` with `spineboy.atlas` and `spineboy.png` from pinned commit `ab28b77c70e3aa766be5bdb759d7aedac9fd0bde`; both formats report 11 animations and render the `walk` animation
- Verified atlas discovery: TASK-011 resolves the official `-pro` skeletons to their base-stem atlas when the caller omits an explicit atlas path
- Verified bounds-aware fit: TASK-010 keeps the 64 by 64 synthetic baseline unchanged and records deterministic 512 by 512 official JSON/binary renders with the full `spineboy` pose visible
- Verified WPF playback: TASK-012 advances a real asset on a dispatcher tick, updates the timeline label, and re-renders through the existing Application path
- Verified metadata: export `4.1.00`, animation `move` at 1 second, skin `default`
- WPF metadata path: TASK-005 verified the same fixture through the native file-open composition, Application inspect use case, isolated adapter, and presentation mapping
- WPF static preview: TASK-007 reuses the Application render use case off the UI thread; TASK-008 verifies the same Ready path for a generated PNG atlas texture
- Verified render: 64 by 64 PNG at 0.5 seconds, SHA-256 `7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E`
- Still unverified: interlaced or non-8-bit PNG, official PMA fixture parity, clipping, non-normal blend modes, multi-page atlases, and other production texture formats

## Rules

- Do not infer compatibility solely from successful compilation or parser presence.
- Do not assume a newer Runtime loads older exports.
- Do not copy these namespace-renamed sources into v3 as provenance-proof snapshots.
- Pin a clean official upstream commit and record any adapter patch before adding a Runtime to v3.
- Patch-level consolidation requires JSON and binary fixture evidence where both formats exist.

## v3 4.0.64 Compatibility Slice

- Official source candidate: legacy vendored snapshot `4.0.64`, commit candidate `01524d4`
- Source patches: no vendored source edits; project-owned `System.Text.Json` decoder bridge and shared deterministic CPU renderer bridge
- Verified input: project-authored JSON, atlas, and P3 PPM texture under `tests/fixtures/v40-minimal`
- Verified behavior: auto-selection, explicit `4.0`/`4.0.64` selection, metadata inspect, deterministic 64 by 64 PNG render, and Application smoke coverage
- Still unverified: official 4.0.64 Editor exports, binary `.skel`, official PMA fixture parity, clipping, non-normal blend modes, multi-page atlases, and production texture formats

## v3 3.8.95 Compatibility Slice

- Official cache: spine-runtimes 3.8 commit `8b4844bd4b193ba9e54487ed397a777993cbad56`.
- Verified export: `3.8.55` JSON and binary spineboy example accepted by the 3.8.95 adapter.
- Verified assets: normal atlas/PNG, PMA atlas/PNG, and the accompanying license file through `scripts/test-official-v38.ps1 -Offline`.
- Verified behavior: explicit and automatic selection, 11 animations, `default` skin, deterministic 512 by 512 JSON/binary/PMA renders.
- Known boundary: clipping and additive slots are present in the example but remain outside the bounded CPU spike's full visual-parity claim.

## v3 Historical Compatibility Slice

- Isolated adapters now cover every vendored 2.1.08 through 4.0.31 line listed above, plus the existing 4.0.64 and 4.1.00 adapters.
- Each historical line has a project-authored JSON/atlas/P3 fixture, explicit and automatic selection smoke coverage, and a deterministic 64 by 64 render hash.
- Binary `.skel`, official editor-export parity, official PMA fixture parity, clipping, non-normal blend modes, multi-page atlases, and production texture formats remain unverified unless listed in the 4.1 or 3.8 official sections. The 3.8 cache verifies the PMA load/render path, but the CPU bridge remains a bounded deterministic spike for clipping and additive content.
- 4.2 and 4.3 remain unsupported because this repository contains no vendored source snapshot for either line.
