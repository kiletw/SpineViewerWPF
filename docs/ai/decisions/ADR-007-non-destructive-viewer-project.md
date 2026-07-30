# ADR-007: Non-Destructive Viewer Project

## Status

Accepted on 2026-07-26 after TASK-004 validated project round-trip, edit history, source isolation, and the WPF Inspector workflow. Amended by TASK-032 to add backward-compatible optional per-layer Track 0 Alpha and PMA fields.

## Context

The product is expected to modify content and save it. Writing version-specific Spine source data requires editable domain contracts and serializers that the current Runtime adapters do not provide. A useful edit-and-save workflow can be delivered earlier without risking source corruption.

## Decision

1. Store the first editable session in a versioned `*.spineviewer.json` sidecar.
2. Include source references, selected animation and skin, model transform, playback settings, track alpha, PMA, and background mode. Schema-version-1 scene layers may add optional Track 0 Alpha and PMA fields; missing values retain the prior top-level primary-layer fallback.
3. Save through an Application service using a temporary file in the destination directory.
4. Restrict Save and Save As to the sidecar extension.
5. Keep Spine JSON, binary, atlas, and texture sources read-only.
6. Keep dirty state and Undo/Redo in the presentation session until a real multi-document Application session exists.
7. Require a separate task and ADR before any source-writing format is introduced.

## Consequences

- Users can preserve useful edits now without changing source assets.
- The sidecar schema can evolve independently from official Runtime object models.
- Opening sidecars in the WPF UI, autosave/recovery, multi-document tabs, and source writing remain separate work.
