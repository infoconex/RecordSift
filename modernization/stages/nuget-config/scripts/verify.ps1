# RecordSift nuget.config case-study stage verification.
# Validates the checked-in configuration with a positive/negative inheritance
# control and optionally restores without an explicit -ConfigFile.
# Usage: pwsh -NoProfile -File modernization/stages/nuget-config/scripts/verify.ps1 -CheckRestore
[CmdletBinding()]
param(
    [string]$OutputDirectory = 'TestResults/modernization-nuget-config',
    [switch]$CheckRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
if (-not [IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot $OutputDirectory
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

# Verify exact Git-path casing; Windows file lookups alone are case-insensitive.
$trackedNuGetConfigs = @(& git -C $repositoryRoot ls-files | Where-Object { $_ -ieq 'nuget.config' })
if ($LASTEXITCODE -ne 0 -or $trackedNuGetConfigs.Count -ne 1 -or $trackedNuGetConfigs[0] -cne 'nuget.config') {
    throw 'Expected exactly one tracked root configuration named nuget.config (lowercase).'
}

$configPath = Join-Path $repositoryRoot 'nuget.config'
$configText = Get-Content -LiteralPath $configPath -Raw
[xml]$configXml = $configText
$sourceUrl = 'https://api.nuget.org/v3/index.json'
foreach ($xpath in @(
    '/configuration/packageSources/clear',
    '/configuration/disabledPackageSources/clear',
    '/configuration/auditSources/clear',
    "/configuration/packageSources/add[@key='nuget.org' and @value='$sourceUrl']",
    "/configuration/auditSources/add[@key='nuget.org-audit' and @value='$sourceUrl']"
)) {
    if (-not $configXml.SelectSingleNode($xpath)) {
        throw "Expected nuget.config element missing: $xpath"
    }
}
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('RecordSiftNuGetStage-' + [guid]::NewGuid().ToString('N'))
$childDirectory = Join-Path $temporaryRoot 'Application'
$childFile = Join-Path $childDirectory 'nuget.config'
$results = [System.Collections.Generic.List[object]]::new()
$report = $null
$failure = $null

function Invoke-SourceProbe([string]$name, [string]$config, [string]$expectedState) {
    Set-Content -LiteralPath $script:childFile -Value $config -Encoding utf8
    Push-Location -LiteralPath $script:childDirectory
    try {
        $output = @(& dotnet nuget list source 2>&1 | ForEach-Object { "$_" })
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    $text = $output -join [Environment]::NewLine
    $statusMatch = [regex]::Match($text, '(?i)\bnuget\.org\s+\[(Enabled|Disabled)\]')
    $actual = if ($statusMatch.Success) { $statusMatch.Groups[1].Value } else { 'NotFound' }
    $unwanted = [regex]::IsMatch($text, '(?i)\btest-feed\s+\[(Enabled|Disabled)\]')
    $passed = ($exitCode -eq 0 -and $actual -eq $expectedState -and -not $unwanted)
    Write-Host "=== $name ==="
    Write-Host $text
    Write-Host "RESULT $(if ($passed) { 'PASS' } else { 'FAIL' }): expected=$expectedState, actual=$actual, inherited-test-feed=$unwanted"
    $script:results.Add([pscustomobject]@{
        scenario = $name
        expected_nuget_org = $expectedState
        actual_nuget_org = $actual
        inherited_test_feed_present = $unwanted
        exit_code = $exitCode
        passed = $passed
        output = $text
    })
}

try {
    New-Item -ItemType Directory -Path $childDirectory -Force | Out-Null
    @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="test-feed" value="C:\RecordSiftTestFeed" />
  </packageSources>
  <disabledPackageSources>
    <add key="nuget.org" value="true" />
  </disabledPackageSources>
</configuration>
'@ | Set-Content -LiteralPath (Join-Path $temporaryRoot 'nuget.config') -Encoding utf8

    $negativeConfig = [regex]::Replace(
        $configText,
        '(?s)\s*<disabledPackageSources>\s*<clear\s*/>\s*</disabledPackageSources>',
        ''
    )
    if ($negativeConfig -eq $configText) {
        throw 'Negative control could not remove disabledPackageSources from disposable configuration.'
    }

    $sdkVersion = (& dotnet --version) -join ''
    $nugetVersion = @(& nuget help 2>&1 | ForEach-Object { "$_" } | Where-Object { $_ -match 'NuGet Version:' } | Select-Object -First 1) -join ''
    $gitCommit = (& git -C $repositoryRoot rev-parse HEAD) -join ''
    Write-Host "Commit: $gitCommit"
    Write-Host "OS: $([System.Runtime.InteropServices.RuntimeInformation]::OSDescription)"
    Write-Host "PowerShell: $($PSVersionTable.PSVersion)"
    Write-Host "dotnet SDK: $sdkVersion"
    Write-Host "NuGet CLI: $nugetVersion"

    Invoke-SourceProbe 'Positive: checked-in configuration overrides inherited disabled source' $configText 'Enabled'
    Invoke-SourceProbe 'Negative: removing disabledPackageSources clear inherits disabled setting' $negativeConfig 'Disabled'
    Invoke-SourceProbe 'Restored: checked-in configuration enables nuget.org again' $configText 'Enabled'

    # Restore negative/positive controls: a single existing RecordSift dependency
    # in disposable packages.config, with separate empty installation directories.
    # -DirectDownload and -NoHttpCache reduce cache interference.
    $testPackageId = 'NUnit'
    $testPackageVersion = '3.14.0'
    $testManifest = Join-Path $childDirectory 'packages.config'
    @'
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="NUnit" version="3.14.0" targetFramework="net48" />
</packages>
'@ | Set-Content -LiteralPath $testManifest -Encoding utf8
    $restoreControls = [System.Collections.Generic.List[object]]::new()

    foreach ($scenario in @(
        @{ label = 'Negative: inherited disabled nuget.org'; xml = $negativeConfig; expectSuccess = $false; destination = 'negative-restore-packages' },
        @{ label = 'Positive: disabled source cleared'; xml = $configText; expectSuccess = $true; destination = 'positive-restore-packages' }
    )) {
        Set-Content -LiteralPath $childFile -Value $scenario.xml -Encoding utf8
        $destination = Join-Path $temporaryRoot $scenario.destination
        Push-Location -LiteralPath $childDirectory
        try {
            $commandOutput = @(& nuget restore .\packages.config -NonInteractive -PackagesDirectory $destination -DirectDownload -NoHttpCache -Verbosity detailed 2>&1 | ForEach-Object { "$_" })
            $commandExit = $LASTEXITCODE
        }
        finally {
            Pop-Location
        }
        $installed = Test-Path -LiteralPath (Join-Path $destination "$testPackageId.$testPackageVersion")
        $success = $commandExit -eq 0 -and $installed
        $passed = $success -eq $scenario.expectSuccess
        $suffix = if ($scenario.expectSuccess) { 'positive' } else { 'negative' }
        $commandOutput | Set-Content -LiteralPath (Join-Path $OutputDirectory "$suffix-restore-control.log") -Encoding utf8
        Write-Host "=== $($scenario.label) ==="
        Write-Host "NuGet restore exit=$commandExit; installed=$installed; test PASS=$passed"
        $restoreControls.Add([pscustomobject]@{
            scenario = $scenario.label
            expected_success = $scenario.expectSuccess
            actual_success = $success
            restore_exit_code = $commandExit
            package_installed = $installed
            no_configfile_argument = $true
            direct_download = $true
            no_http_cache = $true
            passed = $passed
            log = "$suffix-restore-control.log"
        })
    }
    # Ensure cleanup and downstream source checks see the original stage file.
    Set-Content -LiteralPath $childFile -Value $configText -Encoding utf8

    $restore = $null
    if ($CheckRestore) {
        Push-Location -LiteralPath $repositoryRoot
        try {
            $sourceList = @(& dotnet nuget list source 2>&1 | ForEach-Object { "$_" })
            $sourceExit = $LASTEXITCODE
            $sourceList | Set-Content -LiteralPath (Join-Path $OutputDirectory 'effective-sources.txt') -Encoding utf8
            $packagesPath = Join-Path $temporaryRoot 'fresh-packages'
            $restoreOutput = @(& nuget restore .\RecordSift.sln -NonInteractive -PackagesDirectory $packagesPath -Verbosity detailed 2>&1 | ForEach-Object { "$_" })
            $restoreExit = $LASTEXITCODE
            $restoreOutput | Set-Content -LiteralPath (Join-Path $OutputDirectory 'restore.log') -Encoding utf8
        }
        finally {
            Pop-Location
        }
        $sourceText = $sourceList -join [Environment]::NewLine
        $validSources = $sourceExit -eq 0 -and [regex]::IsMatch($sourceText, '(?i)\bnuget\.org\s+\[Enabled\]') -and
            -not [regex]::IsMatch($sourceText, '(?i)\btest-feed\s+\[(Enabled|Disabled)\]')
        $restore = [pscustomobject]@{
            passed = ($validSources -and $restoreExit -eq 0)
            restore_exit_code = $restoreExit
            source_list_exit_code = $sourceExit
            configfile_argument_used = $false
            fresh_packages_directory_used = $true
            effective_sources = $sourceText
        }
        Write-Host "Normal restore (no -ConfigFile): exit=$restoreExit, effective sources match=$validSources"
    }

    $report = [pscustomobject]@{
        source_revision = $gitCommit
        baseline_revision = '3627a62260437db0bde83c5c7d3be56caee8af95'
        recorded_utc = (Get-Date).ToUniversalTime().ToString('o')
        dotnet_sdk = $sdkVersion
        nuget_cli = $nugetVersion
        powershell = "$($PSVersionTable.PSVersion)"
        scenarios = @($results)
        restore_controls = @($restoreControls)
        normal_restore = $restore
    }
    if (@($results | Where-Object { -not $_.passed }).Count -ne 0 -or @($restoreControls | Where-Object { -not $_.passed }).Count -ne 0 -or ($CheckRestore -and -not $restore.passed)) {
        throw 'NuGet configuration stage verification failed; see the report and logs.'
    }
}
catch {
    $failure = $_.Exception.Message
    Write-Host "ERROR: $failure"
}
finally {
    if (-not $report) {
        $report = [pscustomobject]@{
            source_revision = if (Get-Variable gitCommit -ErrorAction Ignore) { $gitCommit } else { $null }
            baseline_revision = '3627a62260437db0bde83c5c7d3be56caee8af95'
            scenarios = @($results)
            failure = $failure
        }
    }
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'results.json') -Encoding utf8
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Force -Recurse
    }
}
if ($failure) {
    throw "NuGet configuration stage verification failed: $failure"
}
Write-Host 'NuGet configuration case-study stage verification passed.'
