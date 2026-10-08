<#
.SYNOPSIS
Regenerates the SpineViewerWPF application icon (TASK-070).

.DESCRIPTION
Draws the icon with WPF vector geometry and writes a multi-size .ico whose
entries are PNG-compressed. Sizes of 24 px and below use a simplified single-bone
mark so they stay legible. Run with Windows PowerShell 5.1 (STA by default).
#>
param(
    [string]$Output = (Join-Path $PSScriptRoot '..\src\SpineViewerWPF.Wpf\Assets\AppIcon.ico'),
    [string]$PreviewDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$accent = [Windows.Media.Color]::FromRgb(0x58, 0xC7, 0xAD)

function New-Brush([byte]$r, [byte]$g, [byte]$b) {
    $brush = New-Object Windows.Media.SolidColorBrush ([Windows.Media.Color]::FromRgb($r, $g, $b))
    $brush.Freeze()
    $brush
}

# A bone is a teardrop: a joint circle at the base tapering to a point at the tip.
function Add-Bone($context, $brush, [double]$bx, [double]$by, [double]$tx, [double]$ty, [double]$halfWidth) {
    $dx = $tx - $bx; $dy = $ty - $by
    $length = [Math]::Sqrt($dx * $dx + $dy * $dy)
    $nx = -$dy / $length * $halfWidth; $ny = $dx / $length * $halfWidth
    $figure = New-Object Windows.Media.PathFigure
    $figure.StartPoint = New-Object Windows.Point ($bx + $nx), ($by + $ny)
    $figure.IsClosed = $true
    $figure.Segments.Add((New-Object Windows.Media.LineSegment (New-Object Windows.Point $tx, $ty), $true))
    $figure.Segments.Add((New-Object Windows.Media.LineSegment (New-Object Windows.Point ($bx - $nx), ($by - $ny)), $true))
    $geometry = New-Object Windows.Media.PathGeometry
    $geometry.Figures.Add($figure)
    $context.DrawGeometry($brush, $null, $geometry)
    $context.DrawEllipse($brush, $null, (New-Object Windows.Point $bx, $by), $halfWidth, $halfWidth)
}

function New-IconFrame([int]$size) {
    $visual = New-Object Windows.Media.DrawingVisual
    $context = $visual.RenderOpen()
    $context.PushTransform((New-Object Windows.Media.ScaleTransform ($size / 256.0), ($size / 256.0)))

    $background = New-Object Windows.Media.LinearGradientBrush (
        [Windows.Media.Color]::FromRgb(0x1A, 0x3A, 0x34)),
        ([Windows.Media.Color]::FromRgb(0x0B, 0x0E, 0x13)),
        (New-Object Windows.Point 0, 0),
        (New-Object Windows.Point 1, 1)
    $background.Freeze()
    $border = New-Object Windows.Media.Pen (New-Brush 0x2E 0x5E 0x54), 6
    $accentBrush = New-Object Windows.Media.SolidColorBrush $accent
    $jointBrush = New-Brush 0x0B 0x0E 0x13

    if ($size -le 24) {
        $context.DrawRoundedRectangle($background, $null, (New-Object Windows.Rect 0, 0, 256, 256), 48, 48)
        Add-Bone $context $accentBrush 74 182 206 50 44
        $context.DrawEllipse($jointBrush, $null, (New-Object Windows.Point 74, 182), 16, 16)
    }
    else {
        $context.DrawRoundedRectangle($background, $border, (New-Object Windows.Rect 11, 11, 234, 234), 52, 52)
        # Root bone, child bone, and the child's end joint form a bent limb.
        Add-Bone $context $accentBrush 70 186 134 88 26
        Add-Bone $context $accentBrush 134 88 204 132 20
        $context.DrawEllipse($jointBrush, $null, (New-Object Windows.Point 70, 186), 11, 11)
        $context.DrawEllipse($jointBrush, $null, (New-Object Windows.Point 134, 88), 8, 8)
        $context.DrawEllipse($accentBrush, $null, (New-Object Windows.Point 204, 132), 13, 13)
    }

    $context.Pop()
    $context.Close()
    $bitmap = New-Object Windows.Media.Imaging.RenderTargetBitmap $size, $size, 96, 96, ([Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = New-Object IO.MemoryStream
    $encoder.Save($stream)
    , $stream.ToArray()
}

$frames = foreach ($size in $sizes) { , (New-IconFrame $size) }

$directory = Split-Path -Parent $Output
if (-not (Test-Path $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
$file = [IO.File]::Create($Output)
try {
    $writer = New-Object IO.BinaryWriter $file
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $dimension = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
        $offset += $frames[$i].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
    $writer.Flush()
}
finally {
    $file.Dispose()
}

if ($PreviewDirectory) {
    if (-not (Test-Path $PreviewDirectory)) { New-Item -ItemType Directory -Path $PreviewDirectory | Out-Null }
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        [IO.File]::WriteAllBytes((Join-Path $PreviewDirectory "icon-$($sizes[$i]).png"), $frames[$i])
    }
}

Write-Host "Wrote $Output ($($sizes -join ', ') px)"
