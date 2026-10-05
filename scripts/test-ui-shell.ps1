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
    'Main.Menu',
    'Main.Menu.File',
    'Main.Menu.Edit',
    'Main.Menu.View',
    'Main.Menu.Playback',
    'Main.Menu.Layer',
    'Main.Menu.Export',
    'Main.Menu.Help',
    'Main.Activity.Layers',
    'Main.Activity.Properties',
    'Main.Activity.Diagnostics',
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
    'Main.Properties.LayerTab',
    'Main.Properties.SlotsTab',
    'Main.Viewport.Options',
    'Main.Status.Zoom',
    'Main.Scene.ReloadLayer',
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
    'Key="V" Modifiers="Control+Alt" Command="{Binding PasteLayerParametersCommand}"',
    'Key="I" Modifiers="Control+Shift" Command="{x:Static local:MainWindow.FloatInspectorCommand}"'
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
    '<RowDefinition Height="38" />',
    '<RowDefinition Height="44" />',
    '<RowDefinition Height="24" />',
    '<Setter Property="Width" Value="270" />',
    'Width="310"',
    '<Style x:Key="NumericTextBox" TargetType="TextBox" BasedOn="{StaticResource {x:Type TextBox}}">',
    '<UniformGrid IsItemsHost="True" Columns="3" Rows="1"',
    '<Style TargetType="TabControl">',
    'x:Name="PART_SelectedContentHost" ContentSource="SelectedContent"',
    '<Style x:Key="ToolbarButton" TargetType="Button" BasedOn="{StaticResource ButtonBase}">',
    'x:Name="BrowsePanelContent"',
    'Click="ToggleLayersPanel"',
    'Command="{Binding DuplicateLayerCommand}"',
    'Command="{Binding ReloadLayerCommand}"',
    'ItemsSource="{Binding AttachmentOptions}"',
    'ScrollViewer.HorizontalScrollBarVisibility="Disabled"',
    'Margin="8"',
    '<Menu Grid.Row="0"',
    'AutomationProperties.AutomationId="Main.Activity.Layers"',
    '<Setter Property="BorderThickness" Value="3,0,0,0" />',
    'Data="M3,3 H17 V8 H3 Z M3,12 H17 V17 H3 Z"',
    'AutomationProperties.HelpText="{DynamicResource Text.Diagnostics}"'
)
foreach ($densityToken in $densityTokens) {
    if (-not $markup.Contains($densityToken)) {
        throw "Missing compact workspace token: $densityToken"
    }
}

$code = Get-Content -LiteralPath $codeBehind -Raw
$contrastTokens = @(
    '<SolidColorBrush x:Key="AccentForegroundBrush" Color="#07120F" />',
    '<Setter Property="Foreground" Value="{DynamicResource AccentForegroundBrush}" />',
    '<Setter Property="Foreground" Value="{DynamicResource TextBrush}" />',
    'Background="{DynamicResource WindowBrush}"',
    'Fill="{DynamicResource MutedBrush}"',
    '<Setter TargetName="Thumb" Property="Fill" Value="{DynamicResource AccentForegroundBrush}" />',
    'SetBrush("AccentForegroundBrush", light ? "#FFFFFF" : "#07120F");'
)
foreach ($contrastToken in $contrastTokens) {
    if (-not ($markup.Contains($contrastToken) -or $code.Contains($contrastToken))) {
        throw "Missing interactive contrast token: $contrastToken"
    }
}
if ($markup.Contains('DynamicResource MutedTextBrush') -or
    $markup.Contains('<Setter Property="Foreground" Value="#07120F" />')) {
    throw 'A theme-specific interactive foreground remains in shared control styles.'
}

if (-not $code.Contains('DataContext = owner.DataContext;')) {
    throw 'Floating panels do not inherit the shell view model.'
}
if (-not $code.Contains('DetachPanel(BrowsePanelContent);') -or
    $code.Contains('DetachPanel(BrowsePanel);')) {
    throw 'Floating Layers still removes the activity rail from the main workspace.'
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
    $process = Start-Process -FilePath $executable -ArgumentList '--state=ReadyWithWarnings' -PassThru
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

    $windowMenu = Wait-AutomationId $mainWindow 'Main.Command.Window'
    if (-not $windowMenu) { throw 'WPF shell did not expose the Window menu.' }
    foreach ($activityId in 'Main.Activity.Layers', 'Main.Activity.Properties', 'Main.Activity.Diagnostics') {
        if (-not (Wait-AutomationId $mainWindow $activityId)) {
            throw "WPF shell did not expose activity: $activityId"
        }
    }
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

    # Keep the selected inspector tab visible across window-placement differences.
    $assetWindowPattern = $assetWindow.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
    ([System.Windows.Automation.WindowPattern] $assetWindowPattern).SetWindowVisualState(
        [System.Windows.Automation.WindowVisualState]::Maximized)
    Start-Sleep -Milliseconds 500

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

# TASK-059: verify actual setup-pose UI enablement and the visible GPU result.
$setupProcess = $null
$setupBackend = $null
try {
    $setupFixture = Join-Path $repository 'tests\fixtures\v41-setup-pose\minimal.json'
    $setupProcess = Start-Process -FilePath $executable -ArgumentList "--asset=$setupFixture" -PassThru
    $setupWindow = $null
    foreach ($attempt in 1..100) {
        Start-Sleep -Milliseconds 100
        $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Children,
            [System.Windows.Automation.Condition]::TrueCondition)
        $setupWindow = $windows | Where-Object {
            $_.Current.ProcessId -eq $setupProcess.Id -and $_.Current.Name -like 'Spine Viewer*'
        } | Select-Object -First 1
        if ($setupWindow) {
            $capture = Find-AutomationId $setupWindow 'Main.Command.Screenshot'
            if ($capture -and $capture.Current.IsEnabled) { break }
        }
    }
    if (-not $setupWindow -or -not $capture -or -not $capture.Current.IsEnabled) {
        throw 'Setup pose did not reach a usable rendered workspace.'
    }
    $windowPattern = $setupWindow.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
    ([System.Windows.Automation.WindowPattern] $windowPattern).SetWindowVisualState(
        [System.Windows.Automation.WindowVisualState]::Maximized)
    # Let maximize settle before reading layout-dependent state (same as the fixture block).
    Start-Sleep -Milliseconds 500
    # Main.Activity.Properties toggles the Inspector, so invoke it only when the
    # Properties tabs are absent. IsOffscreen can be transiently true during the
    # maximize transition; toggling then would hide a visible Inspector.
    if (-not (Wait-AutomationId $setupWindow 'Main.Properties.Tabs')) {
        Invoke-AutomationElement (Wait-AutomationId $setupWindow 'Main.Activity.Properties')
        Start-Sleep -Milliseconds 200
    }
    $animationTab = Wait-AutomationId $setupWindow 'Main.Properties.AnimationTab'
    if (-not $animationTab) { throw 'Setup pose did not expose the Animation properties tab.' }
    $selection = $animationTab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    ([System.Windows.Automation.SelectionItemPattern] $selection).Select()
    $emptyLabel = Wait-AutomationId $setupWindow 'Main.Asset.NoAnimations'
    foreach ($attempt in 1..25) {
        if ($emptyLabel -and -not $emptyLabel.Current.IsOffscreen) { break }
        Start-Sleep -Milliseconds 100
        $emptyLabel = Find-AutomationId $setupWindow 'Main.Asset.NoAnimations'
    }
    if (-not $emptyLabel -or $emptyLabel.Current.Name -ne 'No animations — setup pose' -or $emptyLabel.Current.IsOffscreen) {
        $tabsPresent = [bool](Find-AutomationId $setupWindow 'Main.Properties.Tabs')
        $animationListPresent = [bool](Find-AutomationId $setupWindow 'Main.Asset.AnimationList')
        $tabSelected = ([System.Windows.Automation.SelectionItemPattern] $selection).Current.IsSelected
        throw "Setup-pose animation state was not visible/localized: name=$($emptyLabel.Current.Name), offscreen=$($emptyLabel.Current.IsOffscreen), propertiesTabs=$tabsPresent, animationTabSelected=$tabSelected, animationListPresent=$animationListPresent."
    }
    $play = Wait-AutomationId $setupWindow 'Main.Playback.Toggle'
    $export = Wait-AutomationId $setupWindow 'Main.Command.Export'
    if ($play.Current.IsEnabled -or -not $export.Current.IsEnabled) {
        throw 'Setup-pose playback/export enablement is incorrect.'
    }
    foreach ($attempt in 1..50) {
        $backend = Wait-AutomationId $setupWindow 'Main.Status.Renderer'
        $setupBackend = $backend.Current.Name
        if ($setupBackend -eq 'GPU') { break }
        Start-Sleep -Milliseconds 100
    }
}
finally {
    if ($setupProcess -and -not $setupProcess.HasExited) {
        $setupProcess.CloseMainWindow() | Out-Null
        if (-not $setupProcess.WaitForExit(3000)) { Stop-Process -Id $setupProcess.Id -Force }
    }
}

[pscustomobject]@{
    SetupPoseUi = 'passed'
    SetupPoseBackend = $setupBackend
    Build = 'passed'
    AutomationIds = $automationIds.Count
    EditorShortcuts = $shortcuts.Count
    CompactWorkspaceTokens = $densityTokens.Count
    WarningState = 'launched'
    MenuAndActivityStructure = 'passed'
    FloatInspectorShortcut = 'present'
    SlotAttachmentBinding = 'passed'
    SlotsTabAvailability = 'passed'
    DuplicateLayerCommand = 'passed'
}
