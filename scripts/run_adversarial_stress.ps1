param(
    [int]$Iterations = 3,
    [int]$MaximumPrivateMegabytes = 768,
    [int]$PerProcessTimeoutSeconds = 90
)

$ErrorActionPreference = 'Stop'
if ($Iterations -lt 1 -or $Iterations -gt 10) {
    throw 'Iterations must be between 1 and 10.'
}
if ($MaximumPrivateMegabytes -lt 256 -or $MaximumPrivateMegabytes -gt 2048) {
    throw 'Maximum private memory must be between 256 and 2048 MiB.'
}
if ($PerProcessTimeoutSeconds -lt 15 -or $PerProcessTimeoutSeconds -gt 180) {
    throw 'Process timeout must be between 15 and 180 seconds.'
}

$runner = (Resolve-Path 'packages\NUnit.ConsoleRunner.3.17.0\tools\nunit3-console.exe').Path
$assemblies = @(
    'tests\RecordSift.Infrastructure.Csv.Tests\bin\Release\RecordSift.Infrastructure.Csv.Tests.dll',
    'tests\RecordSift.Infrastructure.Xlsx.Tests\bin\Release\RecordSift.Infrastructure.Xlsx.Tests.dll'
)
New-Item -ItemType Directory -Force -Path 'TestResults' | Out-Null
$privateLimitBytes = [long]$MaximumPrivateMegabytes * 1048576
for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
    foreach ($assembly in $assemblies) {
        $resolvedAssembly = (Resolve-Path $assembly).Path
        $suite = [System.IO.Path]::GetFileNameWithoutExtension($assembly)
        $resultPath = "TestResults/extended-$suite-$iteration.xml"
        # Pass each option as an explicitly quoted command-line argument.
        # NUnit runs IN PROCESS so the monitored PID owns parser allocations.
        $arguments = @(
            '"' + $resolvedAssembly + '"',
            '--inprocess',
            '"--where=Requirement == ''NFR-007''"',
            '"--result=' + $resultPath + ';format=nunit3"',
            '--noheader'
        ) -join ' '

        Write-Host "Extended bounded security run $iteration/$Iterations for $suite"
        $process = Start-Process -FilePath $runner -ArgumentList $arguments -PassThru -NoNewWindow
        $started = [DateTime]::UtcNow
        $sampledPeak = [long]0
        try {
            while (-not $process.HasExited) {
                Start-Sleep -Milliseconds 100
                $process.Refresh()
                if (-not $process.HasExited) {
                    $observed = [long]$process.PrivateMemorySize64
                    if ($observed -gt $sampledPeak) { $sampledPeak = $observed }
                    if ($observed -gt $privateLimitBytes) {
                        throw "Sampled private memory exceeded $MaximumPrivateMegabytes MiB."
                    }
                }
                if (([DateTime]::UtcNow - $started).TotalSeconds -gt $PerProcessTimeoutSeconds) {
                    throw "NUnit process exceeded $PerProcessTimeoutSeconds seconds."
                }
            }
            $process.WaitForExit()
            if ($process.ExitCode -ne 0) {
                throw "NUnit returned exit code $($process.ExitCode) for $suite."
            }
            if (-not (Test-Path $resultPath)) {
                throw "NUnit result file missing: $resultPath"
            }
            [xml]$result = Get-Content -Raw -Path $resultPath
            $selected = [int]$result.'test-run'.total
            $failed = [int]$result.'test-run'.failed
            if ($selected -lt 1 -or $failed -ne 0) {
                throw "Invalid extended test selection: selected=$selected, failed=$failed."
            }
            $sampledMiB = [math]::Round($sampledPeak / 1048576.0, 1)
            Write-Host "PASS: $suite, cases=$selected, failed=0, sampled peak private=$sampledMiB MiB, limit=$MaximumPrivateMegabytes MiB."
        }
        finally {
            $process.Refresh()
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
            $process.Dispose()
        }
    }
}

Write-Host 'Extended NFR-007 stress selections passed. Memory is sampled, not a hard peak or heap limit; NUnit is isolated from the workflow runner.'
