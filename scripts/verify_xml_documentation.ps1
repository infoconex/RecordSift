[CmdletBinding()]
param([switch]$Negative)

$ErrorActionPreference = 'Stop'
$assemblies = @(
    'RecordSift.Domain',
    'RecordSift.Application',
    'RecordSift.Infrastructure.Csv',
    'RecordSift.Infrastructure.Xlsx',
    'RecordSift.Infrastructure.Reporting'
)

foreach ($name in $assemblies) {
    $path = Join-Path (Join-Path (Join-Path 'src' $name) 'bin/Release') "$name.xml"
    if (-not (Test-Path $path)) { throw "Missing Release XML documentation: $path" }
    [xml]$document = Get-Content -Path $path -Raw
    $members = @($document.doc.members.member)
    if ($members.Count -lt 1) { throw "Empty XML documentation members: $path" }
    Write-Host "Verified XML documentation for $name ($($members.Count) entries)."
}

if (-not $Negative) { return }

# Deliberately remove the documented public property, then verify an actual C# compiler
# rebuild rejects it. Always restore the source, including when the negative test fails.
$path = 'src/RecordSift.Domain/Schema/ExpectedColumn.cs'
$original = Get-Content -Path $path -Raw
$pattern = '(?m)^        /// <summary>Gets the exact-case source column identifier\.</summary>\r?\n'
$changed = [regex]::Replace($original, $pattern, '', 1)
if ($changed -eq $original) { throw 'Negative fixture documentation marker was not found.' }

try {
    Set-Content -Path $path -Value $changed -Encoding utf8 -NoNewline
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild) { throw 'MSBuild not found for documentation negative test.' }
    $lines = @(& $msbuild 'src/RecordSift.Domain/RecordSift.Domain.csproj' /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU /verbosity:minimal 2>&1)
    $code = $LASTEXITCODE
    $lines | ForEach-Object { Write-Host "$_" }
    if ($code -eq 0) { throw 'Undocumented public member unexpectedly compiled successfully.' }
    if (-not ($lines -match 'CS1591')) { throw 'Negative build failed without a CS1591 diagnostic.' }
    Write-Host 'Negative verification passed: undocumented public ExpectedColumn.Name fails compilation with CS1591.'
}
finally {
    Set-Content -Path $path -Value $original -Encoding utf8 -NoNewline
}

# A failed compiler invocation was expected. Reset the native exit code so the
# surrounding GitHub Actions PowerShell wrapper treats this successful probe as success.
$global:LASTEXITCODE = 0
