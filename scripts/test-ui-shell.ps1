$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\SpineViewerWPF.Wpf\SpineViewerWPF.Wpf.csproj'
$xaml = Join-Path $repository 'src\SpineViewerWPF.Wpf\MainWindow.xaml'
$codeBehind = Join-Path $repository 'src\SpineViewerWPF.Wpf\MainWindow.xaml.cs'
$gpuViewport = Join-Path $repository 'src\SpineViewerWPF.Wpf\GpuViewport.cs'
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
    dotnet build $project -c Release --no-restore -p:BaseOutputPath="$output\bin\"
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
    'Main.Viewport.GpuSurface',
    'Main.Viewport.Backend',
    'Main.Playback.Toggle',
    'Main.Playback.Stop',
    'Main.Playback.Timeline',
    'Main.Playback.Loop',
    'Main.Viewport.Fit',
    'Main.Status.Runtime',
    'Main.Status.Renderer',
    'Main.Status.Performance',
    'Main.Status.Diagnostics',
    'Main.Inspector.Panel',
    'Main.Inspector.ModelScale',
    'Main.Inspector.PreviewFps',
    'Main.Inspector.ExportFps',
    'Main.Inspector.Background',
    'Main.Inspector.Theme',
    'Main.Viewport.Channel',
    'Main.Inspector.ProjectPath',
    'Main.Scene.LayerList',
    'Main.Scene.AddLayer',
    'Main.Scene.RemoveLayer',
    'Main.Scene.DuplicateLayer',
    'Main.Scene.LayerUp',
    'Main.Scene.LayerDown',
    'Main.Scene.AutoLayout',
    'Main.Scene.LayerX',
    'Main.Scene.LayerY',
    'Main.Scene.LayerScale',
    'Main.Scene.LayerOpacity',
    'Main.Inspector.TrackAlpha',
    'Main.Scene.LayerPma',
    'Main.Scene.SlotVisibility',
    'Main.Scene.SlotOpacity',
    'Main.Scene.SlotAttachment',
    'Main.Scene.LayerAnimation',
    'Main.Scene.LayerSkin',
    'Main.Scene.SlotSearch',
    'Main.Scene.ContextDuplicate',
    'Main.Scene.ContextReload',
    'Main.Scene.CopyAllParameters',
    'Main.Scene.CopyTransformParameters',
    'Main.Scene.CopyRenderParameters',
    'Main.Scene.CopyAppearanceParameters',
    'Main.Scene.PasteParameters',
    'Main.Properties.Tabs',
    'Main.Properties.AnimationTab',
    'Main.Properties.TransformTab',
    'Main.Properties.RenderTab',
    'Main.Properties.AppearanceTab',
    'Main.Properties.SlotsTab',
    'Main.Properties.ViewportTab',
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
    'Key="Y" Modifiers="Control" Command="{Binding RedoCommand}"',
    'Key="D" Modifiers="Control+Shift" Command="{Binding DuplicateLayerCommand}"',
    'Key="C" Modifiers="Control+Alt" Command="{Binding CopyAllLayerParametersCommand}"',
    'Key="V" Modifiers="Control+Alt" Command="{Binding PasteLayerParametersCommand}"'
)
foreach ($shortcut in $shortcuts) {
    if (-not $markup.Contains($shortcut)) { throw "Missing shortcut: $shortcut" }
}
if ($markup.Contains('Content="_Open asset"') -or
    $markup.Contains('Content="_Reload"') -or
    $markup.Contains('>_Save<') -or
    $markup.Contains('Save _As')) {
    throw 'Literal access-key underscores remain visible in command labels.'
}

$densityTokens = @(
    '<Style TargetType="ComboBox">',
    '<Style TargetType="CheckBox">',
    '<Style TargetType="Slider">',
    '<Style TargetType="ScrollBar">',
    '<RowDefinition Height="48" />',
    '<RowDefinition Height="52" />',
    '<Setter Property="Width" Value="250" />',
    'Width="310"',
    '<Style x:Key="NumericTextBox" TargetType="TextBox" BasedOn="{StaticResource {x:Type TextBox}}">',
    '<UniformGrid IsItemsHost="True" Columns="3" Rows="2"',
    '<Style TargetType="TabControl">',
    'Command="{Binding DuplicateLayerCommand}"',
    'Command="{Binding ReloadLayerCommand}"',
    'ItemsSource="{Binding AttachmentOptions}"',
    'Margin="8"'
)
foreach ($densityToken in $densityTokens) {
    if (-not $markup.Contains($densityToken)) {
        throw "Missing compact workspace token: $densityToken"
    }
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
$gpuCode = Get-Content -LiteralPath $gpuViewport -Raw
if ($gpuCode.Contains('slotSetting.Opacity') -or $gpuCode.Contains('!slot.IsVisible')) {
    throw 'GPU viewport still reapplies slot visibility or opacity after the Runtime scene pose.'
}

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class SpineViewerWpfUiMouse
{
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

    public static void Click(IntPtr window, int x, int y)
    {
        SetForegroundWindow(window);
        System.Threading.Thread.Sleep(150);
        SetCursorPos(x, y);
        mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
        mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
    }
}
'@

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
    # GLWpfControl can register a native helper window first; locate the titled
    # WPF window instead of trusting Process.MainWindowHandle.
    $mainWindow = $null
    foreach ($attempt in 1..50) {
        Start-Sleep -Milliseconds 200
        $process.Refresh()
        if ($process.HasExited) { break }
        $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.Condition]::TrueCondition)
        $candidates = $windows | Where-Object {
            $_.Current.ProcessId -eq $process.Id -and $_.Current.Name -like 'Spine Viewer*'
        }
        $mainWindow = $candidates | Where-Object { Find-AutomationId $_ 'Main.Command.Window' } | Select-Object -First 1
        if ($mainWindow) { break }
    }
    if ($process.HasExited -or -not $mainWindow) {
        throw 'WPF shell did not expose its main window.'
    }

    $windowButton = Wait-AutomationId $mainWindow 'Main.Command.Window'
    if (-not $windowButton) { throw 'WPF shell did not expose the Window command.' }
    Invoke-AutomationElement $windowButton
    $floatInspector = Wait-AutomationId ([System.Windows.Automation.AutomationElement]::RootElement) 'Main.Window.FloatInspector' $process.Id
    if (-not $floatInspector) { throw 'Window menu did not expose Float Inspector.' }
    Invoke-AutomationElement $floatInspector

    $animationList = Wait-AutomationId ([System.Windows.Automation.AutomationElement]::RootElement) 'Main.Asset.AnimationList' $process.Id
    if (-not $animationList) { throw 'Floating Inspector did not expose the animation list.' }
    $idle = $animationList.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::NameProperty,
            'idle'))
    if (-not $idle) { throw 'Floating Inspector lost its bound animation data.' }
}
finally {
    if ($process -and -not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(3000)) { Stop-Process -Id $process.Id -Force }
    }
}

$assetProcess = $null
try {
    $fixture = Join-Path $repository 'tests\fixtures\v42-minimal\minimal.json'
    $assetProcess = Start-Process -FilePath $executable -ArgumentList "--asset=$fixture" -PassThru
    $assetWindow = $null
    foreach ($attempt in 1..60) {
        Start-Sleep -Milliseconds 200
        $assetProcess.Refresh()
        if ($assetProcess.HasExited) { break }
        $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.Condition]::TrueCondition)
        $assetWindow = $windows | Where-Object {
            $_.Current.ProcessId -eq $assetProcess.Id -and $_.Current.Name -like 'Spine Viewer*'
        } | Select-Object -First 1
        if ($assetWindow -and (Find-AutomationId $assetWindow 'Main.Scene.LayerList')) { break }
    }
    if (-not $assetWindow) { throw 'Fixture launch did not expose the Spine Viewer window.' }

    $layerList = Wait-AutomationId $assetWindow 'Main.Scene.LayerList'
    if (-not $layerList) { throw 'Fixture launch did not expose the layer list.' }
    $slotsTab = Wait-AutomationId $assetWindow 'Main.Properties.SlotsTab'
    if (-not $slotsTab) { throw 'Fixture launch did not expose the Slots tab.' }
    foreach ($attempt in 1..100) {
        if ($slotsTab.Current.IsEnabled) { break }
        Start-Sleep -Milliseconds 100
    }
    if (-not $slotsTab.Current.IsEnabled) { throw 'Slots tab stayed disabled after the fixture loaded.' }
    $slotsTabPattern = $slotsTab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    ([System.Windows.Automation.SelectionItemPattern] $slotsTabPattern).Select()
    Start-Sleep -Milliseconds 500
    # WPF's TabControl peer exposes the selected tab header but not controls in
    # its DataTemplate. Anchor the physical click to that DPI-aware header
    # instead of a hard-coded window coordinate.
    $slotsTabBounds = $slotsTab.Current.BoundingRectangle
    [SpineViewerWpfUiMouse]::Click(
        [IntPtr]$assetWindow.Current.NativeWindowHandle,
        [int]($slotsTabBounds.Right - [Math]::Max(8, $slotsTabBounds.Width * 0.12)),
        [int]($slotsTabBounds.Bottom + ($slotsTabBounds.Height * 2)))
    $undoButton = Wait-AutomationId $assetWindow 'Main.Command.Undo'
    foreach ($attempt in 1..50) {
        if ($undoButton -and $undoButton.Current.IsEnabled) { break }
        Start-Sleep -Milliseconds 100
    }
    if (-not $undoButton -or -not $undoButton.Current.IsEnabled -or -not $assetWindow.Current.Name.EndsWith('*')) {
        throw 'Clicking the Slot visibility row did not dirty the project or enable Undo.'
    }
    Invoke-AutomationElement $undoButton
    $redoButton = Wait-AutomationId $assetWindow 'Main.Command.Redo'
    if (-not $redoButton -or -not $redoButton.Current.IsEnabled) { throw 'Undoing Slot visibility did not enable Redo.' }
    Invoke-AutomationElement $redoButton
    foreach ($attempt in 1..50) {
        if ($assetWindow.Current.Name.EndsWith('*')) { break }
        Start-Sleep -Milliseconds 100
    }
    if (-not $assetWindow.Current.Name.EndsWith('*')) { throw 'Redo did not reapply the Slot visibility edit.' }
    $before = $layerList.FindAll(
        [System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition).Count
    $duplicate = Wait-AutomationId $assetWindow 'Main.Scene.DuplicateLayer'
    if (-not $duplicate) { throw 'Fixture launch did not expose Duplicate layer.' }
    Invoke-AutomationElement $duplicate
    $after = $before
    foreach ($attempt in 1..50) {
        Start-Sleep -Milliseconds 200
        $after = $layerList.FindAll(
            [System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.Condition]::TrueCondition).Count
        if ($after -gt $before) { break }
    }
    if ($after -ne $before + 1) { throw "Duplicate layer action did not add exactly one layer ($before -> $after)." }
}
finally {
    if ($assetProcess -and -not $assetProcess.HasExited) {
        $assetProcess.CloseMainWindow() | Out-Null
        if (-not $assetProcess.WaitForExit(3000)) { Stop-Process -Id $assetProcess.Id -Force }
    }
}

[pscustomobject]@{
    Build = 'passed'
    AutomationIds = $automationIds.Count
    EditorShortcuts = $shortcuts.Count
    CompactWorkspaceTokens = $densityTokens.Count
    CompactWarningState = 'launched'
    FloatingInspectorBindings = 'passed'
    SlotAttachmentBinding = 'passed'
    SlotVisibilityInteraction = 'passed'
    DuplicateLayerCommand = 'passed'
}
