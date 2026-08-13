# ADR-005: Use a Deterministic CPU Renderer for the First Vertical Slice

## Status

Accepted on 2026-07-26 after the TASK-001 prototype produced identical PNG bytes across repeated renders. Amended by TASK-008 to include bounded standard-library PNG texture decoding, by TASK-031 to reuse loaded Runtime resources during CPU playback/export, by TASK-032 to accept Runtime-neutral Track 0 Alpha and per-layer PMA, by TASK-033 to expose a bounded in-memory interactive frame path, by TASK-036 to retain that frame path as fallback/capture while normal RGBA playback moves to the GPU renderer selected by ADR-009, and by TASK-050 to compose those Runtime-neutral frames for multi-layer Screenshot and PNG-sequence output.

## Context

TASK-001 needs one deterministic, non-WPF render path. Selecting and distributing a GPU framework before the UI and production renderer requirements are measured would broaden the prototype and add deployment and licensing work.

## Decision

Use a small standard-library CPU triangle rasterizer for the 4.1 vertical slice. It supports region and mesh attachments, normal alpha blending, P3 PPM test textures, bounded 8-bit non-interlaced PNG textures, and deterministic PNG output.

The Application boundary may retain a disposable Runtime adapter session containing parsed skeleton data, atlas data, and decoded textures. Each frame still creates transient pose state, applies explicit Track 0 Alpha through the matching Runtime API, and uses the same deterministic CPU rasterizer. WPF scene layers and sequence export own and dispose these sessions explicitly.

Treat interlaced or non-8-bit PNG, other production image formats, and full GPU-equation parity beyond the bounded PMA/clipping/blend bridge as explicitly unsupported. TASK-033 permits the same CPU rasterizer to return bounded BGRA pixels with presentation-only bilinear sampling and viewport framing. ADR-009 selects the separate production WPF interactive renderer.

TASK-050 keeps the default single visible identity layer on the established Runtime PNG path. Multi-layer capture/export instead renders each visible layer to a Runtime-neutral BGRA frame and uses one Application-owned standard-library compositor and PNG writer. The compositor applies stable Z ordering, 96-DPI output-pixel transforms, layer opacity, and straight-alpha source-over onto a transparent canvas. Presentation backgrounds and viewport pan/zoom are not file-output inputs.

## Prototype Evidence

Recorded conditions:

- Runtime: official `4.1.00` tag at `ab28b77c70e3aa766be5bdb759d7aedac9fd0bde`
- fixture: project-authored `tests/fixtures/v41-minimal`
- animation/time: `move` at `0.5` seconds
- output: RGBA PNG, 64 by 64 pixels
- renderer: `CpuRenderer.cs`
- SHA-256: `7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E`

Two consecutive renders produced the same hash.

## Consequences

- The CLI path is deterministic and has no new rendering dependency. Oversized static poses are scaled down and centered from the current Runtime pose bounds; poses that already fit retain the original transform.
- Common generated PNG texture variants render through the same bounded CPU path without a package dependency.
- Interactive CPU playback and sequence export avoid reparsing the skeleton, atlas, and textures for every frame.
- Interactive WPF frames avoid PNG encoding and disk I/O while deterministic file rendering retains the recorded sampling and hashes.
- Unsupported renderer features fail explicitly.
- Normal RGBA playback uses ADR-009; this CPU path remains authoritative for deterministic file output, screenshots, channel inspection, and GPU fallback.
- Screenshot and multi-layer sequence export share one composition rule without introducing WPF or GPU types into Core/Application; the legacy single-layer byte baseline remains independently preserved.
