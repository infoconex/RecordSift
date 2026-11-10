# NuGet.Config source-inheritance verification for dotnet-modernization issue #87.
# Reproducible from the repository root or GitHub Actions; does not edit product files.
# Execute: pwsh -NoProfile -File research/nuget-config/scripts/verify-inheritance.ps1 -CheckRestore
[CmdletBinding()]
param(
    [string]$OutputDirectory = 'TestResults/nuget-config-inheritance',
    [switch]$CheckRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if (-not [IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot $OutputDirectory
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('NuGetConfigResearch-' + [guid]::NewGuid().ToString('N'))
$childDirectory = Join-Path $temporaryRoot 'Application'
$childFile = Join-Path $childDirectory 'NuGet.Config'
$results = [System.Collections.Generic.List[object]]::new()
$runNotes = [System.Collections.Generic.List[string]]::new()

function Write-Note([string]$message) {
    Write-Host $message
    $script:runNotes.Add($message)
}
function Invoke-NuGetSources([string]$label, [string]$config, [string]$expectedStatus, [bool]$expectedTestFeed) {
    Set-Content -LiteralPath $script:childFile -Value $config -Encoding utf8
    Push-Location -LiteralPath $script:childDirectory
    try {
        $lines = @(& dotnet nuget list source 2>&1 | ForEach-Object { "$_" })
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    $actualText = $lines -join [Environment]::NewLine
    $match = [regex]::Match($actualText, '(?i)\bnuget\.org\s+\[(Enabled|Disabled)\]')
    $actualStatus = if ($match.Success) { $match.Groups[1].Value } else { 'NotFound' }
    $actualTestFeed = [regex]::IsMatch($actualText, '(?i)\btest-feed\s+\[(Enabled|Disabled)\]')
    $passed = $exitCode -eq 0 -and $actualStatus -eq $expectedStatus -and $actualTestFeed -eq $expectedTestFeed
    Write-Note "=== $label ==="
    Write-Note $actualText
    Write-Note "RESULT: $(if ($passed) { 'PASS' } else { 'FAIL' }); nuget.org=$actualStatus; test-feed-present=$actualTestFeed; exit=$exitCode"
    $script:results.Add([pscustomobject]@{
        scenario = $label
        pass = $passed
        expected_nuget_status = $expectedStatus
        observed_nuget_status = $actualStatus
        expected_test_feed_present = $expectedTestFeed
        observed_test_feed_present = $actualTestFeed
        exit_code = $exitCode
        output = $actualText
    })
}

$parentXml = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="test-feed" value="C:\NuGetTestFeed" />
  </packageSources>
  <disabledPackageSources>
    <add key="nuget.org" value="true" />
  </disabledPackageSources>
</configuration>
'@
$childFull = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <disabledPackageSources>
    <clear />
  </disabledPackageSources>
</configuration>
'@
$childInheritSources = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <disabledPackageSources>
    <clear />
  </disabledPackageSources>
</configuration>
'@
$childInheritDisabled = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
'@

$exception = $null
$restoreResult = $null
try {
    New-Item -ItemType Directory -Path $childDirectory -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $temporaryRoot 'NuGet.Config') -Value $parentXml -Encoding utf8
    $sdkVersion = (& dotnet --version) -join ''
    $nugetHelp = @(& nuget help 2>&1 | ForEach-Object { "$_" })
    $nugetVersion = ($nugetHelp | Where-Object { $_ -match 'NuGet Version:' } | Select-Object -First 1)
    Write-Note "Repository HEAD: $((& git -C $repositoryRoot rev-parse HEAD) -join '')"
    Write-Note "OS: $([System.Runtime.InteropServices.RuntimeInformation]::OSDescription)"
    Write-Note "PowerShell: $($PSVersionTable.PSVersion)"
    Write-Note "dotnet SDK: $sdkVersion"
    Write-Note "NuGet CLI: $nugetVersion"
    Write-Note "NuGet location: $((Get-Command nuget).Source)"
    Write-Note "Source checks: dotnet nuget list source (NO --configfile)"
    Invoke-NuGetSources 'A: clear both collections' $childFull 'Enabled' $false
    Invoke-NuGetSources 'B: omit packageSources clear' $childInheritSources 'Enabled' $true
    Invoke-NuGetSources 'C: omit entire disabledPackageSources section' $childInheritDisabled 'Disabled' $false
    Invoke-NuGetSources 'D: restore both clears' $childFull 'Enabled' $false

    if ($CheckRestore) {
        Write-Note '=== E: normal RecordSift restore WITHOUT -ConfigFile ==='
        Push-Location -LiteralPath $repositoryRoot
        try {
            $before = @(& dotnet nuget list source 2>&1 | ForEach-Object { "$_" })
            $sourceExit = $LASTEXITCODE
            $before | Set-Content -LiteralPath (Join-Path $OutputDirectory 'record-sift-effective-sources.txt') -Encoding utf8
            $packages = Join-Path $temporaryRoot 'fresh-packages'
            $restoreLines = @(& nuget restore .\RecordSift.sln -NonInteractive -PackagesDirectory $packages -Verbosity detailed 2>&1 | ForEach-Object { "$_" })
            $restoreExit = $LASTEXITCODE
            $restoreLines | Set-Content -LiteralPath (Join-Path $OutputDirectory 'record-sift-restore.log') -Encoding utf8
        }
        finally {
            Pop-Location
        }
        $sourceText = $before -join [Environment]::NewLine
        Write-Note "Effective source listing:"
        Write-Note $sourceText
        Write-Note "NuGet restore exit code: $restoreExit"
        Write-Note "Restore full log saved: record-sift-restore.log"
        $sourceMatch = [regex]::IsMatch($sourceText, '(?i)\bnuget\.org\s+\[Enabled\]')
        $restorePassed = $sourceExit -eq 0 -and $sourceMatch -and $restoreExit -eq 0
        $restoreResult = [pscustomobject]@{
            pass = $restorePassed
            source_list_exit_code = $sourceExit
            restore_exit_code = $restoreExit
            expected_source = 'nuget.org [Enabled]'
            selected_config_argument = $false
            fresh_packages_directory = $true
            full_log = 'record-sift-restore.log'
        }
    }
}
catch {
    $exception = $_.Exception.Message
    Write-Note "UNEXPECTED ERROR: $exception"
}
finally {
    $metadata = [pscustomobject]@{
        timestamp_utc = (Get-Date).ToUniversalTime().ToString('o')
        repository = 'infoconex/RecordSift'
        commit = (& git -C $repositoryRoot rev-parse HEAD) -join ''
        sdk_version = if (Get-Variable sdkVersion -ErrorAction Ignore) { $sdkVersion } else { $null }
        nuget_cli = if (Get-Variable nugetVersion -ErrorAction Ignore) { $nugetVersion } else { $null }
        powershell_version = "$($PSVersionTable.PSVersion)"
        scenarios = @($results)
        normal_restore = $restoreResult
        unexpected_error = $exception
    }
    $metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'results.json') -Encoding utf8
    $runNotes | Set-Content -LiteralPath (Join-Path $OutputDirectory 'inheritance-output.txt') -Encoding utf8
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
if ($exception) { throw "NuGet config research failed: $exception" }
if (@($results | Where-Object { -not $_.pass }).Count -ne 0) {
    throw 'NuGet source inheritance test failed; see results.json and inheritance-output.txt'
}
if ($CheckRestore -and (-not $restoreResult -or -not $restoreResult.pass)) {
    throw 'NuGet normal restore failed; see results.json and record-sift-restore.log'
}
Write-Host 'All executed NuGet configuration research checks passed.'
