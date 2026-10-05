# TASK-051 official Spine 4.3 cache evidence

- Upstream: `EsotericSoftware/spine-runtimes`
- Commit: `de14116488688c27c01b6e2b61fe1544792af2dd`
- Source directory: `spine-csharp/src`
- Official C# package version: `4.3.39`
- Example directory: `examples/spineboy/export`
- Export metadata: Spine `4.3.75-beta`, 11 animations, skin `default`
- Cache: gitignored `artifacts/official-v43/spineboy`
- Validation: `scripts/test-v43.ps1`; pass `-Offline` to prohibit downloads

The upstream `license.txt` permits redistribution of the images when the
license accompanies them and prohibits commercial image use. The repository's
existing fixture policy is stricter: no official binary or image asset is
committed. The script downloads the five required files from the pinned commit
and validates both their Git blob SHA-1 and byte SHA-256.

| File | Git blob SHA-1 | SHA-256 |
|---|---|---|
| `spineboy-pro.json` | `4a58fecd32a97bad83a9169056c44db3f17dba4e` | `24CCFFC13E334E721DFD427EE2B8AEA05C25B59167B5FB0BB0F9685E11D2A7D3` |
| `spineboy-pro.skel` | `cfcbd39497d367d25ba82b6e535a61933b3cba98` | `E10DE3F2473A37139C3AD1FDA14E84B29EF401475EC9FA2129E7D2A311088845` |
| `spineboy.atlas` | `33f0db102bfd643ed3f3c021b6821ac2a775ad03` | `FBD452640DF513D637E0A34D21C1D3EA4EDB27F57BD1856FFD5530B16EE83D00` |
| `spineboy.png` | `6f76c57988f1b0575d5b12ed7b2d4f44170cb5bd` | `AB874448A224F8A92188E133735062C86E5F93EB7286B0FC37C5D39864E6569F` |
| `license.txt` | `c2bbb8670bfbfff48ea0d3b0a921b0ac698c79e3` | `08074C5F8C5F072626A0830250304FE3063D9D49F8F9B039E063BB238F2B993B` |

Offline validation on 2026-08-27 passed explicit and automatic JSON/binary
inspection and rejected explicit 4.2 selection for both 4.3 exports. Both
formats reported export `4.3.75-beta`, Runtime `4.3`, these 11 animations:
`aim`, `death`, `hoverboard`, `idle`, `idle-turn`, `jump`, `portal`, `run`,
`run-to-idle`, `shoot`, and `walk`; skin `default`; and one texture. Two
consecutive 512 by 512 renders at 0.5 seconds produced identical bytes for
each format. The `portal` animation activates the official clipping attachment.

| Animation | Format | Render SHA-256 |
|---|---|---|
| `walk` | JSON | `9E1A72725C44FFF30A4BD955E72E12331F1071C03F2ED3FFF1F1DBCF5802DF12` |
| `walk` | Binary | `C093E2754ABF375CE84EEA8DCE8E9693585D57F1C65B3003CE76265637824BA5` |
| `portal` (clipping) | JSON | `1087F3FAE199833885B36B59AE7F083AF66883D97D5ECCCAABE94C768F16F0BB` |
| `portal` (clipping) | Binary | `E03D7A4C68FD57C11AB50DFB1FAA9C7055CB13A3AF23CD3AD494636DE7F70042` |

The pinned upstream `spine-csharp/src` tree has Git tree
`718c21c0ef26840d14273133120134c85f0cb553`, 139 paths, no missing paths,
and no extras in `SpineRuntime.V43/src`. Source text matches upstream with no
project-authored code patch. Git checkout normalization may represent
`package.json` and `spine-csharp.asmdef` with CRLF locally while retaining the
same text. Official Unity `.meta` files also contain trailing spaces; they are
preserved and verified by Git blob identity, while whitespace checks exclude
the vendored source directory. The project-owned adapter and CPU/GPU-scene
bridges live outside that directory.
