# Run a standalone C# 7.3/.NET Framework 4.8 consumer from a local NuGet install.
[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$CandidateDirectory)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$feed = [IO.Path]::GetFullPath($CandidateDirectory)
if (-not (Test-Path -LiteralPath (Join-Path $feed 'RecordSift.0.1.0-rc.1.nupkg'))) {
    throw 'Unpublished local candidate is missing.'
}
$work = Join-Path $env:RUNNER_TEMP ('RecordSift-Consumer-' + [guid]::NewGuid().ToString('N'))
$packages = Join-Path $work 'packages'
$output = Join-Path $work 'bin'
New-Item -ItemType Directory -Force -Path $packages, $output | Out-Null
try {
    $nuget = (Get-Command nuget -ErrorAction Stop).Source
    # Include nuget.org only to resolve the candidate's pinned third-party package dependencies.
    & $nuget install RecordSift -Version '0.1.0-rc.1' -Framework net48 -OutputDirectory $packages -Source "$feed;https://api.nuget.org/v3/index.json" -NonInteractive -NoCache
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
    $lib = Join-Path $packages 'RecordSift.0.1.0-rc.1\lib\net48'
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
            Where-Object { $_.Name -ne 'RecordSift.0.1.0-rc.1' })) {
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
    Write-Host 'PASS: separate installed NuGet consumer verified without repository binaries.'
} finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
