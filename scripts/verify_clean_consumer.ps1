# Run a standalone C# 7.3/.NET Framework 4.8 consumer from a local NuGet install.
[CmdletBinding()]
param(
    [string]$CandidateDirectory,
    [ValidateSet('0.1.0-rc.1', '0.1.0')][string]$PackageVersion = '0.1.0-rc.1',
    [switch]$FromNuGetOrg,
    [string]$ExpectedSourceCommit
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ($FromNuGetOrg) {
    if (-not [string]::IsNullOrWhiteSpace($CandidateDirectory) -or $PackageVersion -cne '0.1.0' -or
        $ExpectedSourceCommit -cnotmatch '^[0-9a-fA-F]{40}$') {
        throw 'Public NuGet verification requires stable 0.1.0 and an exact expected source SHA; do not specify a local feed.'
    }
    $sources = 'https://api.nuget.org/v3/index.json'
} else {
    if ([string]::IsNullOrWhiteSpace($CandidateDirectory)) {
        throw 'Local candidate directory is required unless -FromNuGetOrg is selected.'
    }
    $feed = [IO.Path]::GetFullPath($CandidateDirectory)
    if (-not (Test-Path -LiteralPath (Join-Path $feed "RecordSift.$PackageVersion.nupkg"))) {
        throw 'Unpublished local candidate is missing.'
    }
    $sources = "$feed;https://api.nuget.org/v3/index.json"
}
$temporaryRoot = if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) { [IO.Path]::GetTempPath() } else { $env:RUNNER_TEMP }
$work = Join-Path $temporaryRoot ('RecordSift-Consumer-' + [guid]::NewGuid().ToString('N'))
$packages = Join-Path $work 'packages'
$output = Join-Path $work 'bin'
New-Item -ItemType Directory -Force -Path $packages, $output | Out-Null
try {
    $nuget = (Get-Command nuget -ErrorAction Stop).Source
    # Include nuget.org only to resolve the candidate's pinned third-party package dependencies.
    & $nuget install RecordSift -Version $PackageVersion -Framework net48 -OutputDirectory $packages -Source $sources -NonInteractive -NoHttpCache
    if ($LASTEXITCODE -ne 0) { throw 'Clean NuGet install failed.' }
    $expectedPackages = @{
        CsvHelper = '33.1.0'
        ExcelDataReader = '3.9.0'
        'Microsoft.Bcl.AsyncInterfaces' = '8.0.0'
        'Microsoft.Bcl.HashCode' = '1.1.1'
        'Microsoft.CSharp' = '4.7.0'
        'System.Memory' = '4.5.5'
        'System.Buffers' = '4.5.1'
        'System.Numerics.Vectors' = '4.5.0'
        'System.Runtime.CompilerServices.Unsafe' = '6.0.0'
        'System.Threading.Tasks.Extensions' = '4.5.4'
        'System.ValueTuple' = '4.5.0'
    }
    foreach ($package in $expectedPackages.GetEnumerator()) {
        $path = Join-Path $packages "$($package.Key).$($package.Value)"
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Unpinned or missing NuGet dependency: $($package.Key) version $($package.Value)"
        }
    }
    $installedRoot = Join-Path $packages "RecordSift.$PackageVersion"
    # Read the manifest in the downloaded .nupkg, not an optional separately
    # extracted .nuspec file. Local-feed CI exercises this same code path.
    $installedPackage = Join-Path $installedRoot "RecordSift.$PackageVersion.nupkg"
    if (-not (Test-Path -LiteralPath $installedPackage)) { throw 'Installed package archive missing.' }
    $archive = [System.IO.Compression.ZipFile]::OpenRead($installedPackage)
    try {
        $manifests = @($archive.Entries | Where-Object { $_.FullName -match '^[^/\\]+\.nuspec$' })
        if ($manifests.Count -ne 1) { throw 'Installed package must have exactly one root nuspec manifest.' }
        $reader = [System.IO.StreamReader]::new($manifests[0].Open())
        try {
            [xml]$publishedManifest = $reader.ReadToEnd()
        } finally {
            $reader.Dispose()
        }
    } finally {
        $archive.Dispose()
    }
    $publishedMeta = $publishedManifest.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
    if ($null -eq $publishedMeta) { throw 'Installed package metadata missing.' }
    $idNode = $publishedMeta.SelectSingleNode('*[local-name()="id"]')
    $versionNode = $publishedMeta.SelectSingleNode('*[local-name()="version"]')
    if ($null -eq $idNode -or $idNode.InnerText -cne 'RecordSift' -or
        $null -eq $versionNode -or $versionNode.InnerText -cne $PackageVersion) {
        throw 'Installed package ID or version is wrong.'
    }
    if ($FromNuGetOrg) {
        $publishedRepo = $publishedMeta.SelectSingleNode('*[local-name()="repository"]')
        if ($null -eq $publishedRepo -or
            $publishedRepo.GetAttribute('branch') -cne 'main' -or
            $publishedRepo.GetAttribute('url') -cne 'https://github.com/infoconex/RecordSift.git' -or
            $publishedRepo.GetAttribute('commit') -cne $ExpectedSourceCommit) {
            throw 'Published package exact source repository commit is wrong.'
        }
        Write-Host "PASS: nuget.org published package source commit $ExpectedSourceCommit"
    }
    foreach ($notice in @('LICENSE', 'THIRD-PARTY-NOTICES.md')) {
        $file = Join-Path $installedRoot $notice
        if (-not (Test-Path -LiteralPath $file)) { throw "Installed NuGet package missing $notice" }
        if ((Get-Content -LiteralPath $file -Raw) -cne (Get-Content -LiteralPath (Join-Path $root $notice) -Raw)) {
            throw "Installed NuGet package notice $notice does not match reviewed source."
        }
    }
    $lib = Join-Path $installedRoot 'lib\net48'
    $components = @('Domain', 'Application', 'Infrastructure.Csv', 'Infrastructure.Reporting', 'Infrastructure.Xlsx')
    foreach ($component in $components) {
        $name = "RecordSift.$component"
        foreach ($ext in @('.dll', '.xml')) {
            if (-not (Test-Path (Join-Path $lib "$name$ext"))) {
                throw "Installed package missing $name$ext"
            }
        }
        Copy-Item (Join-Path $lib "$name.dll") -Destination $output
    }
    # Dependency DLLs must come from the fresh NuGet installation, not repo binaries.
    $frameworks = @('net48', 'net472', 'net471', 'net47', 'net462', 'net461',
                    'net46', 'net45', 'net40', 'netstandard2.0', 'netstandard1.3')
    foreach ($dependency in @(Get-ChildItem -LiteralPath $packages -Directory |
            Where-Object { $_.Name -ne "RecordSift.$PackageVersion" })) {
        foreach ($framework in $frameworks) {
            $candidateLib = Join-Path $dependency.FullName "lib\$framework"
            if (-not (Test-Path $candidateLib)) { continue }
            foreach ($dll in @(Get-ChildItem -LiteralPath $candidateLib -Filter '*.dll' -File)) {
                $target = Join-Path $output $dll.Name
                if (Test-Path $target) { throw "Duplicate dependency: $($dll.Name)" }
                Copy-Item -LiteralPath $dll.FullName -Destination $target
            }
            break
        }
    }
    foreach ($vendor in @('CsvHelper.dll', 'ExcelDataReader.dll')) {
        if (-not (Test-Path (Join-Path $output $vendor))) {
            throw "Missing NuGet dependency: $vendor"
        }
    }
    $vswhere = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild) { throw 'MSBuild missing.' }
    $csc = Join-Path (Split-Path -Parent $msbuild) 'Roslyn\csc.exe'
    if (-not (Test-Path $csc)) { throw "C# compiler missing: $csc" }
    $source = Join-Path $work 'Program.cs'
    Copy-Item (Join-Path $root 'tests\ReleaseConsumer\Program.cs') -Destination $source
    $exe = Join-Path $output 'RecordSift.Consumer.exe'
    $references = @($components | ForEach-Object { '/reference:' + (Join-Path $output "RecordSift.$_.dll") })
    & $csc /nologo /target:exe /langversion:7.3 "/out:$exe" @references $source
    if ($LASTEXITCODE -ne 0) { throw 'Standalone consumer C# compilation failed.' }
    & $exe
    if ($LASTEXITCODE -ne 0) { throw 'Standalone consumer execution failed.' }
    $origin = if ($FromNuGetOrg) { 'public nuget.org only' } else { 'unpublished local feed' }
    Write-Host "PASS: separate installed RecordSift $PackageVersion NuGet consumer from $origin verified without repository binaries."
} finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
