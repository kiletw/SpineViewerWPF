# ADR-008: In-Memory Interactive Preview Before GPU Selection

## Status

Accepted on 2026-07-30 by TASK-033 after in-memory frame, channel-conversion, user-asset framing, and deterministic file-baseline validation. Amended by TASK-036: the path remains the CPU fallback and channel-inspection source while normal RGBA playback uses ADR-009.

## Context

The WPF shell currently asks the deterministic CPU renderer to encode every interactive frame as a fixed 512-by-512 PNG, writes it to disk, reopens it as a WPF bitmap, and scales it into the viewport. This makes disk and PNG encoding part of playback, ignores physical viewport dimensions, and magnifies nearest-sampling artifacts.

The repository does not contain a project-owned production GPU renderer. ADR-005 explicitly leaves that renderer undecided, and the technical constraints prohibit selecting a large renderer or package without bounded evidence.

## Decision

Add a Runtime-neutral BGRA frame result to the existing Application render-session boundary.

- Interactive WPF rendering returns bounded in-memory pixels.
- WPF converts those pixels to a frozen `BitmapSource`.
- Interactive texture sampling may use bilinear filtering.
- Deterministic file rendering and CLI hashes keep the existing path and sampling.
- The WPF render size follows the viewport's physical-pixel aspect within explicit CPU/memory limits.
- Large interactive poses are bounds-centered and only scaled down; tiny world-space fixtures retain their prior camera behavior.

This is the CPU interactive fallback and channel-inspection baseline. ADR-009 owns the production GPU renderer decision.

## Evidence Required

- Repeated playback does not create or rewrite preview PNG files.
- The user-supplied 4.1 asset is fully framed in a 1536-by-1024 interactive frame.
- Interactive output is visibly less pixelated than the fixed 512 PNG path and exposes a correct grayscale Alpha view.
- Deterministic CLI and official Runtime validation hashes remain unchanged.

## Consequences

- PNG encoding and disk I/O leave the playback loop.
- WPF receives a presentation-safe pixel DTO rather than Runtime-specific objects.
- CPU rasterization remains the performance and exact blend-parity ceiling.
- A later GPU prototype can replace the frame producer without changing WPF channel inspection or Application intent.
