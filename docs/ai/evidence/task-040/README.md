# TASK-040 local evidence

TASK-040 reuses one Skeleton and AnimationStateData per serialized render
session. Application smoke performs an A-B-A request sequence where B changes
time, skin mode, Track Alpha, and slot visibility; the two A frame hashes match.

Final user-asset samples on 2026-08-08:

| Asset | Renderer | FPS | Preview work | Coalesced |
|---|---|---:|---:|---:|
| `marianne` 3.6.53 | GPU | 39.7-40.5 | 1.0-2.6 ms | 0-1 |
| `xiu` 4.1.14 | GPU | 39.9-40.3 | 2.6-3.6 ms | 0-1 |

The deterministic v3 hash and official 3.8/4.1 JSON/binary renders remain
unchanged. Timing stayed within TASK-039's low single-digit range; the evidence
supports state reuse and non-regression, not a claimed wall-clock speedup.
