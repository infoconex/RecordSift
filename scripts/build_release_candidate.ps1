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
    $own = @($entries | Where-Object { $_ -match '^lib/net48/.*\.dll$' })
    if ($own.Count -ne $components.Count) {
        throw "Expected only five RecordSift assemblies, found $($own.Count): $($own -join ', ')"
    }
} finally {
    $zip.Dispose()
}
$hash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
Write-Host "Validated unpublished candidate $package SHA256=$hash with five net48 assemblies, XML documentation and NuGet dependencies."
Write-Output $package
