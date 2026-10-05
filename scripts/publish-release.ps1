param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$RuntimeIdentifier = 'win-x64',
    [string]$OutputRoot
)

# TASK-069: builds the portable release packages used by .github/workflows/release.yml.
$ErrorActionPreference = 'Stop'

if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z][0-9A-Za-z.-]*)?$') {
    throw "Version must be SemVer without a leading 'v' (for example 3.0.0-alpha.1): $Version"
}

$repository = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) { $OutputRoot = Join-Path $repository 'artifacts\release' }
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
$staging = Join-Path $OutputRoot 'staging'

$packages = @(
    [pscustomobject]@{
        Name = "SpineViewerWPF-$Version-$RuntimeIdentifier"
        Project = Join-Path $repository 'src\SpineViewerWPF.Wpf\SpineViewerWPF.Wpf.csproj'
        Executable = 'SpineViewerWPF.exe'
    },
    [pscustomobject]@{
        Name = "SpineViewerWPF-CLI-$Version-$RuntimeIdentifier"
        Project = Join-Path $repository 'src\SpineViewerWPF.Cli\SpineViewerWPF.Cli.csproj'
        Executable = 'spineviewerwpf.exe'
    }
)

if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging -Force | Out-Null
foreach ($package in $packages) {
    $zip = Join-Path $OutputRoot "$($package.Name).zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
}

# Spine Runtime licenses require each license and copyright notice to ship with
# redistributed binaries. Adapters without an upstream LICENSE file carry it in
# their source headers.
function Copy-SpineLicenses([string]$Destination) {
    $licenseDirectory = Join-Path $Destination 'licenses\spine-runtimes'
    New-Item -ItemType Directory -Path $licenseDirectory -Force | Out-Null
    foreach ($runtime in Get-ChildItem (Join-Path $repository 'runtimes') -Directory -Filter 'SpineRuntime.V*') {
        $target = Join-Path $licenseDirectory "$($runtime.Name).txt"
        $licenseFile = Join-Path $runtime.FullName 'LICENSE'
        if (Test-Path $licenseFile) {
            Copy-Item $licenseFile $target
            continue
        }
        $source = Get-ChildItem $runtime.FullName -Recurse -File -Filter 'Animation.cs' |
            Where-Object { $_.FullName -notmatch '\\(bin|obj|artifacts)\\' } |
            Select-Object -First 1
        if ($null -eq $source) { throw "No license source found for $($runtime.Name)." }
        $header = [Collections.Generic.List[string]]::new()
        foreach ($line in Get-Content $source.FullName) {
            if ($header.Count -eq 0 -and $line -notmatch '^\s*/\*') { continue }
            $header.Add(($line -replace '^\s*/\*+\s?', '' -replace '^\s*\*+/\s*$', '' -replace '^\s*\*\s?', '').TrimEnd())
            if ($line -match '\*/') { break }
        }
        if (-not ($header -match 'Spine Runtimes')) { throw "Unexpected license header in $($source.FullName)." }
        Set-Content -Path $target -Value ($header -join "`r`n").Trim() -Encoding UTF8
    }
}

$buildMutex = [Threading.Mutex]::new($false, 'SpineViewerWPF.Build')
$buildHeld = $false
try {
    try { $buildHeld = $buildMutex.WaitOne([TimeSpan]::FromMinutes(5)) }
    catch [Threading.AbandonedMutexException] { $buildHeld = $true }
    if (-not $buildHeld) { throw 'Timed out waiting for the shared MSBuild lock.' }

    foreach ($package in $packages) {
        $directory = Join-Path $staging $package.Name
        dotnet publish $package.Project -c Release -r $RuntimeIdentifier --self-contained true `
            -p:Version=$Version -o $directory
        if ($LASTEXITCODE -ne 0) { throw "Publish failed: $($package.Project)" }
        if (-not (Test-Path (Join-Path $directory $package.Executable))) {
            throw "Published package is missing $($package.Executable)."
        }
    }
}
finally {
    if ($buildHeld) { $buildMutex.ReleaseMutex() }
    $buildMutex.Dispose()
}

Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
# Windows PowerShell's ZipFile.CreateFromDirectory writes '\' separators; write '/' entries explicitly.
function New-ZipFromDirectory([string]$Directory, [string]$Zip) {
    $root = Split-Path -Leaf $Directory
    $archive = [IO.Compression.ZipFile]::Open($Zip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem $Directory -Recurse -File) {
            $relative = $file.FullName.Substring($Directory.Length).TrimStart('\').Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive, $file.FullName, "$root/$relative", [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally { $archive.Dispose() }
}

$checksums = [Collections.Generic.List[string]]::new()
foreach ($package in $packages) {
    $directory = Join-Path $staging $package.Name
    foreach ($document in @('README.md', 'README_zhTW.md', 'THIRD-PARTY-NOTICES.md')) {
        Copy-Item (Join-Path $repository $document) $directory
    }
    Copy-SpineLicenses $directory

    $zip = Join-Path $OutputRoot "$($package.Name).zip"
    New-ZipFromDirectory $directory $zip
    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    $checksums.Add("$hash  $($package.Name).zip")
}

$checksumFile = Join-Path $OutputRoot 'SHA256SUMS.txt'
[IO.File]::WriteAllText($checksumFile, (($checksums -join "`n") + "`n"))

[pscustomobject]@{
    Version = $Version
    Runtime = $RuntimeIdentifier
    Packages = @($packages | ForEach-Object { Join-Path $OutputRoot "$($_.Name).zip" })
    Checksums = $checksumFile
}
