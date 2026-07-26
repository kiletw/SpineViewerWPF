$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\SpineViewerWPF.Cli\SpineViewerWPF.Cli.csproj'
$fixture = Join-Path $repository 'tests\fixtures\v41-minimal'
$skeleton = Join-Path $fixture 'minimal.json'
$atlas = Join-Path $fixture 'minimal.atlas'
$artifacts = Join-Path $repository 'artifacts\v3-smoke'
$first = Join-Path $artifacts 'frame-a.png'
$second = Join-Path $artifacts 'frame-b.png'

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

[pscustomobject]@{
    Inspect = 'passed'
    Render = 'passed'
    Sha256 = $firstHash
}
