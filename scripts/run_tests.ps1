# Requires built .NET Framework 4.8 Release test assemblies and restored NUnit packages.
# Usage: .\scripts\run_tests.ps1 -Level Integration -Requirement FR-017
[CmdletBinding()]
param(
    [ValidateSet('All', 'Unit', 'Component', 'Integration')]
    [string]$Level = 'All',
    [string]$Verification = '',
    [string]$Requirement = '',
    [string]$Scenario = '',
    [string]$Where = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$runner = Join-Path $root 'packages\NUnit.ConsoleRunner.3.17.0\tools\nunit3-console.exe'
$assemblies = @(
    'tests\RecordSift.Domain.Tests\bin\Release\RecordSift.Domain.Tests.dll',
    'tests\RecordSift.Application.Tests\bin\Release\RecordSift.Application.Tests.dll',
    'tests\RecordSift.Infrastructure.Csv.Tests\bin\Release\RecordSift.Infrastructure.Csv.Tests.dll',
    'tests\RecordSift.Infrastructure.Reporting.Tests\bin\Release\RecordSift.Infrastructure.Reporting.Tests.dll'
) | ForEach-Object { Join-Path $root $_ }

foreach ($file in @($runner) + $assemblies) {
    if (-not (Test-Path $file)) { throw "Missing built test artifact: $file" }
}
if ($Requirement -and $Requirement -notmatch '^(FR|NFR)-\d{3}$') {
    throw 'Requirement must be FR-### or NFR-###.'
}
if ($Scenario -and $Scenario -notmatch '^(FR|NFR)-\d{3}-SC\d{2}$') {
    throw 'Scenario must be FR-###-SC## or NFR-###-SC##.'
}
if ($Verification -and $Verification -notin @('Acceptance', 'Contract', 'Regression', 'Characterization')) {
    throw 'Unsupported Verification purpose.'
}
if ($Requirement -and $Scenario -and -not $Scenario.StartsWith("$Requirement-SC")) {
    throw 'Scenario must belong to the requested Requirement.'
}

$expressions = @()
if ($Level -ne 'All') { $expressions += "cat == '$Level'" }
if ($Verification) { $expressions += "Verification == '$Verification'" }
if ($Requirement) { $expressions += "Requirement == '$Requirement'" }
if ($Scenario) { $expressions += "Scenario == '$Scenario'" }
if ($Where) { $expressions += "($Where)" }
$args = @($assemblies)
if ($expressions.Count -gt 0) { $args += "--where=$($expressions -join ' && ')" }
$args += '--noheader'
$resultFile = Join-Path $root 'TestResults\nunit-selected.xml'
New-Item -ItemType Directory -Force -Path (Split-Path $resultFile) | Out-Null
$args += "--result=$resultFile;format=nunit3"
& $runner @args
if ($LASTEXITCODE -ne 0) { throw "NUnit failed with exit code $LASTEXITCODE." }
[xml]$result = Get-Content $resultFile
$total = [int]$result.'test-run'.total
if ($total -lt 1) { throw 'The test filter selected zero tests.' }
if ([int]$result.'test-run'.failed -ne 0) { throw 'NUnit returned test failures.' }
Write-Host "NUnit selected $total test cases; evidence: $resultFile"
