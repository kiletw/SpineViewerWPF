# TASK-041 official Spine 4.2 cache evidence

- Upstream: `EsotericSoftware/spine-runtimes`
- Commit: `b81e5a58ed38704aee4f866f0e0ac672623ce914`
- Source directory: `examples/spineboy`
- Export metadata: Spine `4.2.22`, 11 animations, skin `default`
- Cache: gitignored `artifacts/official-v42/spineboy`
- Validation: `scripts/test-v42.ps1`; pass `-Offline` to prohibit downloads

The upstream `license.txt` permits redistribution of the images when the
license accompanies them and prohibits commercial image use. The repository's
existing fixture policy is stricter: no official binary or image asset is
committed. The script downloads the five required files from the pinned commit
and validates both their Git blob SHA-1 and byte SHA-256.

| File | Git blob SHA-1 | SHA-256 |
|---|---|---|
| `spineboy-pro.json` | `f3ba20944ba6025e59b8a5dc00ad22b6cd58c73f` | `488FACE411DDFAD77EE3239B29431DEB574D73F4FECA7ECA541452AD24BB6BFC` |
| `spineboy-pro.skel` | `09e564b7edc42fe37be66a4a0391862071ed948a` | `1345DE0E8E4729559F39060ED614429B21AB39BFC09ED2E1F58FCF054A8B5925` |
| `spineboy.atlas` | `eca542b711e7e140de85efac07b1bccbda07c5f5` | `FC90C2A604C14BB3AE1A81F9C950660863A41654CB29C869423A86E127FA75AE` |
| `spineboy.png` | `0ea9737f30707915e0bf495cdf987c6511dc0500` | `071A6ADEC73378EEBD802A047D274DEEFE11ABEE3CE689A1E71ACB4CB41E331E` |
| `license.txt` | `c2bbb8670bfbfff48ea0d3b0a921b0ac698c79e3` | `08074C5F8C5F072626A0830250304FE3063D9D49F8F9B039E063BB238F2B993B` |

Offline validation on 2026-08-08 passed explicit and automatic JSON/binary
inspection. Both formats reported export `4.2.22`, Runtime `4.2`, 11 animations,
skin `default`, and one texture. Two consecutive 512 by 512 `walk` renders at
0.5 seconds produced identical bytes for each format and established these
baselines:

| Format | Render SHA-256 |
|---|---|
| JSON | `6065E7919809405F237A29A77871A13402E3EF8120176689D5FA821690AE919A` |
| Binary | `DD3A2C6E92B0B89A999CE907B12C03D2236AA7FC14612A2255EDD1F217B1FB67` |
