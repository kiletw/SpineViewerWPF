# SpineViewerWPF

SpineViewerWPF v3 is a Windows/.NET 8 viewer for Spine assets exported by
multiple historical Runtime versions.

[繁體中文](README_zhTW.md)

## Features

- Detects and loads Spine exports from `2.1.08` through `4.2` using isolated
  Runtime adapters.
- Uses an OpenTK GPU viewport for normal RGBA playback and falls back to the
  CPU renderer when GPU initialization or rendering is unavailable.
- Keeps deterministic CPU rendering for screenshots, channel inspection, CLI
  output, and PNG sequence export.
- Opens JSON or binary skeletons by dialog or drag-and-drop, discovers atlas
  dependencies, and allows manual atlas recovery.
- Provides animation/skin selection, playback and preview FPS controls,
  pan/zoom/fit, multiple scene layers, Dark/Light themes, and renderer metrics.
- Stores non-destructive layer, transform, slot visibility/opacity, and named
  attachment settings in versioned `*.spineviewer.json` sidecars.

Supported Runtime selections:

```text
2.1.08  2.1.25  3.1.07  3.2.xx  3.4.02  3.5.51  3.6.32
3.6.39  3.6.53  3.7.94  3.8.95  4.0.31  4.0.64  4.1  4.2
```

## Requirements

- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

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
needed. Omitting it uses embedded export-version detection.

## Validation

```powershell
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v42.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
```

The offline 3.8, 4.1, and 4.2 compatibility scripts use official example
assets from gitignored caches. Run the corresponding script without `-Offline`
once to populate a missing cache.

## Current boundaries

- Runtime 4.3 is not supported yet.
- Spine JSON, binary, atlas, and texture source files are read-only. Save/Save
  As writes a Viewer sidecar, not a modified Spine source file.
- Export currently produces screenshots and deterministic PNG sequences; GIF,
  video, and PSD export are not implemented.
- Multiple layers are available in the viewport, but deterministic sequence
  export currently targets the primary layer.
- Multi-track animation mixing, attachment authoring, unrestricted Adobe-style
  drag docking, runtime language switching, and MCP tools remain deferred.

Official Spine Runtime sources retain their upstream license files and commit
metadata under `runtimes/`.
