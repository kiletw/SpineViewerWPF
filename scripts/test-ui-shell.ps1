$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\SpineViewerWPF.Wpf\SpineViewerWPF.Wpf.csproj'
$xaml = Join-Path $repository 'src\SpineViewerWPF.Wpf\MainWindow.xaml'
$output = Join-Path $repository 'artifacts\ui-shell-smoke'
$executable = Join-Path $output 'bin\Release\net8.0-windows\SpineViewerWPF.exe'

dotnet build $project -c Release -p:BaseOutputPath="$output\bin\"
if ($LASTEXITCODE -ne 0) { throw 'WPF shell build failed.' }

$automationIds = @(
    'Main.Command.OpenAsset',
    'Main.Command.Reload',
    'Main.Command.SaveProject',
    'Main.Command.SaveProjectAs',
    'Main.Command.Undo',
    'Main.Command.Redo',
    'Main.Command.Export',
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
    'Main.Inspector.Background',
    'Main.Inspector.ProjectPath'
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
    foreach ($attempt in 1..20) {
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
