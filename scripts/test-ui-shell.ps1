$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\SpineViewerWPF.Wpf\SpineViewerWPF.Wpf.csproj'
$xaml = Join-Path $repository 'src\SpineViewerWPF.Wpf\MainWindow.xaml'
$codeBehind = Join-Path $repository 'src\SpineViewerWPF.Wpf\MainWindow.xaml.cs'
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
    'Main.Inspector.TrackAlpha',
    'Main.Scene.LayerPma',
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

$code = Get-Content -LiteralPath $codeBehind -Raw
if (-not $code.Contains('DataContext = owner.DataContext;')) {
    throw 'Floating panels do not inherit the shell view model.'
}
if (-not $markup.Contains('Click="ToggleInspectorPanel"') -or
    $markup.Contains('Command="{Binding ToggleInspectorCommand}"')) {
    throw 'Inspector toggles do not share the dock-aware path.'
}
if ($markup.Contains('FAKE PRESENTATION DATA') -or
    $markup.Contains('Main.Prototype.StatePicker') -or
    $markup.Contains('Command="{Binding CycleStateCommand}"')) {
    throw 'Prototype-only controls remain in the normal shell.'
}

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Find-AutomationId {
    param(
        [System.Windows.Automation.AutomationElement] $Root,
        [string] $AutomationId,
        [int] $TargetProcessId = 0
    )

    $idCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)
    $condition = $idCondition
    if ($TargetProcessId) {
        $processCondition = [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
            $TargetProcessId)
        $condition = [System.Windows.Automation.AndCondition]::new(
            [System.Windows.Automation.Condition[]]@($idCondition, $processCondition))
    }
    $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Wait-AutomationId {
    param(
        [System.Windows.Automation.AutomationElement] $Root,
        [string] $AutomationId,
        [int] $TargetProcessId = 0
    )

    foreach ($attempt in 1..25) {
        $element = Find-AutomationId $Root $AutomationId $TargetProcessId
        if ($element) { return $element }
        Start-Sleep -Milliseconds 100
    }
    $null
}

function Invoke-AutomationElement {
    param([System.Windows.Automation.AutomationElement] $Element)

    $pattern = $Element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    ([System.Windows.Automation.InvokePattern] $pattern).Invoke()
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

    $mainWindow = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    Invoke-AutomationElement (Wait-AutomationId $mainWindow 'Main.Command.Window')
    $floatBrowse = Wait-AutomationId ([System.Windows.Automation.AutomationElement]::RootElement) 'Main.Window.FloatBrowse' $process.Id
    if (-not $floatBrowse) { throw 'Window menu did not expose Float Browse.' }
    Invoke-AutomationElement $floatBrowse

    $animationList = Wait-AutomationId ([System.Windows.Automation.AutomationElement]::RootElement) 'Main.Asset.AnimationList' $process.Id
    if (-not $animationList) { throw 'Floating Browse panel did not expose the animation list.' }
    $idle = $animationList.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::NameProperty,
            'idle'))
    if (-not $idle) { throw 'Floating Browse panel lost its bound animation data.' }
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
    FloatingBrowseBindings = 'passed'
}
