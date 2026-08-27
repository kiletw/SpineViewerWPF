param(
    [switch]$Offline,
    [string]$AssetRoot,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\SpineViewerWPF.Cli\SpineViewerWPF.Cli.csproj'
$assetDirectory = if ([string]::IsNullOrWhiteSpace($AssetRoot)) {
    Join-Path $repository 'artifacts\official-v43\spineboy'
} else {
    [IO.Path]::GetFullPath($AssetRoot)
}
$commit = 'de14116488688c27c01b6e2b61fe1544792af2dd'
$apiRoot = 'https://api.github.com/repos/EsotericSoftware/spine-runtimes/contents/examples/spineboy'
$assets = @(
    @{ Name = 'spineboy-pro.json'; Path = 'export/spineboy-pro.json'; BlobSha1 = '4a58fecd32a97bad83a9169056c44db3f17dba4e'; Sha256 = '24CCFFC13E334E721DFD427EE2B8AEA05C25B59167B5FB0BB0F9685E11D2A7D3' },
    @{ Name = 'spineboy-pro.skel'; Path = 'export/spineboy-pro.skel'; BlobSha1 = 'cfcbd39497d367d25ba82b6e535a61933b3cba98'; Sha256 = 'E10DE3F2473A37139C3AD1FDA14E84B29EF401475EC9FA2129E7D2A311088845' },
    @{ Name = 'spineboy.atlas'; Path = 'export/spineboy.atlas'; BlobSha1 = '33f0db102bfd643ed3f3c021b6821ac2a775ad03'; Sha256 = 'FBD452640DF513D637E0A34D21C1D3EA4EDB27F57BD1856FFD5530B16EE83D00' },
    @{ Name = 'spineboy.png'; Path = 'export/spineboy.png'; BlobSha1 = '6f76c57988f1b0575d5b12ed7b2d4f44170cb5bd'; Sha256 = 'AB874448A224F8A92188E133735062C86E5F93EB7286B0FC37C5D39864E6569F' },
    @{ Name = 'license.txt'; Path = 'license.txt'; BlobSha1 = 'c2bbb8670bfbfff48ea0d3b0a921b0ac698c79e3'; Sha256 = '08074C5F8C5F072626A0830250304FE3063D9D49F8F9B039E063BB238F2B993B' }
)
$expectedAnimations = @(
    'aim', 'death', 'hoverboard', 'idle', 'idle-turn', 'jump',
    'portal', 'run', 'run-to-idle', 'shoot', 'walk'
)

function Get-OfficialAsset([hashtable]$asset) {
    $path = Join-Path $assetDirectory $asset.Name
    $valid = Test-Path -LiteralPath $path -PathType Leaf
    if ($valid) { $valid = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -eq $asset.Sha256 }
    if (-not $valid) {
        if ($Offline) { throw "Missing or invalid cached official 4.3 asset: $path" }
        $api = '{0}/{1}?ref={2}' -f $apiRoot, $asset.Path, $commit
        $responsePath = Join-Path $env:TEMP "spineviewer-official-$([Guid]::NewGuid().ToString('N')).json"
        try {
            curl.exe -sS -L --fail --retry 2 -H 'Accept: application/vnd.github+json' -H 'User-Agent: SpineViewerWPF-compat' $api -o $responsePath
            if ($LASTEXITCODE -ne 0) { throw "Download failed: $($asset.Path)" }
            $response = Get-Content -Raw -LiteralPath $responsePath | ConvertFrom-Json
            if ($response.sha -ne $asset.BlobSha1) { throw "Official asset changed at $($asset.Path)." }
            New-Item -ItemType Directory -Path $assetDirectory -Force | Out-Null
            [IO.File]::WriteAllBytes($path, [Convert]::FromBase64String(($response.content -replace '\s', '')))
        }
        finally { Remove-Item -LiteralPath $responsePath -Force -ErrorAction SilentlyContinue }
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $asset.Sha256) {
            throw "Hash mismatch after download: $($asset.Name)"
        }
    }
    return $path
}

function Invoke-Inspect([string]$skeleton, [string]$atlas, [string]$runtime) {
    $arguments = @('--project', $project, '-c', 'Release', '--no-build', '--', 'inspect', $skeleton, '--atlas', $atlas, '--format', 'json')
    if (-not [string]::IsNullOrWhiteSpace($runtime)) { $arguments += @('--runtime', $runtime) }
    $output = & dotnet run @arguments
    if ($LASTEXITCODE -ne 0) { throw "Inspect failed: $skeleton" }
    return ($output -join "`n" | ConvertFrom-Json)
}

function Assert-Inspection($inspect, [string]$atlas, [string]$texture, [bool]$overridden) {
    $animations = @($inspect.animations | ForEach-Object { $_.name } | Sort-Object)
    $expected = @($expectedAnimations | Sort-Object)
    if (-not $inspect.success -or
        $inspect.runtime.selectedLine -ne '4.3' -or
        $inspect.runtime.detectedExportVersion -ne '4.3.75-beta' -or
        [bool]$inspect.runtime.overridden -ne $overridden -or
        (Compare-Object $animations $expected) -or
        $inspect.skins.Count -ne 1 -or $inspect.skins -notcontains 'default' -or
        $inspect.asset.textures.Count -ne 1) {
        throw 'Official 4.3 inspect contract failed.'
    }
    if ([IO.Path]::GetFullPath($inspect.asset.atlasPath) -ne [IO.Path]::GetFullPath($atlas) -or
        [IO.Path]::GetFullPath($inspect.asset.textures[0]) -ne [IO.Path]::GetFullPath($texture)) {
        throw 'Official 4.3 atlas or texture selection failed.'
    }
}

function Assert-Png([string]$path) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -le 1024) {
        throw "Rendered PNG is missing or empty: $path"
    }
    if ([BitConverter]::ToString([IO.File]::ReadAllBytes($path)[0..7]).Replace('-', '') -ne '89504E470D0A1A0A') {
        throw "Rendered output is not a PNG: $path"
    }
}

$json = Get-OfficialAsset $assets[0]
$skel = Get-OfficialAsset $assets[1]
$atlas = Get-OfficialAsset $assets[2]
$texture = Get-OfficialAsset $assets[3]
$license = Get-OfficialAsset $assets[4]
if (-not (Test-Path -LiteralPath $license -PathType Leaf)) { throw 'Official example license is missing.' }

if (-not $NoBuild) {
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
}

foreach ($skeleton in @($json, $skel)) {
    Assert-Inspection (Invoke-Inspect $skeleton $atlas '4.3') $atlas $texture $true
    Assert-Inspection (Invoke-Inspect $skeleton $atlas $null) $atlas $texture $false

    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & dotnet run --project $project -c Release --no-build -- inspect $skeleton --atlas $atlas --runtime 4.2 --format json 2>$null | Out-Null
        $mismatchExitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousErrorAction }
    if ($mismatchExitCode -eq 0) { throw "Incompatible 4.2 override unexpectedly accepted: $skeleton" }
}

$renders = @()
foreach ($skeleton in @($json, $skel)) {
    $format = if ([IO.Path]::GetExtension($skeleton) -eq '.json') { 'json' } else { 'binary' }
    $expectedHash = if ($format -eq 'json') {
        '9E1A72725C44FFF30A4BD955E72E12331F1071C03F2ED3FFF1F1DBCF5802DF12'
    } else {
        'C093E2754ABF375CE84EEA8DCE8E9693585D57F1C65B3003CE76265637824BA5'
    }
    $first = Join-Path $assetDirectory "spineboy-$format-a.png"
    $second = Join-Path $assetDirectory "spineboy-$format-b.png"
    foreach ($output in @($first, $second)) {
        & dotnet run --project $project -c Release --no-build -- render $skeleton --atlas $atlas --runtime 4.3 --animation walk --time 0.5 --width 512 --height 512 --output $output --overwrite | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Render failed: $skeleton" }
        Assert-Png $output
    }
    $firstHash = (Get-FileHash -LiteralPath $first -Algorithm SHA256).Hash
    if ($firstHash -ne (Get-FileHash -LiteralPath $second -Algorithm SHA256).Hash) {
        throw "Repeated $format render bytes differ."
    }
    if ($firstHash -ne $expectedHash) { throw "Official $format render baseline changed." }

    $portalExpectedHash = if ($format -eq 'json') {
        '1087F3FAE199833885B36B59AE7F083AF66883D97D5ECCCAABE94C768F16F0BB'
    } else {
        'E03D7A4C68FD57C11AB50DFB1FAA9C7055CB13A3AF23CD3AD494636DE7F70042'
    }
    $portalFirst = Join-Path $assetDirectory "spineboy-portal-$format-a.png"
    $portalSecond = Join-Path $assetDirectory "spineboy-portal-$format-b.png"
    foreach ($output in @($portalFirst, $portalSecond)) {
        & dotnet run --project $project -c Release --no-build -- render $skeleton --atlas $atlas --runtime 4.3 --animation portal --time 0.5 --width 512 --height 512 --output $output --overwrite | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Portal render failed: $skeleton" }
        Assert-Png $output
    }
    $portalHash = (Get-FileHash -LiteralPath $portalFirst -Algorithm SHA256).Hash
    if ($portalHash -ne (Get-FileHash -LiteralPath $portalSecond -Algorithm SHA256).Hash) {
        throw "Repeated portal $format render bytes differ."
    }
    if ($portalHash -ne $portalExpectedHash) { throw "Official portal $format render baseline changed." }
    $renders += [pscustomobject]@{ Format = $format; Sha256 = $firstHash; PortalSha256 = $portalHash }
}

[pscustomobject]@{
    Commit = $commit
    Assets = 'official spineboy 4.3.75-beta export'
    JsonInspect = 'passed (explicit, automatic, and incompatible override)'
    BinaryInspect = 'passed (explicit, automatic, and incompatible override)'
    JsonRenderSha256 = ($renders | Where-Object Format -eq 'json').Sha256
    BinaryRenderSha256 = ($renders | Where-Object Format -eq 'binary').Sha256
    PortalJsonRenderSha256 = ($renders | Where-Object Format -eq 'json').PortalSha256
    PortalBinaryRenderSha256 = ($renders | Where-Object Format -eq 'binary').PortalSha256
    Cache = $assetDirectory
}
