# Behavior Contracts

## Status

v2 behavior below is verified from commit `79c6135`. “v3 contract” identifies the intended replacement behavior; it is not a claim about v2.

## Asset Opening

### Verified v2

- Open is a modal-like auxiliary window reached from the File menu.
- The user must provide an atlas, JSON or `.skel`, Runtime version, and canvas size.
- Dropping an atlas or skeleton into its matching textbox is supported.
- Atlas discovery checks only the same basename, preferring `.skel` over `.json`.
- Separate missing atlas, skeleton, and texture diagnostics are not implemented.
- Multi-texture suffixes are manually entered and labeled for 3.8.95 or higher.
- A wrong Runtime is not detected before parsing.

### v3 contract

- Accept supported JSON and binary skeleton formats.
- Allow explicit atlas selection and attempt same-asset atlas discovery.
- Report missing skeleton, atlas, and texture files distinctly.
- Validate an explicit Runtime override and never silently select an incompatible Runtime.

## Runtime Selection

### Verified v2

- Runtime selection is always explicit through one of 14 combo-box values.
- The parsed skeleton version is displayed only after load.
- Switching Runtime rebuilds the WPF Player surface; selecting the same Runtime reloads the existing Player.
- Unsupported/mismatched data fails through Runtime parsing; there is no actionable compatibility diagnostic.

### v3 contract

- Detect the export version where reliable.
- Resolve by verified Runtime line; retain patch-level distinctions only when fixtures prove they matter.
- Make an explicit override visible and reload through the shared Application use case.
- Return an actionable unsupported-version diagnostic.

## Quick Browse

### Verified v2

- A settings/open window is required before first preview.
- Successful load selects animation index zero and auto-plays it.
- Animation memory is process-global, not per asset.
- Initial model position is centered, but there is no bounds-based fit.
- An asset with zero animations is not handled; every Player indexes the first animation.

### v3 contract

- A valid asset reaches visible preview without an advanced settings dialog.
- Fit the model in the viewport after successful load.
- Prefer the last remembered animation for that asset when valid; otherwise select the first.
- Auto-play is retained from verified v2 behavior.
- Assets without animations show setup pose and remain usable. TASK-059 uses
  an empty animation string in render requests and scene-layer sidecars, and
  null for the Shell selection when the animation list is empty. Named animation
  requests still require an existing animation; no synthetic names are added.
- Selection memory lasts for one Shell/workspace session, keyed by the existing
  full skeleton path with case-insensitive Windows comparison. Direct open/reload
  uses remembered valid animation/skin, otherwise the first available values
  (no animation means setup pose). Sidecar values take precedence.

## Playback

### Verified v2

- Play/pause preserves approximate current position through Runtime-specific track state.
- There is no distinct stop/reset command.
- Selecting an animation or changing loop restarts the selected animation.
- Speed is stored as an integer nominally centered on 30.
- 2.1–3.7 advance from XNA elapsed time; 3.8–4.1 advance a fixed `Speed / 1000f` per draw.
- Timeline position is a normalized value mapped to Runtime-specific track time.

### v3 contract

- Play resumes or starts the selected animation.
- Pause preserves exact current time.
- Define stop/reset only after a product decision; do not infer it from v2.
- Loop changes apply predictably to the selected animation.
- Playback speed and seeking are independent of UI refresh/draw frequency.

## Viewport and Model Transform

### Verified v2

- Ctrl+wheel and Ctrl+drag change WPF viewport scale/translation.
- Alt+wheel and Alt+drag change skeleton scale/position.
- Flip X/Y, root rotation, PMA, background visibility, and background position are available.
- Changing model scale reparses the skeleton in the live draw/update path.
- “Fit” is not implemented.

### v3 contract

- Keep viewport pan/zoom distinct from model position/scale.
- Fit changes the view, not model data.
- Preserve flip, rotation, position, scale, PMA, and background behavior subject to fixture comparison.

## Capture and Export

### Verified v2

- Capture uses the current visual configuration and opens a PNG save dialog.
- Recording supports GIF and PNG sequence.
- GIF can retain GPU frames in memory or cache temporary PNGs.
- PNG sequence uses `FileMode.Create` and can overwrite matching filenames.
- Timing is coupled to the live Player/draw path and differs by Runtime group.
- Export errors are not caught or isolated from session state.

### v3 contract

- Capture uses the current visual configuration.
- Deterministic export timing is independent from interactive frame rate.
- A pure static setup-pose WPF scene has zero duration and exports one frame under
  the existing inclusive frame schedule. Setup-pose layers remain static in mixed
  scenes. Setup pose does not advance Physics with time; playback is disabled
  for a selected zero-duration layer, while screenshot/export remain available.
- Existing files are not overwritten without an explicit policy.
- Export failure does not unload or corrupt the active session.
- Export owns cancellation, progress, and all temporary resources.

## Resource Lifetime

### Verified v2

- Same-Runtime reload disposes the atlas and `ContentManager`.
- Runtime switch does not call the old Player's `Dispose()`.
- Partially loaded resources have no failure cleanup boundary.
- Window shutdown saves settings but does not explicitly release Runtime/GPU/export resources.

### v3 contract

- Loading a new asset releases the prior Runtime and GPU resources exactly once.
- Failed loads clean up all partially created resources.
- Closing releases renderer, textures, streams, temporary files, and workers.
- UI state never owns Runtime- or GPU-specific objects.
