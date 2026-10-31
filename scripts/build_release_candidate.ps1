# Internal candidate build only. Never push this nupkg to a feed or GitHub release.
[CmdletBinding()]
param([string]$OutputDirectory = (Join-Path $env:TEMP 'RecordSift-RC'))

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
Copy-Item -LiteralPath (Join-Path $root 'packaging\README.md') -Destination (Join-Path $stage 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $stage 'LICENSE') -Force
Copy-Item -LiteralPath (Join-Path $root 'THIRD-PARTY-NOTICES.md') -Destination (Join-Path $stage 'THIRD-PARTY-NOTICES.md') -Force
$nuget = Get-Command nuget -ErrorAction Stop
& $nuget.Source pack (Join-Path $root 'packaging\RecordSift.nuspec') -BasePath $stage -OutputDirectory $destination -NonInteractive
if ($LASTEXITCODE -ne 0) { throw "NuGet pack failed: $LASTEXITCODE" }
$package = Join-Path $destination 'RecordSift.0.1.0-rc.1.nupkg'
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
