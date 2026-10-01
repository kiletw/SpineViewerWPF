# TASK-066: GIF / WebP / APNG / MP4 export through FFmpeg

Status: Completed on 2026-10-02.

## Objective

Roadmap item 5 from `17-external-feature-reference.md`: export the current
animation as GIF, animated WebP, APNG, or MP4 (H.264) by encoding the existing
deterministic PNG frames with a user-installed FFmpeg (ADR-010). WPF only.

## Context

- `decisions/ADR-010-optional-external-ffmpeg.md`
- `12-application-use-cases.md` (`ExportAnimation`), `TASK-063` (framing)
- `16-ui-state-and-interaction-contracts.md`
- User decisions (2026-10-02): formats GIF, WebP, APNG, MP4; find FFmpeg on
  `PATH` with a manual override; remember the override in a user settings
  file; no CLI command in this task.

## Scope

Branch `codex/ffmpeg-export`, stacked on `codex/playback-transport-and-slot-actions`
(PR #55).

## Allowed Paths

- `src/SpineViewerWPF.Core/Contracts.cs` (additive records/enum)
- `src/SpineViewerWPF.Application/**` (encoded export, FFmpeg locator/runner)
- `src/SpineViewerWPF.Wpf/**` (export settings UI, settings store, dialogs, text)
- `tests/SpineViewerWPF.Application.Smoke/**`, `scripts/test-ui-shell.ps1`
- `docs/ai/00-context-index.md`, `12-application-use-cases.md`,
  `16-ui-state-and-interaction-contracts.md`, `17-external-feature-reference.md`,
  `decisions/ADR-010-*`, this task

## Forbidden Paths

- `runtimes/**`, CLI, sidecar schema, renderer output of the PNG path
- new NuGet packages; bundling or downloading FFmpeg

## Required Behavior

- Export settings gain Format: PNG sequence (default), GIF, WebP, APNG, MP4.
  PNG sequence keeps the existing behavior and file naming.
- Encoded formats reuse the same frame request (FPS, framing, layers, slots)
  and write one file chosen in a format-specific save dialog. The dialog's
  overwrite confirmation allows replacing that file; otherwise an existing
  output fails before rendering.
- GIF: generated palette with transparency, infinite loop (frame delays are
  rounded to FFmpeg's 1/100 s GIF timing). WebP: `libwebp_anim`, quality 90,
  alpha, infinite loop. APNG: `apng`, alpha, infinite loop. MP4: `libx264`,
  `yuv420p`, CRF 18, composited over a `#RRGGBB` background (default
  `#000000`) and padded to even dimensions.
- FFmpeg is resolved from the remembered path when that file exists, otherwise
  from `PATH`. Export settings show the resolved path or "not found", a Browse
  button, and "Use PATH" to forget the custom path. Missing FFmpeg yields an
  `EXPORT_FAILED` diagnostic explaining how to fix it.
- Progress covers rendering and encoding; Cancel stops rendering or kills
  FFmpeg. Temporary frames and partial outputs are always removed.
- The FFmpeg path persists in `%APPDATA%\SpineViewerWPF\settings.json`; format
  and MP4 background are session preferences. None dirty the project.

## Validation

```powershell
dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false
$env:SPINEVIEWER_SKIP_WINDOW_SMOKE = '1'
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
git diff --check
```

Smoke: when FFmpeg is on `PATH`, each format writes a file with the right
signature (GIF89a, RIFF/WEBP with ANIM, PNG with acTL, MP4 ftyp) and leaves no
temporary directory; existing output without overwrite fails before
rendering; a missing FFmpeg path and an invalid background are rejected;
pre-canceled export writes nothing; settings round-trip and tolerate a corrupt
file. Without FFmpeg the encoding checks are skipped and reported.

Manual: export spineboy `run` to each format and open the results; cancel an
MP4 export mid-way; choose and reset a custom FFmpeg path; restart the app and
confirm the path is remembered.

## Completion Report

### Changed

- Core: `AnimationEncodeFormat`, `AnimationEncodeOptions`, `EncodedAnimationResult` (additive).
- Application: `AssetService.ExportEncoded`; `FfmpegLocator` (remembered path,
  then `PATH`); internal `FfmpegEncoder` (argument list per format, process run
  with progress, stderr tail, process-tree kill on cancel).
  `InternalsVisibleTo` the smoke project for the encoder test.
- WPF: `UserSettingsStore` (`%APPDATA%\SpineViewerWPF\settings.json`, atomic
  write, corrupt file ignored); Export settings Format, MP4 Background, FFmpeg
  status with Browse / Use PATH; format-specific save dialog; "Rendering n/N"
  then "Encoding n/N" progress.
- Tests: `EncodedExportSmoke.cs`; 5 AutomationIds in `test-ui-shell.ps1`.
- Docs: ADR-010, `12`, `16`, `17`, index.

### Preserved

- PNG sequence export, file naming, and byte baselines (`test-v3.ps1` SHA-256
  unchanged); sidecar schema; CLI.

### Validation (2026-10-02, Windows, FFmpeg 6.1 full build on PATH)

- `dotnet build SpineViewerWPF.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false`: 0 warnings, 0 errors.
- Application.Smoke: passed, including all four formats with signature checks,
  progress ending at 2 x frames, refused and confirmed overwrite, missing
  FFmpeg, invalid background, pre-canceled and post-render cancellation,
  cancellation of a running ten-minute FFmpeg encode within the time limit with
  no FFmpeg process left, no leftover temp directory, settings round-trip and
  corrupt-file handling, and a Shell GIF export.
- `scripts/test-v3.ps1`: passed. `scripts/test-ui-shell.ps1`: passed (101 AutomationIds).
- `git diff --check`: passed.
- Manual (spineboy 4.1 through the WPF UI): `run` GIF 21 frames 665 x 714 with
  alpha (frame inspected); `run` WebP 21 ANMF frames with alpha and infinite
  loop; `portal` WebP at 60 FPS / scale 2, 191 frames; FFmpeg status showed the
  PATH executable.

### Known Gaps

- `libwebp_anim` reported progress only when it finished, so WebP showed
  "Encoding 0/N" until the end. TBD: whether MP4 reports intermediate encoding
  progress was not observed.
- GIF frame delays use 1/100 s units, so FPS above 50 or non-divisors of 100 are
  rounded by FFmpeg.
- CPU PNG rendering dominates large exports (about 0.5 frames per second for
  191 frames at 2x scale in the manual run); this is the existing export path.
- APNG and MP4 were exercised by the smoke tests, not through the save dialog.
- Browse, Use PATH, and the remembered path were verified through the view model
  and settings file in smoke tests; choosing a path in the real dialog and
  restarting the app was not done manually.
