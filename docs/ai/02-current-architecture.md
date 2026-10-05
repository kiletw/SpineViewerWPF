# v2 Verified Architecture Baseline

## Status

Verified against `kiletw/SpineViewerWPF` commit `79c6135` (`2.4.0.0`) on 2026-07-26.

## Reproducible Build

The checkout contains one Visual Studio solution and one non-SDK WPF project:

```text
SpineViewerWPF.sln
└── SpineViewerWPF/SpineViewerWPF.csproj
```

Prerequisites verified on Windows:

- Visual Studio 2022 MSBuild (the project declares ToolsVersion 15.0)
- .NET Framework 4.7.2 targeting pack
- Microsoft XNA Framework Redistributable 4.0; the three x86 assemblies resolve from `C:\Windows\Microsoft.NET\assembly\GAC_32`
- `nuget.exe` for the legacy `packages.config` restore

Commands:

```powershell
nuget restore SpineViewerWPF.sln -NonInteractive
& 'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe' SpineViewerWPF.sln -m -p:Configuration=Debug -verbosity:minimal
```

`dotnet msbuild -restore` is not a substitute: it does not restore this `packages.config` project and its .NET-hosted x86 resource task fails.

The verified build produced `SpineViewerWPF/bin/Debug/SpineViewerWPF.exe`. Startup smoke reached a responsive main window titled `SpineViewerWPF v2.4.0.0`. No asset smoke was possible because the repository contains no Spine fixtures.

Build warnings:

- project target is AnyCPU while XNA references are x86
- `Slot.hasSecondColor` is never assigned in the 3.6.32, 3.6.39, and 3.6.53 snapshots
- restored ImageSharp 2.1.3 and System.Drawing.Common 4.7.0 have known NuGet vulnerability advisories

## Dependencies

Direct packages from `SpineViewerWPF/packages.config`:

| Package | Version | Role |
|---|---:|---|
| ControlzEx | 3.0.2.4 | WPF interaction dependency |
| WpfXnaControl | 1.0.0 | hosts XNA in WPF |
| SixLabors.ImageSharp | 2.1.3 | GIF assembly and PNG decoding |
| System.Drawing.Common | 4.7.0 | background/image conversion |
| System.Buffers | 4.5.1 | transitive/runtime support |
| System.Memory | 4.5.4 | transitive/runtime support |
| System.Numerics.Vectors | 4.5.0 | transitive/runtime support |
| System.Runtime.CompilerServices.Unsafe | 5.0.0 | transitive/runtime support |
| System.Text.Encoding.CodePages | 5.0.0 | image encoding support |

Microsoft XNA Framework, WPF, and .NET Framework assemblies are direct non-package references.

## Source Shape and Responsibilities

| Area | Verified responsibility |
|---|---|
| `App.xaml.cs` | owns process-wide mutable UI, playback, XNA, texture, sizing, and temporary-path state |
| `MainWindow.xaml(.cs)` | binds global state; opens assets; swaps/reloads Players; controls playback, capture, export, background, settings, and device reset |
| `Windows/Open.xaml(.cs)` | collects atlas/skeleton paths, explicit Runtime version, texture suffixes, and canvas size |
| `Views/UCPlayer.xaml(.cs)` | selects one of 14 Runtime-specific Players and attaches it to `XnaControl` events; handles pan/zoom gestures |
| `PublicFunction/Player.cs` | initializes the shared graphics device/sprite batch and draws the optional background |
| `PublicFunction/GlobalValue.cs` | 38-property global state bag for asset, playback, transform, export, loading, and GPU-backed GIF frames |
| `PublicFunction/Common.cs` | path matching, state reset, transforms, screenshot, frame capture, GIF/PNG output, and temporary-file deletion |
| `PublicFunction/Player/IPlayer.cs` | common lifecycle surface: initialize, load, update, draw, reload, resize, dispose |
| `PublicFunction/Player/Player_*` | duplicated Runtime-specific load, metadata, playback, render, capture, seek, reload, and disposal logic |
| `SpineLibrary/spine-runtimes-*` | 14 vendored and namespace-renamed official Runtime snapshots, compiled directly into the WPF executable |

The project contains 597 vendored Runtime `.cs` files and 14 Player files. Largest product-owned files are `GlobalValue.cs` (634 lines), `Common.cs` (379 lines), and `MainWindow.xaml.cs` (347 lines).

## Actual Dependency Direction

```text
WPF windows and controls
        ↓
process-wide App / GlobalValue state
        ↓
Runtime-specific Player_x_x_xx
        ↓
namespace-renamed vendored Spine Runtime + XNA renderer
        ↓
XNA GraphicsDevice / textures / render targets
```

There is no separate Core, Application, adapter, renderer, export, CLI, or test project. UI, use-case, Runtime, renderer, and export concerns are mutually coupled through static state.

## Player Comparison

All 14 Players implement the same seven lifecycle methods. After normalizing only version identifiers, they form 10 source groups:

| Group | Players | Material difference |
|---|---|---|
| A | 2.1.08 | JSON only; legacy track-time API; rendering occurs in `Update` |
| B | 2.1.25 | adds binary; otherwise legacy flow |
| C | 3.1.07, 3.2.xx, 3.4.02 | source-identical apart from version names; binary + JSON |
| D | 3.5.51 | changes to `IsComplete` / `AnimationEnd` track API |
| E | 3.6.32 | changes renderer to `SkeletonRenderer` |
| F | 3.6.39 | shorter Runtime-specific variant of the 3.6 flow |
| G | 3.6.53 | 3.6 variant with its matching Runtime API |
| H | 3.7.94 | matching 3.7 Runtime APIs |
| I | 3.8.95 | rendering moves to `Draw`; time advances by `Speed / 1000f` per draw |
| J | 4.0.31, 4.0.64, 4.1.00 | source-identical apart from version names |

Common behavior:

- create an atlas, parser, skeleton, animation state, and renderer
- expose animation and skin names through `GlobalValue`
- select the remembered name when non-empty, otherwise index the first animation
- mutate skeleton state directly from global flags
- capture export frames from the live draw loop
- implement `Dispose()` by calling `ChangeSet()`, which disposes the content manager and atlas and immediately reloads

## Verified Flows

### Open and Load

```text
File → Open Spine
→ user selects/drops atlas and skeleton
→ user explicitly selects Runtime and canvas size
→ Open.btn_Open_Click
→ MainWindow.LoadPlayer(version)
→ Common.Reset
→ create or reuse UCPlayer/XnaControl
→ UCPlayer chooses Player_x_x_xx
→ XnaControl.LoadContent
→ Player loads atlas + JSON/binary + textures
→ lists animations/skins
→ selects first animation
→ auto-plays
```

Atlas discovery is only a same-basename string replacement: `.atlas` → `.skel`, then `.json`. There is no Runtime detection. The skeleton's embedded version is displayed only after the selected Runtime successfully parses it.

### Playback and Seek

- animation/skin selection sets global flags; the Player applies them on its next callback
- play/pause stores `Speed`, sets it to zero, and uses `TimeScale`/track time to preserve position
- changing loop restarts the selected animation
- there is no separate stop/reset command
- 2.1–3.7 advance using XNA elapsed time; 3.8–4.1 advance by `Speed / 1000f` per draw, so timing depends on draw frequency
- the slider writes a normalized `Lock`; the Player maps it back to Runtime-specific track time

### Render and Transform

- one shared `GraphicsDevice` and `SpriteBatch` live on `App`
- the background is drawn first, then the Runtime renderer draws the skeleton
- model position, Runtime parse scale, flips, root rotation, PMA flag, and background are global values
- viewport pan/zoom are WPF transforms on the hosting canvas and are distinct from model transforms
- initial position is centered; there is no bounds-based fit operation

### Capture and Export

Single capture pauses the global timescale, redraws into a `RenderTarget2D`, copies pixels to a new texture, and opens a PNG save dialog.

Recording starts the current animation from time zero and captures from the live draw callback:

- GIF memory mode retains one GPU `Texture2D` per frame, then converts frames on an STA worker thread
- GIF cache mode writes temporary PNGs under `<working-directory>/Temp`
- PNG sequence writes directly to the selected directory using deterministic names and `FileMode.Create`

Frame scheduling is not deterministic across Runtime groups. Existing sequence files can be overwritten. Export exceptions are not isolated from live state.

### Reload and Runtime Switch

- same Runtime: detach/re-attach control events, dispose `ContentManager` and atlas, then invoke `LoadContent` again
- different Runtime: dispose `ContentManager`, clear XNA delegates and visual parents, then create a new `UCPlayer`
- the old Player's `Dispose()` is not called on a Runtime switch

### Shutdown

Window close and Exit save `LastSelectDir` and close the auxiliary Open window. They do not explicitly stop the timer, dispose the active Player/atlas/renderer, shared sprite batch, background texture, graphics device/control, retained GIF frames, or temporary files.

## Mutable State and Resource Ownership

| Resource/state | Current owner | Release path | Verified gap |
|---|---|---|---|
| `GlobalValue` and all session state | static `App.globalValues` | process exit | combines unrelated lifetimes |
| `XnaControl` / `GraphicsDevice` | static `App.appXC` / `App.graphicsDevice` | implicit process/control teardown | no explicit shutdown |
| shared `SpriteBatch` | static `App.spriteBatch` | none | recreated on initialize without explicit disposal |
| active Player/Runtime objects | static `MainWindow.UC_Player` + instance Player | partial reload | old Player not disposed on Runtime switch |
| atlas textures | Runtime `Atlas` / `XnaTextureLoader` | `atlas.Dispose()` on same-Runtime reload | failure and switch paths are incomplete |
| background texture | static `App.textureBG` | reset/replacement | reset disposes but does not null; no shutdown release |
| GIF frame textures | `GlobalValue.GifList` | successful GIF completion | cancellation/failure can retain GPU memory |
| render targets and capture textures | `Common` locals | mostly `using`/explicit dispose | timescale/render target restoration is not exception-safe |
| temporary PNGs | `<working-directory>/Temp` | export start/end | no shutdown cleanup; `ClearCacheFile` assumes directory exists |
| export worker | untracked STA `Thread` | natural completion | no cancellation, joining, or error propagation |

## Prioritized Risks

1. No committed fixtures or compatibility tests; supported-version claims are unverified.
2. Runtime choice is manual and a wrong choice fails inside version-specific parsing.
3. Runtime switching, failed loading, and shutdown have incomplete GPU/Runtime disposal.
4. Export timing is draw-loop-dependent and differs between Runtime groups.
5. Asset-with-no-animation is unsupported because Players index animation zero.
6. AnyCPU output depends on x86 XNA and produces architecture warnings.
7. ImageSharp 2.1.3 and System.Drawing.Common 4.7.0 have known vulnerabilities.
8. Product logic is duplicated across 14 Players and coupled through static state.

## Candidate First Vertical Slice

Runtime 4.1.00 remains the best candidate: it is the newest verified v2 line, has an official matching upstream tag, supports JSON and binary in the v2 Player, and shares its Player structure with 4.0.31/4.0.64. The vertical slice must not claim compatibility until a redistributable 4.1 fixture is added.
