param(
    [string] $AssetRoot = ('D:\Spine' + [char]0x6E2C + [char]0x8A66),
    [ValidateRange(2, 30)]
    [int] $SampleSeconds = 3
)

$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$cliProject = Join-Path $repository 'src\SpineViewerWPF.Cli\SpineViewerWPF.Cli.csproj'
$wpfProject = Join-Path $repository 'src\SpineViewerWPF.Wpf\SpineViewerWPF.Wpf.csproj'
$output = Join-Path $repository 'artifacts\user-playback-metrics'
$cliDll = Join-Path $output 'cli\Release\net8.0\spineviewerwpf.dll'
$wpfExecutable = Join-Path $output 'wpf\Release\net8.0-windows\SpineViewerWPF.exe'
if (-not (Test-Path -LiteralPath $AssetRoot -PathType Container)) {
    throw "Missing non-redistributable acceptance root: $AssetRoot"
}
$xiuSkeletons = @(Get-ChildItem -LiteralPath $AssetRoot -Recurse -File -Filter 'xiu.skel')
if ($xiuSkeletons.Count -ne 1) {
    throw "Expected exactly one xiu.skel below $AssetRoot; found $($xiuSkeletons.Count)."
}
$assets = @(
    [pscustomobject]@{
        Name = 'marianne'
        Skeleton = Join-Path $AssetRoot 'illust_r_2110601_marianne01_01.skel'
        Atlas = Join-Path $AssetRoot 'illust_r_2110601_marianne01_01.atlas'
        Runtime = '3.6.53'
        ExportVersion = '3.6.53'
        Animations = 5
        Skins = 3
        Textures = 5
    },
    [pscustomobject]@{
        Name = 'xiu'
        Skeleton = $xiuSkeletons[0].FullName
        Atlas = [IO.Path]::ChangeExtension($xiuSkeletons[0].FullName, '.atlas')
        Runtime = '4.1'
        ExportVersion = '4.1.14'
        Animations = 2
        Skins = 1
        Textures = 1
    }
)

foreach ($asset in $assets) {
    foreach ($path in @($asset.Skeleton, $asset.Atlas)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Missing non-redistributable acceptance asset: $path"
        }
    }
}

$buildMutex = [Threading.Mutex]::new($false, 'SpineViewerWPF.Build')
$buildHeld = $false
try {
    try { $buildHeld = $buildMutex.WaitOne([TimeSpan]::FromMinutes(5)) }
    catch [Threading.AbandonedMutexException] { $buildHeld = $true }
    if (-not $buildHeld) { throw 'Timed out waiting for the shared MSBuild lock.' }

    dotnet build $cliProject -c Release --no-restore -p:BaseOutputPath="$output\cli\"
    if ($LASTEXITCODE -ne 0) { throw 'CLI build failed.' }
    dotnet build $wpfProject -c Release --no-restore -p:BaseOutputPath="$output\wpf\"
    if ($LASTEXITCODE -ne 0) { throw 'WPF build failed.' }
}
finally {
    if ($buildHeld) { $buildMutex.ReleaseMutex() }
    $buildMutex.Dispose()
}

foreach ($asset in $assets) {
    $json = & dotnet $cliDll inspect $asset.Skeleton --atlas $asset.Atlas --runtime $asset.Runtime --format json
    if ($LASTEXITCODE -ne 0) { throw "$($asset.Name) CLI inspect failed." }
    $inspection = $json | ConvertFrom-Json
    if (-not $inspection.success -or
        $inspection.runtime.detectedExportVersion -ne $asset.ExportVersion -or
        $inspection.runtime.selectedLine -ne $asset.Runtime -or
        $inspection.animations.Count -ne $asset.Animations -or
        $inspection.skins.Count -ne $asset.Skins -or
        $inspection.asset.textures.Count -ne $asset.Textures) {
        throw "$($asset.Name) CLI inspect result did not match the verified acceptance contract."
    }
}

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Find-AutomationId {
    param(
        [System.Windows.Automation.AutomationElement] $Root,
        [string] $AutomationId
    )

    $Root.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
            $AutomationId))
}

function Wait-AutomationId {
    param(
        [System.Windows.Automation.AutomationElement] $Root,
        [string] $AutomationId,
        [int] $TimeoutSeconds = 10
    )

    foreach ($attempt in 1..($TimeoutSeconds * 10)) {
        $element = Find-AutomationId $Root $AutomationId
        if ($element) { return $element }
        Start-Sleep -Milliseconds 100
    }
    $null
}

function Measure-AssetPlayback {
    param([pscustomobject] $Asset)

    $process = $null
    try {
        $process = Start-Process -FilePath $wpfExecutable -ArgumentList "--asset=$($Asset.Skeleton)" -PassThru
        $mainWindow = $null
        foreach ($attempt in 1..100) {
            Start-Sleep -Milliseconds 100
            $process.Refresh()
            if ($process.HasExited) { break }
            $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
                [System.Windows.Automation.TreeScope]::Children,
                [System.Windows.Automation.Condition]::TrueCondition)
            $mainWindow = $windows | Where-Object {
                $_.Current.ProcessId -eq $process.Id -and $_.Current.Name -like 'Spine Viewer*'
            } | Select-Object -First 1
            if ($mainWindow) { break }
        }
        if ($process.HasExited -or -not $mainWindow) { throw 'WPF shell did not expose its main window.' }

        $performance = Wait-AutomationId $mainWindow 'Main.Status.Performance' 20
        if (-not $performance) {
            throw 'Playback metrics are unavailable: missing AutomationId Main.Status.Performance.'
        }
        $renderer = Wait-AutomationId $mainWindow 'Main.Status.Renderer'
        if (-not $renderer) { throw 'Renderer status is unavailable.' }

        $deadline = [DateTime]::UtcNow.AddSeconds(60)
        do {
            Start-Sleep -Milliseconds 250
            $firstSample = $performance.Current.Name
        } while ($firstSample -notmatch '(?i)\bfps\b' -and [DateTime]::UtcNow -lt $deadline)
        if ($firstSample -notmatch '(?i)\bfps\b') {
            throw "Playback did not start for $($Asset.Name): '$firstSample'"
        }

        $samples = @()
        foreach ($second in 1..$SampleSeconds) {
            Start-Sleep -Seconds 1
            $text = $performance.Current.Name
            if ($text -notmatch '(?i)\bfps\b' -or
                $text -notmatch '(?i)\bms\b' -or
                $text -notmatch '(?i)(coalesced|dropped)') {
                throw "Playback metrics are incomplete for $($Asset.Name): '$text'"
            }
            $samples += $text
        }

        $rendererText = $renderer.Current.Name
        if ($rendererText -notmatch '(?i)(GPU|CPU fallback)') {
            throw "Renderer status is not explicit for $($Asset.Name): '$rendererText'"
        }
        $viewportBackend = Wait-AutomationId $mainWindow 'Main.Viewport.Backend'
        if (-not $viewportBackend -or $viewportBackend.Current.Name -ne $rendererText) {
            throw "Renderer status disagrees with the viewport for $($Asset.Name)."
        }
        $diagnostic = $null
        if ($rendererText -match '(?i)CPU fallback') {
            $diagnosticButton = Wait-AutomationId $mainWindow 'Main.Status.Diagnostics'
            $invokePattern = $diagnosticButton.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
            ([System.Windows.Automation.InvokePattern] $invokePattern).Invoke()
            $diagnostic = (Wait-AutomationId $mainWindow 'Main.Diagnostics.Summary').Current.Name
        }

        [pscustomobject]@{
            Asset = $Asset.Name
            ExportVersion = $Asset.ExportVersion
            Runtime = $Asset.Runtime
            Renderer = $rendererText
            Diagnostic = $diagnostic
            Samples = $samples
        }
    }
    finally {
        if ($process -and -not $process.HasExited) {
            $process.CloseMainWindow() | Out-Null
            if (-not $process.WaitForExit(3000)) { Stop-Process -Id $process.Id -Force }
        }
    }
}

$playback = @($assets | ForEach-Object { Measure-AssetPlayback $_ })
[pscustomobject]@{
    Inspect = 'passed'
    AssetRoot = $AssetRoot
    SampleSeconds = $SampleSeconds
    Playback = $playback
}
