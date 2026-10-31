# NFR-010-SC01: inspect compiled net48 public assemblies; no third-party API diff tool.
# Run on Windows PowerShell 5.1 AFTER Release MSBuild: powershell -File scripts/verify_public_api.ps1
# To review intentional changes, inspect the printed diff and update the baseline in a PR.
param([switch]$PrintCurrent)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$baselinePath = Join-Path $root 'docs\compatibility\public-api-net48.txt'
$components = @(
    'Domain',
    'Application',
    'Infrastructure.Csv',
    'Infrastructure.Reporting',
    'Infrastructure.Xlsx'
)
$flags = [System.Reflection.BindingFlags]::Public -bor
    [System.Reflection.BindingFlags]::Instance -bor
    [System.Reflection.BindingFlags]::Static -bor
    [System.Reflection.BindingFlags]::DeclaredOnly

function Format-Type([Type]$type) {
    if ($type.IsByRef) { return (Format-Type $type.GetElementType()) + '&' }
    if ($type.IsPointer) { return (Format-Type $type.GetElementType()) + '*' }
    if ($type.IsArray) { return (Format-Type $type.GetElementType()) + '[]' }
    if ($type.IsGenericParameter) { return $type.Name }
    if ($type.IsGenericType) {
        $definition = $type.GetGenericTypeDefinition().FullName -replace '`[0-9]+', ''
        $arguments = @($type.GetGenericArguments() | ForEach-Object { Format-Type $_ })
        return $definition + '<' + ($arguments -join ', ') + '>'
    }
    if ($type.FullName) { return $type.FullName }
    return $type.Name
}

function Format-Constraints([Type]$parameter) {
    $parts = New-Object 'System.Collections.Generic.List[string]'
    $attrs = $parameter.GenericParameterAttributes
    $special = $attrs -band [System.Reflection.GenericParameterAttributes]::SpecialConstraintMask
    if ($special -band [System.Reflection.GenericParameterAttributes]::ReferenceTypeConstraint) {
        $parts.Add('class')
    }
    if ($special -band [System.Reflection.GenericParameterAttributes]::NotNullableValueTypeConstraint) {
        $parts.Add('struct')
    }
    foreach ($constraint in $parameter.GetGenericParameterConstraints()) {
        $parts.Add((Format-Type $constraint))
    }
    if ($special -band [System.Reflection.GenericParameterAttributes]::DefaultConstructorConstraint) {
        $parts.Add('new()')
    }
    if ($parts.Count -eq 0) { return '' }
    return $parameter.Name + ':' + ($parts -join '&')
}

function Format-Generics([Type[]]$parameters) {
    $parts = @($parameters | Where-Object { $_.IsGenericParameter } |
        ForEach-Object { Format-Constraints $_ } |
        Where-Object { $_ -ne '' })
    if ($parts.Count -eq 0) { return '' }
    return ' where ' + ($parts -join '; ')
}

function Format-Parameter([System.Reflection.ParameterInfo]$parameter) {
    $name = Format-Type $parameter.ParameterType
    if ($parameter.IsOut) { $name = 'out ' + $name }
    if ($parameter.IsIn) { $name = 'in ' + $name }
    if ($parameter.IsDefined([ParamArrayAttribute], $false)) { $name = 'params ' + $name }
    $value = $name + ' ' + $parameter.Name
    if ($parameter.IsOptional) { $value += ' optional' }
    if ($parameter.HasDefaultValue) {
        if ($null -eq $parameter.DefaultValue) { $value += '=null' }
        else {
            $value += '=' + [Convert]::ToString(
                $parameter.DefaultValue, [Globalization.CultureInfo]::InvariantCulture)
        }
    }
    return $value
}

function Format-Parameters([System.Reflection.ParameterInfo[]]$parameters) {
    return (@($parameters | ForEach-Object { Format-Parameter $_ }) -join ', ')
}

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# RecordSift net48 compiled public API baseline; approved via reviewed pull requests.')
$lines.Add('# Generated with scripts/verify_public_api.ps1 from Release binaries.')
$lines.Add('# Framework modernization MUST compare against this baseline; net48-only checks do not certify modernization.')
foreach ($component in $components) {
    $name = 'RecordSift.' + $component
    $assemblyPath = Join-Path $root ('src\' + $name + '\bin\Release\' + $name + '.dll')
    if (-not (Test-Path $assemblyPath)) { throw "Build Release first; assembly missing: $assemblyPath" }
    $assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
    $identity = $assembly.GetName()
    $token = [BitConverter]::ToString($identity.GetPublicKeyToken()).Replace('-', '').ToLowerInvariant()
    if ([string]::IsNullOrEmpty($token)) { $token = 'null' }
    $culture = $identity.CultureName
    if ([string]::IsNullOrEmpty($culture)) { $culture = 'neutral' }
    $lines.Add("assembly $($identity.Name) version=$($identity.Version) culture=$culture publicKeyToken=$token")
    $fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($assemblyPath).FileVersion
    $lines.Add("fileVersion $name $fileVersion")
    $projectPath = Join-Path $root ('src\' + $name + '\' + $name + '.csproj')
    [xml]$project = Get-Content -Raw $projectPath
    $lines.Add("project $name target=$($project.Project.PropertyGroup[0].TargetFrameworkVersion) output=$($project.Project.PropertyGroup[0].OutputType)")
    $config = Join-Path $root ('src\' + $name + '\packages.config')
    if (Test-Path $config) {
        [xml]$packages = Get-Content -Raw $config
        foreach ($package in @($packages.packages.package)) {
            $lines.Add("package $name $($package.id)=$($package.version)")
        }
    }
    $metadata = @(
        'System.Reflection.AssemblyTitleAttribute',
        'System.Reflection.AssemblyDescriptionAttribute',
        'System.Reflection.AssemblyCompanyAttribute',
        'System.Reflection.AssemblyProductAttribute',
        'System.Runtime.Versioning.TargetFrameworkAttribute'
    )
    foreach ($attribute in $assembly.GetCustomAttributesData()) {
        if ($metadata -contains $attribute.AttributeType.FullName) {
            $value = if ($attribute.ConstructorArguments.Count -gt 0) {
                [string]$attribute.ConstructorArguments[0].Value
            } else { '' }
            $lines.Add("metadata $name $($attribute.AttributeType.Name)=$value")
        }
    }
    foreach ($type in $assembly.GetExportedTypes()) {
        $typeName = Format-Type $type
        $kind = if ($type.IsInterface) { 'interface' }
            elseif ($type.IsEnum) { 'enum' }
            elseif ($type.IsValueType) { 'struct' }
            elseif ([MulticastDelegate].IsAssignableFrom($type.BaseType)) { 'delegate' }
            else { 'class' }
        $modifier = if ($type.IsAbstract -and $type.IsSealed) { 'static' }
            elseif ($type.IsAbstract -and -not $type.IsInterface) { 'abstract' }
            elseif ($type.IsSealed -and -not $type.IsValueType -and -not $type.IsEnum) { 'sealed' }
            else { 'ordinary' }
        $lines.Add("type $typeName kind=$kind modifier=$modifier")
        if ($type.BaseType) { $lines.Add("base $typeName : $(Format-Type $type.BaseType)") }
        foreach ($interface in $type.GetInterfaces()) {
            $lines.Add("interface $typeName : $(Format-Type $interface)")
        }
        if ($type.IsGenericTypeDefinition) {
            $lines.Add("generic $typeName$(Format-Generics $type.GetGenericArguments())")
        }
        foreach ($constructor in $type.GetConstructors($flags)) {
            $lines.Add("constructor $typeName($(Format-Parameters $constructor.GetParameters()))")
        }
        foreach ($method in $type.GetMethods($flags)) {
            if ($method.IsSpecialName -and $method.Name -match '^(get_|set_|add_|remove_)') {
                continue
            }
            $scope = if ($method.IsStatic) { 'static' } else { 'instance' }
            $mod = if ($method.IsAbstract) { 'abstract' }
                elseif ($method.IsVirtual) { 'virtual' } else { 'ordinary' }
            $generic = if ($method.IsGenericMethod) {
                '<' + (@($method.GetGenericArguments() | ForEach-Object { $_.Name }) -join ', ') + '>'
            } else { '' }
            $constraints = if ($method.IsGenericMethod) {
                Format-Generics $method.GetGenericArguments()
            } else { '' }
            $lines.Add("method $typeName $scope $mod $(Format-Type $method.ReturnType) $($method.Name)$generic($(Format-Parameters $method.GetParameters()))$constraints")
        }
        foreach ($property in $type.GetProperties($flags)) {
            $getter = if ($property.GetGetMethod()) { 'get' } else { '-' }
            $setter = if ($property.GetSetMethod()) { 'set' } else { '-' }
            $lines.Add("property $typeName $(Format-Type $property.PropertyType) $($property.Name)[$(Format-Parameters $property.GetIndexParameters())] {$getter,$setter}")
        }
        foreach ($field in $type.GetFields($flags)) {
            $mode = if ($field.IsLiteral) { 'const' }
                elseif ($field.IsInitOnly) { 'readonly' } else { 'mutable' }
            $scope = if ($field.IsStatic) { 'static' } else { 'instance' }
            $lines.Add("field $typeName $scope $mode $(Format-Type $field.FieldType) $($field.Name)")
            if ($field.IsLiteral) { $lines.Add("literal $typeName.$($field.Name)=$($field.GetRawConstantValue())") }
        }
        foreach ($event in $type.GetEvents($flags)) {
            $lines.Add("event $typeName $(Format-Type $event.EventHandlerType) $($event.Name)")
        }
    }
}
# Keep fixed header lines; sort all data using ordinal rather than ambient locale.
$data = New-Object 'System.Collections.Generic.List[string]'
foreach ($line in $lines) {
    if (-not $line.StartsWith('#', [StringComparison]::Ordinal)) { $data.Add($line) }
}
$data.Sort([StringComparer]::Ordinal)
$current = @(($lines | Where-Object { $_.StartsWith('#', [StringComparison]::Ordinal) }) + $data.ToArray())
if ($PrintCurrent) { $current | ForEach-Object { Write-Host $_ }; return }
if (-not (Test-Path $baselinePath)) {
    Write-Host 'Approved baseline is absent. Review the following compiled net48 surface:'
    $current | ForEach-Object { Write-Host $_ }
    throw "NFR-010 baseline not approved: $baselinePath"
}
$expected = @(Get-Content $baselinePath -Encoding UTF8)
$changes = @(Compare-Object -ReferenceObject $expected -DifferenceObject $current)
if ($changes.Count -gt 0) {
    Write-Host 'PUBLIC API BASELINE DIFFERENCES (<= approved baseline; => current binaries):'
    $changes | ForEach-Object { Write-Host ('  ' + $_.SideIndicator + ' ' + $_.InputObject) }
    throw 'NFR-010 public API or package metadata changed; approve updates in a reviewed PR.'
}
Write-Host "NFR-010-SC01: verified $($data.Count) compiled public API and package metadata entries against approved net48 baseline."
