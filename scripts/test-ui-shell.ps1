$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\SpineViewerWPF.Wpf\SpineViewerWPF.Wpf.csproj'
$xaml = Join-Path $repository 'src\SpineViewerWPF.Wpf\MainWindow.xaml'
$executable = Join-Path $repository 'src\SpineViewerWPF.Wpf\bin\Release\net8.0-windows\SpineViewerWPF.exe'

dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) { throw 'WPF shell build failed.' }

$automationIds = @(
    'Main.Command.OpenAsset',
    'Main.Command.Reload',
    'Main.Command.Export',
    'Main.Asset.AnimationSearch',
    'Main.Asset.AnimationList',
    'Main.Asset.SkinList',
    'Main.Viewport.Surface',
    'Main.Playback.Toggle',
    'Main.Playback.Stop',
    'Main.Playback.Timeline',
    'Main.Playback.Loop',
    'Main.Viewport.Fit',
    'Main.Status.Runtime',
    'Main.Status.Diagnostics'
)
$markup = Get-Content -LiteralPath $xaml -Raw
foreach ($automationId in $automationIds) {
    if (-not $markup.Contains("AutomationProperties.AutomationId=`"$automationId`"")) {
        throw "Missing automation ID: $automationId"
    }
}

$process = $null
try {
    $process = Start-Process -FilePath $executable -ArgumentList '--state=ReadyWithWarnings --compact' -PassThru
    foreach ($attempt in 1..20) {
        Start-Sleep -Milliseconds 200
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowTitle) { break }
    }
    if ($process.HasExited -or $process.MainWindowTitle -notlike 'Spine Viewer*Quick Browse') {
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
    CompactWarningState = 'launched'
}
