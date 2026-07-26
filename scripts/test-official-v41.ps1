param(
    [switch]$Offline,
    [string]$AssetRoot
)

$ErrorActionPreference = 'Stop'

$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src\SpineViewerWPF.Cli\SpineViewerWPF.Cli.csproj'
$assetDirectory = if ([string]::IsNullOrWhiteSpace($AssetRoot)) {
    Join-Path $repository 'artifacts\official-v41\spineboy'
} else {
    [IO.Path]::GetFullPath($AssetRoot)
}
$commit = 'ab28b77c70e3aa766be5bdb759d7aedac9fd0bde'
$apiRoot = "https://api.github.com/repos/EsotericSoftware/spine-runtimes/contents/examples/spineboy"

$assets = @(
    @{ Name = 'spineboy-pro.json'; Path = 'export/spineboy-pro.json'; BlobSha1 = 'b4dd8b40f02eeabbc2274bc40353c2882d79f28e'; Sha256 = '719AD3DA48EF93C1BBE3927EBB2FD1E861B409478CCB552690C2C8142E154442' },
    @{ Name = 'spineboy-pro.skel'; Path = 'export/spineboy-pro.skel'; BlobSha1 = '199839b499688be6c2aed175b683a2ee93f1a64e'; Sha256 = '5ABFC213ADFB64380C13D7E60B03F306E7D88C846B930C6DB7DCFA4FB376A266' },
    @{ Name = 'spineboy.atlas'; Path = 'export/spineboy.atlas'; BlobSha1 = 'b07ccc3bf7be62f3af41c77ad027971433566c36'; Sha256 = 'FDC4BBC4BE5F1A638FA0FF384271007B1F2DF72855DD80B4B239BCFE8A4AC31E' },
    @{ Name = 'spineboy.png'; Path = 'export/spineboy.png'; BlobSha1 = 'd1c3ac1be1223bbd5db68f0d66013461a0af8c4c'; Sha256 = '1932938CA3F4B97F59F2FE6AE8A84B3F27F420C038EC9687B8D2AB60281E9602' },
    @{ Name = 'license.txt'; Path = 'license.txt'; BlobSha1 = '60ff2d2a1c0f69cc0daac5ac03ff9ec744c5610b'; Sha256 = 'E10E34D4F3B31A4AD76FF08A3B2BD1581B74C1A143F096103A93EE4C16EF4857' }
)

function Get-OfficialAsset([hashtable]$asset) {
    $path = Join-Path $assetDirectory $asset.Name
    $valid = Test-Path -LiteralPath $path -PathType Leaf
    if ($valid) {
        $valid = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -eq $asset.Sha256
    }

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
        finally {
            Remove-Item -LiteralPath $responsePath -Force -ErrorAction SilentlyContinue
        }
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $asset.Sha256) {
            throw "Hash mismatch after download: $($asset.Name)"
        }
    }

    return $path
}

function Invoke-Inspect([string]$skeleton, [string]$atlas) {
    $arguments = @('--project', $project, '-c', 'Release', '--no-build', '--', 'inspect', $skeleton)
    if (-not [string]::IsNullOrWhiteSpace($atlas)) { $arguments += @('--atlas', $atlas) }
    $arguments += @('--runtime', '4.1', '--format', 'json')
    $output = & dotnet run @arguments
    if ($LASTEXITCODE -ne 0) { throw "Inspect failed: $skeleton" }
    return ($output -join "`n" | ConvertFrom-Json)
}

function Assert-Png([string]$path) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -le 1024) {
        throw "Rendered PNG is missing or empty: $path"
    }
    $signature = [BitConverter]::ToString([IO.File]::ReadAllBytes($path)[0..7]).Replace('-', '')
    if ($signature -ne '89504E470D0A1A0A') { throw "Rendered output is not a PNG: $path" }
}

$json = Get-OfficialAsset $assets[0]
$skel = Get-OfficialAsset $assets[1]
$atlas = Get-OfficialAsset $assets[2]
$png = Get-OfficialAsset $assets[3]
$license = Get-OfficialAsset $assets[4]
if (-not (Test-Path -LiteralPath $license)) { throw 'Official license file is missing.' }

dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$jsonInspect = Invoke-Inspect $json $null
$skelInspect = Invoke-Inspect $skel $null
foreach ($inspect in @($jsonInspect, $skelInspect)) {
    if (-not $inspect.success -or $inspect.runtime.selectedLine -ne '4.1' -or
        -not $inspect.runtime.detectedExportVersion.StartsWith('4.1', [StringComparison]::Ordinal) -or
        $inspect.animations.Count -lt 1 -or $inspect.skins -notcontains 'default') {
        throw 'Official 4.1 inspect contract failed.'
    }
    if ([IO.Path]::GetFullPath($inspect.asset.atlasPath) -ne [IO.Path]::GetFullPath($atlas)) {
        throw 'Official atlas discovery selected the wrong file.'
    }
}

$outputs = @(
    @{ Skeleton = $json; Output = Join-Path $assetDirectory 'spineboy-json.png'; Sha256 = '643A19AF580DBA2C79555B59D1EE4ECC73D9CFAECF5477C5FA94DC4BB7EE3731' },
    @{ Skeleton = $skel; Output = Join-Path $assetDirectory 'spineboy-skel.png'; Sha256 = '6BB0635EFEACE5F2B99B8FF4031E4203D2276D9E2975F8955E8BC55D6695B406' }
)
foreach ($case in $outputs) {
    & dotnet run --project $project -c Release --no-build -- render $case.Skeleton --atlas $atlas --runtime 4.1 --animation walk --time 0.5 --width 512 --height 512 --output $case.Output --overwrite | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Render failed: $($case.Skeleton)" }
    Assert-Png $case.Output
    if ((Get-FileHash -LiteralPath $case.Output -Algorithm SHA256).Hash -ne $case.Sha256) {
        throw "Official render baseline changed: $($case.Skeleton)"
    }
}

[pscustomobject]@{
    Commit = $commit
    Assets = 'official spineboy export'
    JsonInspect = "passed ($($jsonInspect.runtime.detectedExportVersion), $($jsonInspect.animations.Count) animations)"
    BinaryInspect = "passed ($($skelInspect.runtime.detectedExportVersion), $($skelInspect.animations.Count) animations)"
    JsonRender = 'passed'
    BinaryRender = 'passed'
    Cache = $assetDirectory
}
