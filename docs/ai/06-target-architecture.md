# v3 Target Architecture

## Solution Shape

```text
SpineViewerWPF.sln
├── src/
│   ├── SpineViewerWPF.Core
│   ├── SpineViewerWPF.Application
│   ├── SpineViewerWPF.RuntimeAdapters
│   ├── SpineViewerWPF.Rendering
│   ├── SpineViewerWPF.Export
│   ├── SpineViewerWPF.App.Wpf
│   ├── SpineViewerWPF.Cli
│   └── SpineViewerWPF.Mcp              # deferred
├── runtimes/
│   ├── SpineRuntime.V41
│   └── additional pinned Runtime lines
├── tests/
│   ├── SpineViewerWPF.Core.Tests
│   ├── SpineViewerWPF.Application.Tests
│   ├── SpineViewerWPF.Compatibility.Tests
│   └── SpineViewerWPF.Ui.Tests         # later
└── docs/
```

Names are provisional until the first prototype.

## Core Concepts

- `SpineExportVersion`
- `RuntimeDescriptor`
- `AssetReference`
- `AnimationDescriptor`
- `SkinDescriptor`
- `PlaybackState`
- `ModelTransform`
- `ViewportState`
- `Diagnostic`

## Application Use Cases

- Detect Runtime version
- Inspect and validate asset
- Open/close asset session
- Select animation and skins
- Control playback and seek
- Change model/viewport/background settings
- Render/capture frame
- Export animation

## Runtime Boundary

Runtime adapters translate official version-specific APIs to stable project contracts. No official Runtime object crosses into WPF, CLI, or MCP.

## Renderer Boundary

Renderer owns graphics device, textures, render targets, resize/device recovery, drawing, and capture. Application owns intent and deterministic orchestration.

## UI Boundary

WPF is a quick-browse adapter with presentation DTOs, commands, and explicit states. ViewModels do not own Runtime or GPU objects.
