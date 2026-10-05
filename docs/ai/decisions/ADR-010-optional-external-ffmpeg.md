# ADR-010: Encode animated exports through an optional, user-installed FFmpeg

## Status

Accepted on 2026-10-02 for TASK-066.

## Context

Roadmap item 5 in `17-external-feature-reference.md` asks for GIF, WebP, APNG,
and MP4 export. The deterministic PNG sequence export (TASK-063) already
produces correctly framed RGBA frames. Encoding those formats in managed code
would need several new packages (GIF quantization, WebP, APNG, H.264) with
mixed licenses, maintenance, and patent exposure.

## Decision

- Encoded formats are produced by running an external `ffmpeg` executable over
  a temporary PNG sequence rendered by the existing export path.
- FFmpeg is not bundled or downloaded. It is located from a user-chosen path,
  otherwise from `PATH`. Without it, encoded export reports an actionable
  diagnostic and PNG sequence export keeps working.
- The process boundary lives in Application (`System.Diagnostics.Process` only,
  no new package). Core gains additive, Runtime-neutral option and result
  records. WPF only chooses paths and shows progress.
- Arguments are passed as an argument list, never through a shell. Temporary
  frames and the intermediate file are deleted on success, failure, and
  cancellation; cancellation terminates the FFmpeg process tree.
- The user-chosen FFmpeg path is the first cross-session user setting. It is
  stored in `%APPDATA%\SpineViewerWPF\settings.json`, never in the Viewer
  project sidecar, and a missing or corrupt file falls back to defaults.

## Rejected Alternatives

- Bundling FFmpeg: large binaries and GPL/LGPL redistribution obligations.
- Managed encoder packages: several dependencies and incomplete alpha support
  across the requested formats.
- Downloading FFmpeg on demand: network access, integrity checks, and update
  policy outside the viewer's scope.

## Consequences

- Encoded output depends on the user's FFmpeg build and its encoders
  (`gif`, `libwebp_anim`, `apng`, `libx264`); a missing encoder is reported
  from FFmpeg's error output.
- Output is not byte-stable across FFmpeg versions; the PNG frames remain the
  deterministic reference.
- Exports temporarily use disk space for the PNG frames under the system temp
  directory.
