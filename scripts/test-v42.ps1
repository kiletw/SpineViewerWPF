param(
    [switch]$Offline,
    [string]$AssetRoot
)

$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\SpineViewerWPF.Cli\SpineViewerWPF.Cli.csproj'
$assetDirectory = if ([string]::IsNullOrWhiteSpace($AssetRoot)) {
    Join-Path $repository 'artifacts\official-v42\spineboy'
} else {
    [IO.Path]::GetFullPath($AssetRoot)
}
$commit = 'b81e5a58ed38704aee4f866f0e0ac672623ce914'
$apiRoot = 'https://api.github.com/repos/EsotericSoftware/spine-runtimes/contents/examples/spineboy'
$assets = @(
    @{ Name = 'spineboy-pro.json'; Path = 'export/spineboy-pro.json'; BlobSha1 = 'f3ba20944ba6025e59b8a5dc00ad22b6cd58c73f'; Sha256 = '488FACE411DDFAD77EE3239B29431DEB574D73F4FECA7ECA541452AD24BB6BFC' },
    @{ Name = 'spineboy-pro.skel'; Path = 'export/spineboy-pro.skel'; BlobSha1 = '09e564b7edc42fe37be66a4a0391862071ed948a'; Sha256 = '1345DE0E8E4729559F39060ED614429B21AB39BFC09ED2E1F58FCF054A8B5925' },
    @{ Name = 'spineboy.atlas'; Path = 'export/spineboy.atlas'; BlobSha1 = 'eca542b711e7e140de85efac07b1bccbda07c5f5'; Sha256 = 'FC90C2A604C14BB3AE1A81F9C950660863A41654CB29C869423A86E127FA75AE' },
    @{ Name = 'spineboy.png'; Path = 'export/spineboy.png'; BlobSha1 = '0ea9737f30707915e0bf495cdf987c6511dc0500'; Sha256 = '071A6ADEC73378EEBD802A047D274DEEFE11ABEE3CE689A1E71ACB4CB41E331E' },
    @{ Name = 'license.txt'; Path = 'license.txt'; BlobSha1 = 'c2bbb8670bfbfff48ea0d3b0a921b0ac698c79e3'; Sha256 = '08074C5F8C5F072626A0830250304FE3063D9D49F8F9B039E063BB238F2B993B' }
)

function Get-OfficialAsset([hashtable]$asset) {
    $path = Join-Path $assetDirectory $asset.Name
    $valid = Test-Path -LiteralPath $path -PathType Leaf
    if ($valid) { $valid = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -eq $asset.Sha256 }
    if (-not $valid) {
        if ($Offline) { throw "Missing or invalid cached official 4.2 asset: $path" }
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
if (-not (Test-Path -LiteralPath $texture) -or -not (Test-Path -LiteralPath $license)) {
    throw 'Official texture or license file is missing.'
}

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

$inspections = @(
    (Invoke-Inspect $json $atlas '4.2'),
    (Invoke-Inspect $skel $atlas '4.2'),
    (Invoke-Inspect $json $atlas $null),
    (Invoke-Inspect $skel $atlas $null)
)
foreach ($inspect in $inspections) {
    if (-not $inspect.success -or $inspect.runtime.selectedLine -ne '4.2' -or
        $inspect.runtime.detectedExportVersion -ne '4.2.22' -or
        $inspect.animations.Count -ne 11 -or $inspect.skins.Count -ne 1 -or
        $inspect.skins -notcontains 'default' -or $inspect.asset.textures.Count -ne 1) {
        throw 'Official 4.2 inspect contract failed.'
    }
    if ([IO.Path]::GetFullPath($inspect.asset.atlasPath) -ne [IO.Path]::GetFullPath($atlas)) {
        throw 'Official atlas selection failed.'
    }
}

$renders = @()
foreach ($skeleton in @($json, $skel)) {
    $format = if ([IO.Path]::GetExtension($skeleton) -eq '.json') { 'json' } else { 'binary' }
    $expectedHash = if ($format -eq 'json') {
        '6065E7919809405F237A29A77871A13402E3EF8120176689D5FA821690AE919A'
    } else {
        'DD3A2C6E92B0B89A999CE907B12C03D2236AA7FC14612A2255EDD1F217B1FB67'
    }
    $first = Join-Path $assetDirectory "spineboy-$format-a.png"
    $second = Join-Path $assetDirectory "spineboy-$format-b.png"
    foreach ($output in @($first, $second)) {
        & dotnet run --project $project -c Release --no-build -- render $skeleton --atlas $atlas --runtime 4.2 --animation walk --time 0.5 --width 512 --height 512 --output $output --overwrite | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Render failed: $skeleton" }
        Assert-Png $output
    }
    $firstHash = (Get-FileHash -LiteralPath $first -Algorithm SHA256).Hash
    $secondHash = (Get-FileHash -LiteralPath $second -Algorithm SHA256).Hash
    if ($firstHash -ne $secondHash) { throw "Repeated $format render bytes differ." }
    if ($firstHash -ne $expectedHash) { throw "Official $format render baseline changed." }
    $renders += [pscustomobject]@{ Format = $format; Sha256 = $firstHash }
}

[pscustomobject]@{
    Commit = $commit
    Assets = 'official spineboy 4.2.22 export'
    JsonInspect = 'passed (explicit and automatic)'
    BinaryInspect = 'passed (explicit and automatic)'
    JsonRenderSha256 = ($renders | Where-Object Format -eq 'json').Sha256
    BinaryRenderSha256 = ($renders | Where-Object Format -eq 'binary').Sha256
    Cache = $assetDirectory
}
