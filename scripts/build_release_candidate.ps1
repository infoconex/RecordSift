# Build and validate a local package only. Publication is a separate, authenticated release step.
[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $env:TEMP 'RecordSift-RC'),
    [ValidateSet('0.1.0-rc.1', '0.1.0')][string]$PackageVersion = '0.1.0-rc.1'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$destination = [IO.Path]::GetFullPath($OutputDirectory)
$stage = Join-Path $destination 'staging'
$lib = Join-Path $stage 'lib\net48'
New-Item -ItemType Directory -Force -Path $lib | Out-Null
$components = @('Domain', 'Application', 'Infrastructure.Csv', 'Infrastructure.Reporting', 'Infrastructure.Xlsx')
foreach ($component in $components) {
    $name = "RecordSift.$component"
    $build = Join-Path $root "src\$name\bin\Release"
    foreach ($extension in @('.dll', '.xml')) {
        $source = Join-Path $build "$name$extension"
        if (-not (Test-Path -LiteralPath $source)) {
            throw "Build Release first: missing $source"
        }
        Copy-Item -LiteralPath $source -Destination $lib -Force
    }
    $identity = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $lib "$name.dll"))
    $fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $lib "$name.dll")).FileVersion
    if ($identity.Version.ToString() -ne '0.1.0.0' -or $fileVersion -ne '0.1.0.0') {
        throw "$name identity/file version must be 0.1.0.0, observed $($identity.Version)/$fileVersion"
    }
}
# Stage a consumer-facing README with exactly one package-version token.
$readme = Get-Content -LiteralPath (Join-Path $root 'packaging\README.md') -Raw
if (-not $readme.Contains('{{PACKAGE_VERSION}}')) { throw 'Package README version token is missing.' }
$readme = $readme.Replace('{{PACKAGE_VERSION}}', $PackageVersion)
[IO.File]::WriteAllText((Join-Path $stage 'README.md'), $readme, [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $stage 'LICENSE') -Force
Copy-Item -LiteralPath (Join-Path $root 'THIRD-PARTY-NOTICES.md') -Destination (Join-Path $stage 'THIRD-PARTY-NOTICES.md') -Force
# Stamp the exact checked-out source commit into a temporary nuspec. Never mutate
# the version-controlled manifest; the release artifact must identify its source.
$git = (Get-Command git -ErrorAction Stop).Source
$sourceCommit = (& $git -C $root rev-parse --verify HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceCommit -cnotmatch '^[0-9a-fA-F]{40}
if ($LASTEXITCODE -ne 0) { throw "NuGet pack failed: $LASTEXITCODE" }
$package = Join-Path $destination "RecordSift.$PackageVersion.nupkg"
if (-not (Test-Path -LiteralPath $package)) { throw "Expected package missing: $package" }

Add-Type -AssemblyName System.IO.Compression
$zip = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $entries = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    foreach ($component in $components) {
        $name = "RecordSift.$component"
        foreach ($extension in @('.dll', '.xml')) {
            $expected = "lib/net48/$name$extension"
            if ($entries -cnotcontains $expected) { throw "Package missing $expected" }
        }
    }
    if ($entries -cnotcontains 'README.md') { throw 'Package missing NuGet README.md' }
    $readmeEntry = $zip.GetEntry('README.md')
    $readmeReader = [IO.StreamReader]::new($readmeEntry.Open())
    try { $packedReadme = $readmeReader.ReadToEnd() }
    finally { $readmeReader.Dispose() }
    if ($packedReadme -cne $readme -or $packedReadme.Contains('{{PACKAGE_VERSION}}')) {
        throw 'Packaged README does not match the expected consumer-facing version.'
    }
    foreach ($file in @('LICENSE', 'THIRD-PARTY-NOTICES.md')) {
        if ($entries -cnotcontains $file) { throw "Package missing $file" }
        $entry = $zip.GetEntry($file)
        $reader = [IO.StreamReader]::new($entry.Open())
        try {
            $packed = $reader.ReadToEnd()
        } finally { $reader.Dispose() }
        $source = Get-Content -LiteralPath (Join-Path $root $file) -Raw
        if ($packed -cne $source) { throw "Package notice $file differs from reviewed source." }
    }
    $nuspecEntry = $zip.GetEntry('RecordSift.nuspec')
    if ($null -eq $nuspecEntry) { throw 'NuGet manifest missing from package.' }
    $nuspecReader = [IO.StreamReader]::new($nuspecEntry.Open())
    try { [xml]$packedNuspec = $nuspecReader.ReadToEnd() }
    finally { $nuspecReader.Dispose() }
    $metadata = $packedNuspec.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
    $publishedVersion = $metadata.SelectSingleNode('*[local-name()="version"]').InnerText
    if ($publishedVersion -cne $PackageVersion) {
        throw "Packed version mismatch: requested $PackageVersion, found $publishedVersion."
    }
    # Ensure the internal 0.1.0 package is never accidentally marked as a prerelease.
    if ($PackageVersion -eq '0.1.0' -and $publishedVersion.Contains('-')) {
        throw 'Stable candidate manifest contains a prerelease suffix.'
    }
    $packedRepository = $metadata.SelectSingleNode('*[local-name()="repository"]')
    if ($null -eq $packedRepository -or $packedRepository.GetAttribute('branch') -cne 'main' -or
        $packedRepository.GetAttribute('commit') -cne $sourceCommit -or
        $packedRepository.GetAttribute('url') -cne 'https://github.com/infoconex/RecordSift.git') {
        throw 'Packaged repository metadata must identify main at the exact source commit.'
    }
    $license = $packedNuspec.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="license"]')
    if ($null -eq $license -or $license.GetAttribute('type') -ne 'expression' -or $license.InnerText -ne 'MIT') {
        throw 'Packaged .nuspec must declare the approved MIT SPDX license expression.'
    }
    $own = @($entries | Where-Object { $_ -match '^lib/net48/.*\.dll$' })
    if ($own.Count -ne $components.Count) {
        throw "Expected only five RecordSift assemblies, found $($own.Count): $($own -join ', ')"
    }
} finally {
    $zip.Dispose()
}
$hash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
Write-Host "Validated locally built $PackageVersion package $package SHA256=$hash sourceCommit=$sourceCommit (five net48 assemblies, XML documentation, MIT license and notices, NuGet dependencies)."
Write-Output $package
) {
    throw 'A verified 40-character source Git commit is required for packaging.'
}
[xml]$stagedManifest = Get-Content -LiteralPath (Join-Path $root 'packaging\RecordSift.nuspec') -Raw
$repository = $stagedManifest.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="repository"]')
if ($null -eq $repository -or $repository.GetAttribute('url') -cne 'https://github.com/infoconex/RecordSift.git') {
    throw 'Unexpected source repository metadata.'
}
$repository.SetAttribute('branch', 'main')
$repository.SetAttribute('commit', $sourceCommit)
$stagedNuspec = Join-Path $stage 'RecordSift.nuspec'
$stagedManifest.Save($stagedNuspec)
$nuget = Get-Command nuget -ErrorAction Stop
& $nuget.Source pack $stagedNuspec -BasePath $stage -OutputDirectory $destination -Version $PackageVersion -NonInteractive
if ($LASTEXITCODE -ne 0) { throw "NuGet pack failed: $LASTEXITCODE" }
$package = Join-Path $destination "RecordSift.$PackageVersion.nupkg"
if (-not (Test-Path -LiteralPath $package)) { throw "Expected package missing: $package" }

Add-Type -AssemblyName System.IO.Compression
$zip = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $entries = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    foreach ($component in $components) {
        $name = "RecordSift.$component"
        foreach ($extension in @('.dll', '.xml')) {
            $expected = "lib/net48/$name$extension"
            if ($entries -cnotcontains $expected) { throw "Package missing $expected" }
        }
    }
    if ($entries -cnotcontains 'README.md') { throw 'Package missing NuGet README.md' }
    foreach ($file in @('LICENSE', 'THIRD-PARTY-NOTICES.md')) {
        if ($entries -cnotcontains $file) { throw "Package missing $file" }
        $entry = $zip.GetEntry($file)
        $reader = [IO.StreamReader]::new($entry.Open())
        try {
            $packed = $reader.ReadToEnd()
        } finally { $reader.Dispose() }
        $source = Get-Content -LiteralPath (Join-Path $root $file) -Raw
        if ($packed -cne $source) { throw "Package notice $file differs from reviewed source." }
    }
    $nuspecEntry = $zip.GetEntry('RecordSift.nuspec')
    if ($null -eq $nuspecEntry) { throw 'NuGet manifest missing from package.' }
    $nuspecReader = [IO.StreamReader]::new($nuspecEntry.Open())
    try { [xml]$packedNuspec = $nuspecReader.ReadToEnd() }
    finally { $nuspecReader.Dispose() }
    $metadata = $packedNuspec.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
    $publishedVersion = $metadata.SelectSingleNode('*[local-name()="version"]').InnerText
    if ($publishedVersion -cne $PackageVersion) {
        throw "Packed version mismatch: requested $PackageVersion, found $publishedVersion."
    }
    # Ensure the internal 0.1.0 package is never accidentally marked as a prerelease.
    if ($PackageVersion -eq '0.1.0' -and $publishedVersion.Contains('-')) {
        throw 'Stable candidate manifest contains a prerelease suffix.'
    }
    $license = $packedNuspec.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="license"]')
    if ($null -eq $license -or $license.GetAttribute('type') -ne 'expression' -or $license.InnerText -ne 'MIT') {
        throw 'Packaged .nuspec must declare the approved MIT SPDX license expression.'
    }
    $own = @($entries | Where-Object { $_ -match '^lib/net48/.*\.dll$' })
    if ($own.Count -ne $components.Count) {
        throw "Expected only five RecordSift assemblies, found $($own.Count): $($own -join ', ')"
    }
} finally {
    $zip.Dispose()
}
$hash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
Write-Host "Validated unpublished candidate $package SHA256=$hash with five net48 assemblies, XML documentation, MIT license and notices, and NuGet dependencies."
Write-Output $package
