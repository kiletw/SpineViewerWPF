# Runtime Compatibility Matrix

## Status

Inventory verified from v2 commit `79c6135`. Project-authored JSON fixtures verify every connected line from 2.1.08 through 4.2. TASK-009 verifies official 4.1 JSON/binary, TASK-026 verifies official 3.8.55 JSON/binary/PMA, and TASK-041 pins official 4.2 commit `b81e5a58ed38704aee4f866f0e0ac672623ce914`, verifies its 4.2.22 JSON/binary example export, and exercises deterministic Physics replay. The shared Runtime-neutral CPU/GPU-scene bridges retain the clipping, blend, Track Alpha, slot, multi-page, and real-asset coverage recorded by TASK-027 through TASK-040. Official 4.2 PMA and multi-page export parity remain unverified.

Each snapshot is isolated in a version-specific assembly under `runtimes/`. Historical sources retain their namespace/patch provenance uncertainty, but TASK-047 removes the unused XNA helpers and verifies that all 484 active historical `.cs` inputs are byte-identical after relocation. The 4.1 and 4.2 projects use clean official sources with license headers. The 4.2 `spine-csharp/src` tree was compared file-for-file with its pinned commit with zero mismatches.

| v2 selection | Vendored directory | Official tag candidate | JSON path | Binary path | PMA path | Multi-page | Fixture | v3 priority |
|---|---|---|---:|---:|---:|---:|---|---|
| 2.1.08 | `runtimes/SpineRuntime.V21_08/src` | `2.1.08` / `39ce4b2` | Yes | No | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 2.1.25 | `runtimes/SpineRuntime.V21_25/src` | `2.1.25` / `142e770` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.1.07 | `runtimes/SpineRuntime.V31_07/src` | `3.1.07` / `e74b61e` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.2.xx | `runtimes/SpineRuntime.V32/src` | TBD; no exact tag identified | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.4.02 | `runtimes/SpineRuntime.V34_02/src` | `3.4.02` / `ef50131` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.5.51 | `runtimes/SpineRuntime.V35_51/src` | `3.5.51` / `2cd9467` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.6.32 | `runtimes/SpineRuntime.V36_32/src` | `3.6.32` / `283f63b` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.6.39 | `runtimes/SpineRuntime.V36_39/src` | `3.6.39` / `43f37ce` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.6.53 | `runtimes/SpineRuntime.V36_53/src` | `3.6.53` / `a4a36d8` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.7.94 | `runtimes/SpineRuntime.V37_94/src` | `3.7.94` / `45b8125` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 3.8.95 | `runtimes/SpineRuntime.V38_95/src` | `3.8.95` / `3e93e2d` | Yes | Yes | Present | TBD | Official 3.8.55 cache plus project-authored fixture | Official cache verified (JSON/Binary/PMA) |
| 4.0.31 | `runtimes/SpineRuntime.V40_31/src` | `4.0.31` / `8770e31` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 4.0.64 | `runtimes/SpineRuntime.V40/src` | `4.0.64` / `01524d4` | Yes | Yes | Present | TBD | Project-authored JSON fixture | Prototype complete (JSON) |
| 4.1.00 | `runtimes/SpineRuntime.V41/src` | `4.1.00` / `ab28b77` | Yes | Yes | Present | Project-authored two-page smoke; official TBD | Synthetic and two-page fixtures plus official `spineboy` JSON/binary export smoke | Prototype complete |
| 4.2 | `runtimes/SpineRuntime.V42/src` | `4.2` branch / `b81e5a5` | Yes | Yes | Present | TBD | Project Physics fixture plus official 4.2.22 JSON/binary cache | Prototype complete |
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
- Verified multi-page atlas path: TASK-028 loads two project-authored PPM pages and renders attachments from both pages deterministically
- Verified atlas discovery: TASK-011 resolves the official `-pro` skeletons to their base-stem atlas when the caller omits an explicit atlas path
- Verified bounds-aware fit: TASK-010 keeps the 64 by 64 synthetic baseline unchanged and records deterministic 512 by 512 official JSON/binary renders with the full `spineboy` pose visible
- Verified WPF playback: TASK-012 advances a real asset on a dispatcher tick and updates the timeline label; TASK-031 keeps one loaded Application render session per scene layer; TASK-033 returns bounded in-memory BGRA fallback frames; TASK-036 returns Runtime-neutral clipped triangle scenes for normal GPU playback and validates the user-supplied 4.1.14 `xiu.skel` through a non-empty OpenGL framebuffer
- Verified padded texture coordinates: TASK-033 preserves a non-zero atlas page size instead of replacing it with decoded PNG dimensions; Application smoke covers a deliberately mismatched atlas/texture size and the user-supplied 4.1.14 asset renders without global UV displacement
- Verified Track 0 Alpha: TASK-032 proves that changing the Runtime-neutral track weight changes the rendered 4.1 fixture while the default value `1` retains the recorded baseline
- Verified metadata: export `4.1.00`, animation `move` at 1 second, skin `default`
- WPF metadata path: TASK-005 verified the same fixture through the native file-open composition, Application inspect use case, isolated adapter, and presentation mapping
- WPF static preview: TASK-007 reuses the Application render use case off the UI thread; TASK-008 verifies the same Ready path for a generated PNG atlas texture
- Verified render: 64 by 64 PNG at 0.5 seconds, SHA-256 `7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E`
- Verified GPU PMA composition: TASK-042 reproduces a user-supplied 3.5.51 export with one Screen slot, converts straight-alpha texture input to premultiplied shader output, and uses `(One, OneMinusSrcColor)` for Screen RGB with independent source-over alpha factors
- Still unverified: interlaced or non-8-bit PNG, official PMA fixture parity, GPU-equation parity beyond the verified Screen source factor, official multi-page exports, and other production texture formats

## Rules

- Do not infer compatibility solely from successful compilation or parser presence.
- Do not assume a newer Runtime loads older exports.
- Do not copy these namespace-renamed sources into v3 as provenance-proof snapshots.
- Pin a clean official upstream commit and record any adapter patch before adding a Runtime to v3.
- Patch-level consolidation requires JSON and binary fixture evidence where both formats exist.

## v3 4.2 Compatibility Slice

- Official source: 4.2 branch snapshot `b81e5a58ed38704aee4f866f0e0ac672623ce914`; the vendored `spine-csharp/src` files have zero content mismatches and retain the official 2025 Runtime license.
- Isolation: `SpineRuntime.V42` exposes only the existing Application adapter contracts; WPF and CLI register the adapter without receiving Runtime-specific types.
- Official cache: `scripts/test-v42.ps1 -Offline` verifies the 4.2.22 `spineboy` JSON and binary exports, 11 animations, `default` skin, one atlas texture, explicit and automatic selection, and deterministic 512 by 512 renders.
- Project fixture: `tests/fixtures/v42-minimal` verifies 4.2 Physics, deterministic 64 by 64 CPU output, visible Runtime-neutral textured triangles, and same-session A-B-A geometry equality.
- Physics sampling: assets without Physics remain O(1); Physics assets reset at time zero and replay at 60 Hz up to 600 steps. Times over 10 seconds use 600 evenly distributed steps to keep interactive work bounded.
- Still unverified: official 4.2 PMA, clipping/blend feature-isolated parity, multi-page exports, long-timeline Physics fidelity beyond the bounded replay ceiling, and production formats outside the existing PNG/PPM boundary.

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
- Known boundary: the shared CPU bridge now consumes clipping attachments and maps normal/additive/multiply/screen slots; this remains a bounded deterministic path, not a full visual-parity claim.

## v3 Historical Compatibility Slice

- Isolated adapters now cover every listed line from 2.1.08 through 4.2.
- Each historical line has a project-authored JSON/atlas/P3 fixture, explicit and automatic selection smoke coverage, and a deterministic 64 by 64 render hash.
- TASK-032 compiles the Track 0 weight against every historical adapter; lines through 3.4 use the matching legacy `Mix` property and 3.5 onward use `Alpha`.
- TASK-031 additionally desktop-verifies the user-supplied 3.6.53 binary export, five-page atlas, first-animation selection, 512 by 512 preview, and active playback controls.
- Binary `.skel`, official editor-export parity, official PMA fixture parity, feature-isolated clipping/non-normal blend fixtures, multi-page atlases, and production texture formats remain unverified unless listed in the 4.2, 4.1, or 3.8 official sections. The 3.8 cache verifies the PMA path; TASK-027 provides the bounded deterministic clipping/blend bridge.
- 4.3 remains unsupported because it has no connected pinned adapter or verified fixture.
