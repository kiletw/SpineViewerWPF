# SpineViewerWPF

SpineViewerWPF v3 is a Windows/.NET 8 viewer for Spine assets exported by
multiple historical Runtime versions.

[繁體中文](README_zhTW.md)

## Features

- Detects and loads Spine exports from `2.1.08` through `4.3` using isolated
  Runtime adapters.
- Uses an OpenTK GPU viewport for normal RGBA playback and falls back to the
  CPU renderer when GPU initialization or rendering is unavailable.
- Keeps deterministic CPU rendering for screenshots, channel inspection, CLI
  output, and PNG sequence export.
- Opens JSON or binary skeletons by dialog or drag-and-drop, discovers atlas
  dependencies, and allows manual atlas recovery.
- Provides animation/skin selection, playback and preview FPS controls,
  pan/zoom/fit, multiple scene layers, Dark/Light themes, and renderer metrics.
- Shows setup pose for assets without animations; skins, layers, sidecar save,
  screenshots, and one-frame static export remain available.
- Remembers animation/skin per canonical asset path within the current workspace
  session; explicit sidecar selections take precedence.
- Stores non-destructive layer, transform, slot visibility/opacity, and named
  attachment settings in versioned `*.spineviewer.json` sidecars.

Supported Runtime selections:

```text
2.1.08  2.1.25  3.1.07  3.2.xx  3.4.02  3.5.51  3.6.32
3.6.39  3.6.53  3.7.94  3.8.95  4.0.31  4.0.64  4.1  4.2  4.3
```

## Download

Portable Windows x64 packages are published on the
[Releases](https://github.com/kiletw/SpineViewerWPF/releases) page. Extract the
zip and run `SpineViewerWPF.exe`; no .NET installation is required. The
executables are not code-signed, so SmartScreen may warn on first launch.

SpineViewerWPF integrates the Spine Runtimes: each user must hold their own
[Spine Editor license](https://esotericsoftware.com/spine-editor-license). See
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Requirements

- Windows
- To build: .NET SDK 9.0.300 or a later 9.0 feature band (pinned by
  `global.json`; the projects target .NET 8) and the
  [.NET 8 runtime](https://dotnet.microsoft.com/download/dotnet/8.0) for running
  from source

An OpenGL-capable GPU is recommended for interactive playback. The application
reports the active backend and retains a CPU fallback.

## Projects

- WPF application: `src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj`
- CLI: `src/SpineViewerWPF.Cli/SpineViewerWPF.Cli.csproj`
- Application/Core contracts: `src/SpineViewerWPF.Application` and
  `src/SpineViewerWPF.Core`
- Isolated Runtime adapters: `runtimes/SpineRuntime.*`

## Build and run

```powershell
dotnet restore SpineViewerWPF.sln
dotnet build SpineViewerWPF.sln -c Release --no-restore
dotnet run --project src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
```

CLI examples:

```powershell
dotnet run --project src/SpineViewerWPF.Cli/SpineViewerWPF.Cli.csproj -c Release -- inspect "asset.skel" --atlas "asset.atlas" --format json
dotnet run --project src/SpineViewerWPF.Cli/SpineViewerWPF.Cli.csproj -c Release -- render "asset.skel" --atlas "asset.atlas" --animation "idle" --time 0.5 --output "frame.png" --overwrite
```

The `--runtime` option is available when an explicit Runtime override is
needed. Omitting it uses embedded export-version detection. For assets with no
animations, omit `--animation` on `render` to output setup pose (`--time` remains
required); animated assets still require a named animation.

## Validation

```powershell
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v42.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v43.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
```

The offline 3.8, 4.1, 4.2, and 4.3 compatibility scripts use official example
assets from gitignored caches. Run the corresponding script without `-Offline`
once to populate a missing cache.

GitHub Actions runs the build, Application.Smoke, and `scripts/test-v3.ps1` on
every pull request and on pushes to `master`.

## Release

Push a SemVer tag on a `master` commit to publish a GitHub Release:

```powershell
git tag v3.0.0-alpha.1
git push origin v3.0.0-alpha.1
```

Tags with a suffix such as `-alpha.1` become prereleases. To build the same
packages locally into `artifacts/release`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/publish-release.ps1 -Version 3.0.0-local.1
```

## Current boundaries

- Runtime 4.3 is connected and has pinned official JSON/binary fixture coverage.
  This is not a full compatibility claim: official 4.2/4.3 PMA, multi-page atlas,
  feature-isolated blend parity, and long-timeline Physics fidelity remain limited
  or unverified. See the [compatibility matrix](docs/ai/04-runtime-matrix.md).
- Spine JSON, binary, atlas, and texture source files are read-only. Save/Save
  As writes a Viewer sidecar, not a modified Spine source file.
- Export currently produces screenshots and deterministic PNG sequences; GIF,
  video, and PSD export are not implemented.
- Screenshot and PNG sequence export include ordered visible scene layers, their
  transforms, opacity, and slot settings. Viewport backgrounds are not baked in;
  all-hidden scenes export transparent frames.
- Multi-track animation mixing, attachment authoring, unrestricted Adobe-style
  drag docking, runtime language switching, and MCP tools remain deferred.

Official Spine Runtime sources retain their upstream license files and commit
metadata under `runtimes/`.
