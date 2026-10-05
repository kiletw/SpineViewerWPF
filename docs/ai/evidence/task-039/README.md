# TASK-039 local user-asset evidence

Run from the repository root on an interactive Windows desktop:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-user-playback-metrics.ps1
```

The script reads but never copies or changes the non-redistributable assets in
`D:\Spine測試`. It verifies the known 3.6.53 `marianne` and 4.1.14 `xiu`
CLI metadata contracts, opens both assets through the real WPF startup path, and
samples `Main.Status.Performance` for three seconds. A missing asset, unexpected
metadata result, absent metric, incomplete metric label, or ambiguous renderer
status fails the run.

Observed CLI baseline before live-metric integration:

| Asset | Export | Runtime | Animations | Skins | Textures |
|---|---:|---:|---:|---:|---:|
| `marianne` | 3.6.53 | 3.6.53 | 5 | 3 | 5 |
| `xiu` | 4.1.14 | 4.1 | 2 | 1 | 1 |

Final live GPU playback samples on 2026-08-08:

| Asset | Sampled FPS | Average preview work | Coalesced |
|---|---:|---:|---:|
| `marianne` | 39.8-40.0 | 1.3-2.2 ms | 0 |
| `xiu` | 38.6-40.4 | 2.4-3.2 ms | 0 |

Both assets reported `GPU`; CLI inspection and the bounded WPF playback run
completed successfully.
