param(
    [switch]$Offline,
    [string]$AssetRoot
)

$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\SpineViewerWPF.Cli\SpineViewerWPF.Cli.csproj'
$assetDirectory = if ([string]::IsNullOrWhiteSpace($AssetRoot)) {
    Join-Path $repository 'artifacts\official-v38\spineboy'
} else {
    [IO.Path]::GetFullPath($AssetRoot)
}
$commit = '8b4844bd4b193ba9e54487ed397a777993cbad56'
$apiRoot = "https://api.github.com/repos/EsotericSoftware/spine-runtimes/contents/examples/spineboy"

$assets = @(
    @{ Name = 'spineboy-pro.json'; Path = 'export/spineboy-pro.json'; BlobSha1 = 'aae987d8fddd39bf37b8348db79daa028955afc1'; Sha256 = 'D794AE5ECC8F245929FEF6B6F5212CE511990FC7FFDBC3ED77422DB72AB3B14C' },
    @{ Name = 'spineboy-pro.skel'; Path = 'export/spineboy-pro.skel'; BlobSha1 = 'df612671da4d860a52638e559d3de48ca079e765'; Sha256 = 'D36F8E97A113E7EC2B1C9E20A706B0A1464914A92C057DC5541C921A3EC4123C' },
    @{ Name = 'spineboy.atlas'; Path = 'export/spineboy.atlas'; BlobSha1 = 'a2a83fea311b6e818e484cf4222e1e4e17a7894f'; Sha256 = '2A504910A91F8541D0FB1F83D80C07236EDC80888C95901953009E86E53A8147' },
    @{ Name = 'spineboy.png'; Path = 'export/spineboy.png'; BlobSha1 = '904dc4851edf4e2f7ae2aad3bb2b5dbfdab35d70'; Sha256 = '42E3D9411F5CA02146908FD22081968267419E048D66FCC5C721315146E114F6' },
    @{ Name = 'spineboy-pma.atlas'; Path = 'export/spineboy-pma.atlas'; BlobSha1 = '513c074d90de93cde4f609333a96f9200ff1dd0a'; Sha256 = '829EE567E7E1652F8B3BB1BC0AC154B2CB4A603567F6CB1793544CDB0121AE66' },
    @{ Name = 'spineboy-pma.png'; Path = 'export/spineboy-pma.png'; BlobSha1 = '067ea691ff8879437fe8f35d1ffdb79adbb15b89'; Sha256 = '329772025BC8B329FB9C94F6B0DEC5EB8BDBC0CD401291ADB24ABF327250D195' },
    @{ Name = 'license.txt'; Path = 'license.txt'; BlobSha1 = '60ff2d2a1c0f69cc0daac5ac03ff9ec744c5610b'; Sha256 = 'E10E34D4F3B31A4AD76FF08A3B2BD1581B74C1A143F096103A93EE4C16EF4857' }
)

function Get-OfficialAsset([hashtable]$asset) {
    $path = Join-Path $assetDirectory $asset.Name
    $valid = Test-Path -LiteralPath $path -PathType Leaf
    if ($valid) { $valid = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -eq $asset.Sha256 }
    if (-not $valid) {
        if ($Offline) { throw "Missing or invalid cached official asset: $path" }
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
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $asset.Sha256) { throw "Hash mismatch after download: $($asset.Name)" }
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
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -le 1024) { throw "Rendered PNG is missing or empty: $path" }
    if ([BitConverter]::ToString([IO.File]::ReadAllBytes($path)[0..7]).Replace('-', '') -ne '89504E470D0A1A0A') { throw "Rendered output is not a PNG: $path" }
}

$json = Get-OfficialAsset $assets[0]
$skel = Get-OfficialAsset $assets[1]
$atlas = Get-OfficialAsset $assets[2]
$texture = Get-OfficialAsset $assets[3]
$pmaAtlas = Get-OfficialAsset $assets[4]
$pmaTexture = Get-OfficialAsset $assets[5]
$license = Get-OfficialAsset $assets[6]
if (-not (Test-Path -LiteralPath $license)) { throw 'Official license file is missing.' }

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
    (Invoke-Inspect $json $atlas '3.8.95'),
    (Invoke-Inspect $skel $atlas '3.8.95'),
    (Invoke-Inspect $json $atlas $null)
)
foreach ($inspect in $inspections) {
    if (-not $inspect.success -or $inspect.runtime.selectedLine -ne '3.8.95' -or
        -not $inspect.runtime.detectedExportVersion.StartsWith('3.8', [StringComparison]::Ordinal) -or
        $inspect.animations.Count -ne 11 -or $inspect.skins -notcontains 'default') {
        throw 'Official 3.8 inspect contract failed.'
    }
    if ([IO.Path]::GetFullPath($inspect.asset.atlasPath) -ne [IO.Path]::GetFullPath($atlas)) { throw 'Official atlas selection failed.' }
}

$outputs = @(
    @{ Skeleton = $json; Atlas = $atlas; Pma = $false; Output = Join-Path $assetDirectory 'spineboy-json.png'; Sha256 = 'B3D532AB8CDB88016276292F19C68AE1534A709642042A6A22FB27DDF48631DA' },
    @{ Skeleton = $skel; Atlas = $atlas; Pma = $false; Output = Join-Path $assetDirectory 'spineboy-skel.png'; Sha256 = '4FFA23C7B2E31F1E4AAFEC3AE7440EEAD1F84BD365F4661C02424732535217F1' },
    @{ Skeleton = $skel; Atlas = $pmaAtlas; Pma = $true; Output = Join-Path $assetDirectory 'spineboy-pma-render.png'; Sha256 = 'C51B19EEAC11D1C969657FEFF5560751F9938972072DCF38C0C08688EC8996DF' }
)
foreach ($case in $outputs) {
    $arguments = @('--project', $project, '-c', 'Release', '--no-build', '--', 'render', $case.Skeleton, '--atlas', $case.Atlas, '--runtime', '3.8.95', '--animation', 'walk', '--time', '0.5', '--width', '512', '--height', '512', '--output', $case.Output, '--overwrite')
    if ($case.Pma) { $arguments += '--pma' }
    & dotnet run @arguments | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Render failed: $($case.Skeleton)" }
    Assert-Png $case.Output
    if ((Get-FileHash -LiteralPath $case.Output -Algorithm SHA256).Hash -ne $case.Sha256) { throw "Official render baseline changed: $($case.Output)" }
}

[pscustomobject]@{
    Commit = $commit
    Assets = 'official spineboy 3.8 export'
    JsonInspect = "passed ($($inspections[0].runtime.detectedExportVersion), $($inspections[0].animations.Count) animations)"
    BinaryInspect = "passed ($($inspections[1].runtime.detectedExportVersion), $($inspections[1].animations.Count) animations)"
    AutomaticSelection = 'passed'
    JsonRender = 'passed'
    BinaryRender = 'passed'
    PmaRender = 'passed'
    Cache = $assetDirectory
}
