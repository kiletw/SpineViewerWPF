# ADR-005: Use a Deterministic CPU Renderer for the First Vertical Slice

## Status

Accepted on 2026-07-26 after the TASK-001 prototype produced identical PNG bytes across repeated renders. Amended by TASK-008 to include bounded standard-library PNG texture decoding without selecting a production renderer.

## Context

TASK-001 needs one deterministic, non-WPF render path. Selecting and distributing a GPU framework before the UI and production renderer requirements are measured would broaden the prototype and add deployment and licensing work.

## Decision

Use a small standard-library CPU triangle rasterizer for the 4.1 vertical slice. It supports region and mesh attachments, normal alpha blending, P3 PPM test textures, bounded 8-bit non-interlaced PNG textures, and deterministic PNG output.

Treat PMA, clipping attachments, non-normal blend modes, interlaced or non-8-bit PNG, other production image formats, and interactive rendering as explicitly unsupported. This spike does not select the production WPF renderer.

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

- The CLI path is deterministic and has no new rendering dependency.
- Common generated PNG texture variants render through the same bounded CPU path without a package dependency.
- Unsupported renderer features fail explicitly.
- A production renderer still requires separate prototype evidence and an ADR update or replacement.
