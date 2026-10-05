# Third-Party Notices

SpineViewerWPF release packages include or are built from the following
third-party components.

## Spine Runtimes

Copyright (c) 2013-2025, Esoteric Software LLC.

The Runtime adapters under `runtimes/SpineRuntime.*` integrate official Spine
Runtimes (spine-csharp) for export lines 2.1.08 through 4.3. Each line keeps the
license version it was published under. Release packages include every license
text in `licenses/spine-runtimes/`.

**Each user of SpineViewerWPF must obtain their own Spine Editor license.** See
<https://esotericsoftware.com/spine-editor-license>.

## OpenTK

OpenTK and OpenTK.GLWpfControl, MIT License.
<https://github.com/opentk/opentk> and <https://github.com/opentk/GLWpfControl>

OpenTK.redist.glfw redistributes GLFW, zlib/libpng License.
<https://www.glfw.org/license.html>

## .NET

Self-contained packages include the .NET runtime and Windows Desktop runtime,
MIT License, with their own third-party notices.
<https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT>

## FFmpeg

FFmpeg is not included in release packages. Any FFmpeg-based export uses an
executable that the user installs separately and that remains under its own
license.
