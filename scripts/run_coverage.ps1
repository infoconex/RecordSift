# Runs the existing five-assembly NUnit test script under pinned Microsoft code coverage tooling.
# Prerequisites: restored NuGet packages, built Release test assemblies, .NET SDK 8+, PowerShell 7.
[CmdletBinding()]
param(
    [ValidateSet('All', 'Unit', 'Component', 'Integration')]
    [string]$Level = 'All',
    [ValidateSet('', 'Acceptance', 'Contract', 'Regression', 'Characterization')]
    [string]$Verification = '',
    [string]$Requirement = '',
    [string]$Scenario = '',
    [string]$Where = ''
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$root = Split-Path -Parent $PSScriptRoot
$coverageDirectory = Join-Path $root 'TestResults/coverage'
$reportDirectory = Join-Path $coverageDirectory 'report'
$coverageFile = Join-Path $coverageDirectory 'coverage.cobertura.xml'
$settings = Join-Path $root 'config/coverage.xml'
$testScript = Join-Path $root 'scripts/run_tests.ps1'

Push-Location $root
try {
    New-Item -ItemType Directory -Force -Path $coverageDirectory | Out-Null
    Remove-Item $coverageFile -ErrorAction SilentlyContinue

    & dotnet --version
    if ($LASTEXITCODE -ne 0) { throw 'The .NET SDK is required to run pinned local tools.' }
    & dotnet tool restore --configfile (Join-Path $root 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw "Local tool restore failed: $LASTEXITCODE" }
    & dotnet tool run dotnet-coverage -- --version
    if ($LASTEXITCODE -ne 0) { throw "dotnet-coverage version check failed: $LASTEXITCODE" }

    # Quoting a command string follows Microsoft's documented dotnet-coverage collect syntax.
    # run_tests.ps1 remains authoritative for selection, missing assemblies and NUnit failures.
    $selectorArgs = @()
    if ($Level -ne 'All') { $selectorArgs += @('-Level', $Level) }
    if ($Verification) { $selectorArgs += @('-Verification', $Verification) }
    if ($Requirement) { $selectorArgs += @('-Requirement', $Requirement) }
    if ($Scenario) { $selectorArgs += @('-Scenario', $Scenario) }
    if ($Where) { $selectorArgs += @('-Where', ('"' + $Where.Replace('"', '\"') + '"')) }

    $testCommand = 'pwsh -NoProfile -File "' + $testScript + '"'
    if ($selectorArgs.Count -gt 0) { $testCommand += ' ' + ($selectorArgs -join ' ') }
    & dotnet tool run dotnet-coverage -- collect $testCommand --settings $settings --output $coverageFile --output-format cobertura
    if ($LASTEXITCODE -ne 0) { throw "Coverage collection or NUnit execution failed: $LASTEXITCODE" }
    if (-not (Test-Path $coverageFile)) { throw "Coverage report was not created: $coverageFile" }

    [xml]$tests = Get-Content (Join-Path $root 'TestResults/nunit-selected.xml') -Raw
    $testCount = [int]$tests.'test-run'.total
    $failedTests = [int]$tests.'test-run'.failed
    $skippedTests = [int]$tests.'test-run'.skipped
    $testAssemblies = @($tests.SelectNodes('//test-suite[@type="Assembly"]')).Count
    if ($testCount -lt 1 -or $failedTests -ne 0 -or $testAssemblies -ne 5) {
        throw "Invalid coverage NUnit run: total=$testCount failed=$failedTests assemblies=$testAssemblies."
    }
    if ($selectorArgs.Count -eq 0 -and ($testCount -lt 419 -or $skippedTests -ne 1)) {
        throw "Full-suite coverage regression: total=$testCount skipped=$skippedTests; expected at least 419 and one intentional skip."
    }
    Write-Host "Coverage NUnit evidence: total=$testCount failed=$failedTests skipped=$skippedTests assemblies=$testAssemblies."

    # An empty or incomplete collector run must not appear successful.
    [xml]$coverage = Get-Content $coverageFile -Raw
    $expected = @(
        'RecordSift.Domain',
        'RecordSift.Application',
        'RecordSift.Infrastructure.Csv',
        'RecordSift.Infrastructure.Xlsx',
        'RecordSift.Infrastructure.Reporting'
    )
    $packages = @($coverage.SelectNodes('//package'))
    $missing = @($expected | Where-Object {
        $name = $_
        -not @($packages | Where-Object { $_.GetAttribute('name') -eq $name }).Count
    })
    if ($missing.Count -gt 0) {
        throw "Coverage is missing production assemblies: $($missing -join ', '). Observed packages: $(@($packages | ForEach-Object { $_.GetAttribute('name') }) -join ', ')"
    }
    $unexpected = @($packages | Where-Object { $expected -cnotcontains $_.GetAttribute('name') })
    if ($unexpected.Count -gt 0) {
        throw "Coverage includes unexpected packages: $(@($unexpected | ForEach-Object { $_.GetAttribute('name') }) -join ', ')"
    }

    $linesValid = [int]$coverage.coverage.'lines-valid'
    $branchesValid = [int]$coverage.coverage.'branches-valid'
    if ($linesValid -lt 1 -or $branchesValid -lt 1) {
        throw "Coverage lacks line or branch instrumentation: lines=$linesValid branches=$branchesValid"
    }
    foreach ($package in $packages) {
        $classes = @($package.SelectNodes('./classes/class'))
        if ($classes.Count -eq 0) { throw "Coverage package '$($package.name)' has no source classes." }
        $source = @($classes | Where-Object { $_.GetAttribute('filename') -match '(^|[/\\])src[/\\].*\.cs        Write-Host ("Coverage {0}: line={1:p2}, branch={2:p2}, classes={3}" -f $package.name, [double]$package.'line-rate', [double]$package.'branch-rate', $classes.Count)
    }

    & dotnet tool run reportgenerator -- "-reports:$coverageFile" "-targetdir:$reportDirectory" '-reporttypes:Html;Cobertura;MarkdownSummaryGithub'
    if ($LASTEXITCODE -ne 0) { throw "ReportGenerator failed: $LASTEXITCODE" }
    if (-not (Test-Path (Join-Path $reportDirectory 'index.html'))) { throw 'HTML coverage report is missing.' }
    Write-Host "Overall: line=$($coverage.coverage.'line-rate'), branch=$($coverage.coverage.'branch-rate'); report: $reportDirectory"
} finally {
    Pop-Location
}
 })
        if ($source.Count -eq 0) { throw "Coverage package '$($package.name)' has no RecordSift source file paths." }
        $existingFiles = @($source | Where-Object {
            $file = $_.GetAttribute('filename').Replace('/', [IO.Path]::DirectorySeparatorChar)
            $resolved = if ([IO.Path]::IsPathRooted($file)) { $file } else { Join-Path $root $file }
            Test-Path -LiteralPath $resolved -PathType Leaf
        })
        if ($existingFiles.Count -eq 0) {
            throw "Coverage package '$($package.name)' has no matching source files on disk; check PDB and source mapping."
        }
        Write-Host ("Coverage {0}: line={1:p2}, branch={2:p2}, classes={3}" -f $package.name, [double]$package.'line-rate', [double]$package.'branch-rate', $classes.Count)
    }

    & dotnet tool run reportgenerator -- "-reports:$coverageFile" "-targetdir:$reportDirectory" '-reporttypes:Html;Cobertura;MarkdownSummaryGithub'
    if ($LASTEXITCODE -ne 0) { throw "ReportGenerator failed: $LASTEXITCODE" }
    if (-not (Test-Path (Join-Path $reportDirectory 'index.html'))) { throw 'HTML coverage report is missing.' }
    Write-Host "Overall: line=$($coverage.coverage.'line-rate'), branch=$($coverage.coverage.'branch-rate'); report: $reportDirectory"
} finally {
    Pop-Location
}
