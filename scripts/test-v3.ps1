$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\SpineViewerWPF.Cli\SpineViewerWPF.Cli.csproj'
$fixture = Join-Path $repository 'tests\fixtures\v41-minimal'
$v40Fixture = Join-Path $repository 'tests\fixtures\v40-minimal'
$v42Fixture = Join-Path $repository 'tests\fixtures\v42-minimal'
$v43Fixture = Join-Path $repository 'tests\fixtures\v43-minimal'
$skeleton = Join-Path $fixture 'minimal.json'
$atlas = Join-Path $fixture 'minimal.atlas'
$v40Skeleton = Join-Path $v40Fixture 'minimal.json'
$v40Atlas = Join-Path $v40Fixture 'minimal.atlas'
$v42Skeleton = Join-Path $v42Fixture 'minimal.json'
$v42Atlas = Join-Path $v42Fixture 'minimal.atlas'
$v43Skeleton = Join-Path $v43Fixture 'minimal.json'
$v43Atlas = Join-Path $v43Fixture 'minimal.atlas'
$artifacts = Join-Path $repository 'artifacts\v3-smoke'
$first = Join-Path $artifacts 'frame-a.png'
$second = Join-Path $artifacts 'frame-b.png'
$pma = Join-Path $artifacts 'pma-frame.png'
$v40First = Join-Path $artifacts 'v40-frame-a.png'
$v40Second = Join-Path $artifacts 'v40-frame-b.png'
$v42First = Join-Path $artifacts 'v42-frame-a.png'
$v42Second = Join-Path $artifacts 'v42-frame-b.png'
$v43First = Join-Path $artifacts 'v43-frame-a.png'
$v43Second = Join-Path $artifacts 'v43-frame-b.png'

$buildMutex = [Threading.Mutex]::new($false, 'SpineViewerWPF.Build')
$buildHeld = $false
try {
    try { $buildHeld = $buildMutex.WaitOne([TimeSpan]::FromMinutes(5)) }
    catch [Threading.AbandonedMutexException] { $buildHeld = $true }
    if (-not $buildHeld) { throw 'Timed out waiting for the shared MSBuild lock.' }
    dotnet build $project -c Release
}
finally {
    if ($buildHeld) { $buildMutex.ReleaseMutex() }
    $buildMutex.Dispose()
}
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

New-Item -ItemType Directory -Path $artifacts -Force | Out-Null

$inspectJson = & dotnet run --project $project -c Release --no-build -- inspect $skeleton --atlas $atlas --runtime 4.1 --format json
if ($LASTEXITCODE -ne 0) { throw 'Inspect failed.' }
$inspect = $inspectJson | ConvertFrom-Json
foreach ($property in @('success', 'asset', 'runtime', 'animations', 'skins', 'diagnostics')) {
    if ($property -notin $inspect.PSObject.Properties.Name) { throw "Inspect schema field is missing: $property" }
}
if (-not $inspect.success) { throw 'Inspect result was not successful.' }
if ($inspect.runtime.selectedLine -ne '4.1') { throw 'Unexpected Runtime line.' }
if ($inspect.runtime.detectedExportVersion -ne '4.1.00') { throw 'Unexpected export version.' }
if ($inspect.animations.Count -ne 1 -or $inspect.animations[0].name -ne 'move' -or $inspect.animations[0].durationSeconds -ne 1) {
    throw 'Unexpected animation metadata.'
}
if ($inspect.skins.Count -ne 1 -or $inspect.skins[0] -ne 'default') { throw 'Unexpected skin metadata.' }

foreach ($output in @($first, $second)) {
    & dotnet run --project $project -c Release --no-build -- render $skeleton --atlas $atlas --runtime 4.1 --animation move --time 0.5 --width 64 --height 64 --output $output --overwrite | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Render failed: $output" }
}

$firstHash = (Get-FileHash -LiteralPath $first -Algorithm SHA256).Hash
$secondHash = (Get-FileHash -LiteralPath $second -Algorithm SHA256).Hash
if ($firstHash -ne $secondHash) { throw 'Rendered PNG is not deterministic.' }
if ($firstHash -ne '7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E') {
    throw 'Rendered PNG does not match the recorded baseline.'
}
if ([BitConverter]::ToString([IO.File]::ReadAllBytes($first)[0..7]).Replace('-', '') -ne '89504E470D0A1A0A') {
    throw 'Rendered output is not a PNG.'
}

& dotnet run --project $project -c Release --no-build -- render $skeleton --atlas $atlas --runtime 4.1 --animation move --time 0.5 --width 64 --height 64 --output $pma --overwrite --pma | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'PMA render failed.' }
if ((Get-FileHash -LiteralPath $pma -Algorithm SHA256).Hash -ne $firstHash) { throw 'Opaque PMA baseline changed.' }

$v40InspectJson = & dotnet run --project $project -c Release --no-build -- inspect $v40Skeleton --atlas $v40Atlas --runtime 4.0 --format json
if ($LASTEXITCODE -ne 0) { throw '4.0 inspect failed.' }
$v40Inspect = $v40InspectJson | ConvertFrom-Json
if (-not $v40Inspect.success -or $v40Inspect.runtime.selectedLine -ne '4.0' -or $v40Inspect.runtime.detectedExportVersion -ne '4.0.64') {
    throw '4.0 Runtime selection contract failed.'
}
foreach ($output in @($v40First, $v40Second)) {
    & dotnet run --project $project -c Release --no-build -- render $v40Skeleton --atlas $v40Atlas --runtime 4.0.64 --animation move --time 0.5 --width 64 --height 64 --output $output --overwrite | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "4.0 render failed: $output" }
}
$v40Hash = (Get-FileHash -LiteralPath $v40First -Algorithm SHA256).Hash
if ($v40Hash -ne (Get-FileHash -LiteralPath $v40Second -Algorithm SHA256).Hash) { throw '4.0 rendered PNG is not deterministic.' }
if ($v40Hash -ne 'E16719DD53FB8CACED7D8C28E4BDD50DBEB8812483B041EC640913C13C09D28B') {
    throw '4.0 rendered PNG does not match the recorded baseline.'
}

$v42InspectJson = & dotnet run --project $project -c Release --no-build -- inspect $v42Skeleton --atlas $v42Atlas --runtime 4.2 --format json
if ($LASTEXITCODE -ne 0) { throw '4.2 inspect failed.' }
$v42Inspect = $v42InspectJson | ConvertFrom-Json
if (-not $v42Inspect.success -or $v42Inspect.runtime.selectedLine -ne '4.2' -or $v42Inspect.runtime.detectedExportVersion -ne '4.2.00') {
    throw '4.2 Runtime selection contract failed.'
}
foreach ($output in @($v42First, $v42Second)) {
    & dotnet run --project $project -c Release --no-build -- render $v42Skeleton --atlas $v42Atlas --runtime 4.2 --animation move --time 0.5 --width 64 --height 64 --output $output --overwrite | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "4.2 render failed: $output" }
}
$v42Hash = (Get-FileHash -LiteralPath $v42First -Algorithm SHA256).Hash
if ($v42Hash -ne (Get-FileHash -LiteralPath $v42Second -Algorithm SHA256).Hash) { throw '4.2 rendered PNG is not deterministic.' }
if ($v42Hash -ne '9F94B3309ABFC350372E67ED894971BF24A230C9087BF19C4AC628720579C7A9') {
    throw '4.2 rendered PNG does not match the recorded baseline.'
}

$v43InspectJson = & dotnet run --project $project -c Release --no-build -- inspect $v43Skeleton --atlas $v43Atlas --runtime 4.3 --format json
if ($LASTEXITCODE -ne 0) { throw '4.3 inspect failed.' }
$v43Inspect = $v43InspectJson | ConvertFrom-Json
if (-not $v43Inspect.success -or $v43Inspect.runtime.selectedLine -ne '4.3' -or $v43Inspect.runtime.detectedExportVersion -ne '4.3.00') {
    throw '4.3 Runtime selection contract failed.'
}
foreach ($output in @($v43First, $v43Second)) {
    & dotnet run --project $project -c Release --no-build -- render $v43Skeleton --atlas $v43Atlas --runtime 4.3 --animation move --time 0.5 --width 64 --height 64 --output $output --overwrite | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "4.3 render failed: $output" }
}
$v43Hash = (Get-FileHash -LiteralPath $v43First -Algorithm SHA256).Hash
if ($v43Hash -ne (Get-FileHash -LiteralPath $v43Second -Algorithm SHA256).Hash) { throw '4.3 rendered PNG is not deterministic.' }
if ($v43Hash -ne '7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E') {
    throw '4.3 rendered PNG does not match the recorded baseline.'
}

$historical = @(
    @('v21_08-minimal', '2.1.08'), @('v21_25-minimal', '2.1.25'),
    @('v31_07-minimal', '3.1.07'), @('v32-minimal', '3.2.xx'),
    @('v34_02-minimal', '3.4.02'), @('v35_51-minimal', '3.5.51'),
    @('v36_32-minimal', '3.6.32'), @('v36_39-minimal', '3.6.39'),
    @('v36_53-minimal', '3.6.53'), @('v37_94-minimal', '3.7.94'),
    @('v38_95-minimal', '3.8.95'), @('v40_31-minimal', '4.0.31')
)
foreach ($entry in $historical) {
    $name = $entry[0]
    $runtime = $entry[1]
    $historicalSkeleton = Join-Path $repository "tests\fixtures\$name\minimal.json"
    $explicit = & dotnet run --project $project -c Release --no-build -- inspect $historicalSkeleton --runtime $runtime --format json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $explicit.runtime.selectedLine -ne $runtime) { throw "$runtime explicit inspect failed." }
    $automatic = & dotnet run --project $project -c Release --no-build -- inspect $historicalSkeleton --format json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $automatic.runtime.selectedLine -ne $runtime) { throw "$runtime automatic inspect failed." }
    $historicalFirst = Join-Path $artifacts "$name-a.png"
    $historicalSecond = Join-Path $artifacts "$name-b.png"
    foreach ($output in @($historicalFirst, $historicalSecond)) {
        & dotnet run --project $project -c Release --no-build -- render $historicalSkeleton --runtime $runtime --animation move --time 0.5 --width 64 --height 64 --output $output --overwrite | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "$runtime render failed." }
    }
    if ((Get-FileHash -LiteralPath $historicalFirst -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $historicalSecond -Algorithm SHA256).Hash) {
        throw "$runtime rendered PNG is not deterministic."
    }
}

# No animation argument is needed only when inspection finds no animations.
$setupSkeleton = Join-Path $repository 'tests\fixtures\v41-setup-pose\minimal.json'
$setupInspection = & dotnet run --project $project -c Release --no-build -- inspect $setupSkeleton --format json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or -not $setupInspection.success -or $setupInspection.animations.Count -ne 0) {
    throw 'Setup-pose inspect failed.'
}
$setupOutputs = @((Join-Path $artifacts 'setup-default.png'), (Join-Path $artifacts 'setup-faded.png'))
foreach ($index in 0..1) {
    $skin = @('default', 'faded')[$index]
    & dotnet run --project $project -c Release --no-build -- render $setupSkeleton --time 0 --skin $skin --width 64 --height 64 --output $setupOutputs[$index] --overwrite | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Setup-pose CLI render failed.' }
}
if ((Get-FileHash $setupOutputs[0]).Hash -eq (Get-FileHash $setupOutputs[1]).Hash) {
    throw 'Setup-pose CLI skin selection did not affect output.'
}
$previousErrorPolicy = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & dotnet run --project $project -c Release --no-build -- render $skeleton --time 0 --output (Join-Path $artifacts 'missing-animation.png') --overwrite 2>$null | Out-Null
    $missingAnimationExit = $LASTEXITCODE
}
finally { $ErrorActionPreference = $previousErrorPolicy }
if ($missingAnimationExit -ne 2) { throw 'Animated CLI render no longer requires an animation.' }

[pscustomobject]@{
    SetupPose = 'passed'
    Inspect = 'passed'
    Render = 'passed'
    Sha256 = $firstHash
    Pma = 'passed'
    Runtime40 = 'passed'
    Runtime42 = 'passed'
    Runtime43 = 'passed'
    HistoricalRuntimes = $historical.Count
}
