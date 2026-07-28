$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\SpineViewerWPF.Wpf\SpineViewerWPF.Wpf.csproj'
$xaml = Join-Path $repository 'src\SpineViewerWPF.Wpf\MainWindow.xaml'
$output = Join-Path $repository 'artifacts\ui-shell-smoke'
$executable = Join-Path $output 'bin\Release\net8.0-windows\SpineViewerWPF.exe'

# Clear only a stale copy of this smoke executable before MSBuild tries to replace its DLLs.
Get-Process -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $executable } |
    ForEach-Object {
        $_.CloseMainWindow() | Out-Null
        if (-not $_.WaitForExit(1000)) { Stop-Process -Id $_.Id -Force }
    }

$buildMutex = [Threading.Mutex]::new($false, 'SpineViewerWPF.Build')
$buildHeld = $false
try {
    try { $buildHeld = $buildMutex.WaitOne([TimeSpan]::FromMinutes(5)) }
    catch [Threading.AbandonedMutexException] { $buildHeld = $true }
    if (-not $buildHeld) { throw 'Timed out waiting for the shared MSBuild lock.' }
    dotnet build $project -c Release -p:BaseOutputPath="$output\bin\"
}
finally {
    if ($buildHeld) { $buildMutex.ReleaseMutex() }
    $buildMutex.Dispose()
}
if ($LASTEXITCODE -ne 0) { throw 'WPF shell build failed.' }

$automationIds = @(
    'Main.Command.OpenAsset',
    'Main.Command.OpenProject',
    'Main.Command.Reload',
    'Main.Command.SaveProject',
    'Main.Command.SaveProjectAs',
    'Main.Command.Undo',
    'Main.Command.Redo',
    'Main.Command.Export',
    'Main.Command.Window',
    'Main.Window.ToggleBrowse',
    'Main.Window.ToggleInspector',
    'Main.Window.FloatBrowse',
    'Main.Window.FloatInspector',
    'Main.Window.RedockPanels',
    'Main.Window.ResetLayout',
    'Main.Export.Cancel',
    'Main.Asset.AnimationSearch',
    'Main.Asset.AnimationList',
    'Main.Asset.SkinList',
    'Main.Viewport.Surface',
    'Main.Viewport.RenderedPreview',
    'Main.Playback.Toggle',
    'Main.Playback.Stop',
    'Main.Playback.Timeline',
    'Main.Playback.Loop',
    'Main.Viewport.Fit',
    'Main.Status.Runtime',
    'Main.Status.Diagnostics',
    'Main.Inspector.Panel',
    'Main.Inspector.ModelScale',
    'Main.Inspector.ExportFps',
    'Main.Inspector.Background',
    'Main.Inspector.ProjectPath',
    'Main.Scene.LayerList',
    'Main.Scene.AddLayer',
    'Main.Scene.RemoveLayer',
    'Main.Scene.LayerUp',
    'Main.Scene.LayerDown',
    'Main.Scene.AutoLayout',
    'Main.Scene.LayerX',
    'Main.Scene.LayerY',
    'Main.Scene.LayerScale',
    'Main.Scene.LayerOpacity',
    'Main.Scene.LayerAnimation',
    'Main.Scene.LayerSkin',
    'Main.Viewport.SceneLayers'
)
$markup = Get-Content -LiteralPath $xaml -Raw
foreach ($automationId in $automationIds) {
    if (-not $markup.Contains("AutomationProperties.AutomationId=`"$automationId`"")) {
        throw "Missing automation ID: $automationId"
    }
}

$shortcuts = @(
    'Key="S" Modifiers="Control" Command="{Binding SaveCommand}"',
    'Key="S" Modifiers="Control+Shift" Command="{Binding SaveAsCommand}"',
    'Key="Z" Modifiers="Control" Command="{Binding UndoCommand}"',
    'Key="Y" Modifiers="Control" Command="{Binding RedoCommand}"'
)
foreach ($shortcut in $shortcuts) {
    if (-not $markup.Contains($shortcut)) { throw "Missing shortcut: $shortcut" }
}

$process = $null
try {
    $process = Start-Process -FilePath $executable -ArgumentList '--state=ReadyWithWarnings --compact' -PassThru
    foreach ($attempt in 1..50) {
        Start-Sleep -Milliseconds 200
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowTitle) { break }
    }
    if ($process.HasExited -or $process.MainWindowTitle -notlike 'Spine Viewer*') {
        throw 'WPF shell did not expose its main window.'
    }
}
finally {
    if ($process -and -not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(3000)) { Stop-Process -Id $process.Id -Force }
    }
}

[pscustomobject]@{
    Build = 'passed'
    AutomationIds = $automationIds.Count
    EditorShortcuts = $shortcuts.Count
    CompactWarningState = 'launched'
}
