#!/usr/bin/env python3
"""Protect actual Domain -> Application -> Infrastructure assembly boundaries.

No third-party packages are needed for this CI verification. Check both
file placement and actual traditional MSBuild ProjectReferences.
"""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
DOMAIN = ROOT / "src" / "RecordSift.Domain"
APPLICATION = ROOT / "src" / "RecordSift.Application"
INFRASTRUCTURE = ROOT / "src" / "RecordSift.Infrastructure.Csv"

EXPECTED = {
    "RecordSift.Domain": {
        "Diagnostics": ("DiagnosticCodes", "DiagnosticScope", "DomainMessageResources", "ImportDiagnostic"),
        "Schema": ("ExpectedColumn", "ImportSchema", "SchemaBindingResult",
                   "SourceColumnBinding", "SourceColumnPosition"),
        "Results": ("ImportResult", "ImportStatus", "ImportSummary"),
    },
    "RecordSift.Application": {
        "Ports/Csv": ("ICsvRecordReader", "CsvReadOptions", "CsvParseResult", "CsvRecord"),
        "Importing/Csv": ("CsvDataRow", "CsvHeaderPolicy", "CsvTableReader", "CsvTableResult"),
        "Messages": ("CsvMessageResources",),
    },
}
TEST_PROJECT_REFERENCES = {
    "RecordSift.Domain.Tests": ["RecordSift.Domain.csproj"],
    "RecordSift.Application.Tests": [
        "RecordSift.Application.csproj", "RecordSift.Domain.csproj",
    ],
    "RecordSift.Infrastructure.Csv.Tests": [
        "RecordSift.Application.Tests.csproj", "RecordSift.Domain.csproj",
        "RecordSift.Application.csproj", "RecordSift.Infrastructure.Csv.csproj",
    ],
}

def verify_test_dependency_graph(graph):
    return [f"{name}: test dependencies {sorted(graph.get(name, []))} "
            f"must be {sorted(expected)}"
            for name, expected in TEST_PROJECT_REFERENCES.items()
            if sorted(graph.get(name, [])) != sorted(expected)]

DOMAIN_RESX = "Diagnostics/Resources/DomainMessages.resx"
RESX_IDENTITY = "RecordSift.Resources.DomainMessages.resources"
ADAPTER_SOURCE = "CsvRecordReaderAdapter.cs"
ADAPTER_FILES = (
    "CsvRecordReaderAdapter.cs",
    "CharacterLimitedTextReader.cs",
    "MalformedCsvException.cs",
    "CharacterLimitExceededException.cs",
)

def verify_adapter_source_ownership(sources):
    errors = []
    for file, expected_type in (
        ("CsvRecordReaderAdapter.cs", "CsvRecordReaderAdapter"),
        ("CharacterLimitedTextReader.cs", "CharacterLimitedTextReader"),
        ("MalformedCsvException.cs", "MalformedCsvException"),
        ("CharacterLimitExceededException.cs", "CharacterLimitExceededException"),
    ):
        source = sources.get(file)
        if source is None:
            errors.append(f"Missing focused infrastructure source {file}")
            continue
        declared = re.findall(r"\b(?:public|internal)\s+sealed\s+class\s+(\w+)", source)
        if declared != [expected_type]:
            errors.append(f"{file}: expected one owned class {expected_type}, got {declared}")
        errors.extend(verify_namespace(source, "RecordSift.Infrastructure.Csv", file))
    return errors


def project(path):
    return ET.parse(path).getroot()

def items(root, tag):
    return [element for element in root.iter()
            if element.tag.rsplit("}", 1)[-1] == tag]

def includes(root, tag):
    return [element.attrib["Include"].replace("\\", "/") for element in items(root, tag)]

def dependencies(root):
    # A traditional project uses explicit ProjectReference, not NuGet package
    # abstractions, so the direction can be enforced without compiling.
    return [Path(item).name for item in includes(root, "ProjectReference")]

def verify_dependency_graph(graph):
    correct = {
        "RecordSift.Domain": [],
        "RecordSift.Application": ["RecordSift.Domain.csproj"],
        "RecordSift.Infrastructure.Csv": [
            "RecordSift.Application.csproj", "RecordSift.Domain.csproj",
        ],
    }
    return [f"{project}: dependencies {sorted(graph.get(project, []))} "
            f"must be {sorted(expected)}"
            for project, expected in correct.items()
            if sorted(graph.get(project, [])) != sorted(expected)]

def verify_namespace(source, expected, location):
    """Enforce a single declared namespace matching its assembly and responsibility."""
    declared = re.findall(r"(?m)^\s*namespace\s+([A-Za-z_][\w.]*)\s*\{", source)
    return [] if declared == [expected] else [
        f"{location}: namespaces {declared} must be [{expected}]"
    ]


CORE_FRAMEWORK_REFERENCES = {
    "RecordSift.Domain": {"System", "System.Core"},
    "RecordSift.Application": {"System", "System.Core"},
}
CORE_ALLOWED_IMPORT = r"$(MSBuildToolsPath)\Microsoft.CSharp.targets"

def verify_core_dependency_metadata(root, project_name):
    """Allow only approved .NET Framework assembly references and the MSBuild C# target.

    ProjectReferences are independently checked by verify_dependency_graph.
    Do not accept a vendor DLL under a System-prefixed assembly or a disguised
    HintPath/Import/PackageReference/Analyzer.
    """
    errors = []
    allowed = CORE_FRAMEWORK_REFERENCES[project_name]
    for reference in items(root, "Reference"):
        identity = reference.attrib.get("Include", "")
        if identity not in allowed:
            errors.append(f"{project_name}: unauthorized assembly reference {identity}")
        if items(reference, "HintPath"):
            errors.append(f"{project_name}: explicit assembly HintPath is forbidden")
    for tag in ("PackageReference", "PackageDownload", "PackageVersion",
                "Analyzer", "COMReference", "COMFileReference",
                "ReferencePath", "UsingTask", "NuGetPackageImportStamp"):
        if items(root, tag):
            errors.append(f"{project_name}: forbidden dependency metadata {tag}")
    for item in items(root, "Import"):
        target = item.attrib.get("Project", "")
        if target != CORE_ALLOWED_IMPORT:
            errors.append(f"{project_name}: unauthorized build import {target}")
    if root.attrib.get("Sdk"):
        errors.append(f"{project_name}: unexpected SDK project import")
    for tag in ("None", "Content", "EmbeddedResource"):
        for item in items(root, tag):
            if item.attrib.get("Include", "").replace("\\", "/").lower().endswith("packages.config"):
                errors.append(f"{project_name}: package manifest is forbidden")
    for tag in ("RestoreProjectStyle", "RestoreSources",
                "RestoreAdditionalProjectSources", "RestorePackagesPath",
                "RestoreAdditionalProjectFallbackFolders"):
        if items(root, tag):
            errors.append(f"{project_name}: forbidden package restore metadata {tag}")
    return errors


def verify_layout():
    errors = []
    if (ROOT / "src" / "RecordSift" / "RecordSift.csproj").exists():
        errors.append("Legacy mixed domain/application project still exists")
    if (ROOT / "src" / "RecordSift.CsvHelperAdapter").exists():
        errors.append("Legacy vendor project does not identify the Infrastructure layer")
    if (ROOT / "tests" / "RecordSift.CsvHelperAdapter.Tests").exists():
        errors.append("Legacy vendor test project does not identify the Infrastructure layer")
    if (APPLICATION / "Abstractions").exists():
        errors.append("Legacy generic Application Abstractions folder must be named Ports")
    graph = {}
    for name in EXPECTED:
        location = ROOT / "src" / name
        project_path = location / (name + ".csproj")
        if not project_path.is_file():
            errors.append(f"Missing layer project: {project_path.relative_to(ROOT)}")
            continue
        root = project(project_path)
        graph[name] = dependencies(root)
        expected_files = {"Properties/AssemblyInfo.cs"}
        for group, filenames in EXPECTED[name].items():
            expected_files.update(f"{group}/{filename}.cs" for filename in filenames)
        actual = {str(path.relative_to(location)).replace("\\", "/")
                  for path in location.rglob("*.cs")}
        declared = includes(root, "Compile")
        if set(declared) != actual or len(declared) != len(set(declared)):
            errors.append(f"{name}: MSBuild Compile entries must match physical C# files exactly")
        for missing in sorted(expected_files - actual):
            errors.append(f"{name}: missing expected layer file {missing}")
        for path in sorted(actual - expected_files):
            errors.append(f"{name}: unexpected source file (assign explicit owner) {path}")
        errors.extend(verify_core_dependency_metadata(root, name))
        for manifest in location.rglob("packages.config"):
            errors.append(f"{name}: forbidden external package manifest {manifest.relative_to(ROOT)}")
        csproj_text = project_path.read_text(encoding="utf-8")
        if "CsvHelper" in csproj_text:
            errors.append(f"{name}: application/domain layer references CsvHelper")
        for file in location.rglob("*.cs"):
            if file.name == "AssemblyInfo.cs":
                continue
            source = file.read_text(encoding="utf-8")
            group = file.relative_to(location).parent.as_posix().replace("/", ".")
            expected_namespace = f"{name}.{group}"
            errors.extend(verify_namespace(source, expected_namespace,
                                           str(file.relative_to(ROOT))))
            if name == "RecordSift.Domain" and re.search(
                r"using\s+System\.IO\b|using\s+RecordSift\.Adapters\b|"
                r"\b(?:TextReader|StreamReader|ICsvRecordReader|CsvTableReader)\b", source
            ):
                errors.append(f"{file.relative_to(ROOT)}: outward dependency in Domain")
            if name == "RecordSift.Application" and re.search(
                r"using\s+CsvHelper\b|using\s+RecordSift\.Adapters\b|"
                r"\b(?:CsvConfiguration|CsvRecordReaderAdapter)\b", source
            ):
                errors.append(f"{file.relative_to(ROOT)}: infrastructure reference in Application")
        root_namespaces = [child.text for child in root.iter()
                           if child.tag.rsplit("}", 1)[-1] == "RootNamespace"]
        if root_namespaces != [name]:
            errors.append(f"{name}: root namespace must match its assembly")
        assembly_names = re.findall(
            r"<AssemblyName>([^<]+)</AssemblyName>", csproj_text
        )
        if assembly_names != [name]:
            errors.append(f"{name}: wrong CLR assembly identity")
    adapter_project = INFRASTRUCTURE / "RecordSift.Infrastructure.Csv.csproj"
    if adapter_project.is_file():
        root = project(adapter_project)
        graph["RecordSift.Infrastructure.Csv"] = dependencies(root)
        if set(includes(root, "Compile")) != set(ADAPTER_FILES) or len(includes(root, "Compile")) != len(ADAPTER_FILES):
            errors.append("Infrastructure project must explicitly compile the four focused CSV sources")
        sources = {file: (INFRASTRUCTURE / file).read_text(encoding="utf-8")
                   for file in ADAPTER_FILES if (INFRASTRUCTURE / file).is_file()}
        errors.extend(verify_adapter_source_ownership(sources))
        actual_source = {path.name for path in INFRASTRUCTURE.glob("*.cs")}
        if actual_source != set(ADAPTER_FILES):
            errors.append("Infrastructure source ownership mismatch")
        if not (INFRASTRUCTURE / ADAPTER_SOURCE).is_file():
            errors.append("CsvHelper implementation source missing")
        else:
            adapter_source = (INFRASTRUCTURE / ADAPTER_SOURCE).read_text(encoding="utf-8")
            if not re.search(r"namespace\s+RecordSift\.Infrastructure\.Csv\s*\{", adapter_source):
                errors.append("Infrastructure CsvHelper implementation namespace is inconsistent")
        root_namespaces = [child.text for child in root.iter()
                           if child.tag.rsplit("}", 1)[-1] == "RootNamespace"]
        if root_namespaces != ["RecordSift.Infrastructure.Csv"]:
            errors.append("Infrastructure root namespace must identify CSV integration")
        assembly_names = [child.text for child in root.iter()
                          if child.tag.rsplit("}", 1)[-1] == "AssemblyName"]
        if assembly_names != ["RecordSift.Infrastructure.Csv"]:
            errors.append("Infrastructure CLR assembly name must include its layer")

    else:
        errors.append("Infrastructure project missing")
    errors.extend(verify_dependency_graph(graph))

    resource_project = DOMAIN / "RecordSift.Domain.csproj"
    if resource_project.is_file():
        resources = items(project(resource_project), "EmbeddedResource")
        if len(resources) != 1 or resources[0].attrib.get("Include", "").replace("\\", "/") != DOMAIN_RESX:
            errors.append("Domain .resx must be embedded by its physical location")
        elif not (DOMAIN / DOMAIN_RESX).is_file():
            errors.append("Missing Domain .resx file")
        elif [entry.text for entry in resources[0]
              if entry.tag.rsplit("}", 1)[-1] == "LogicalName"] != [RESX_IDENTITY]:
            errors.append("Domain diagnostic resource manifest identity changed")
    application_resources = APPLICATION / "RecordSift.Application.csproj"
    if application_resources.is_file():
        resx = items(project(application_resources), "EmbeddedResource")
        expected_resource = "Messages/Resources/CsvMessages.resx"
        if len(resx) != 1 or resx[0].attrib.get("Include", "").replace("\\", "/") != expected_resource:
            errors.append("Application CSV .resx must be embedded at its physical location")
        elif not (APPLICATION / expected_resource).is_file():
            errors.append("Missing Application CSV .resx")
        elif [entry.text for entry in resx[0]
              if entry.tag.rsplit("}", 1)[-1] == "LogicalName"] != [
                  "RecordSift.Resources.CsvMessages.resources"]:
            errors.append("Application CSV resource manifest identity changed")
    # Verify exact test boundaries, including source ownership and dependencies.
    test_graph = {}
    for project_name in TEST_PROJECT_REFERENCES:
        path = ROOT / "tests" / project_name / (project_name + ".csproj")
        if not path.is_file():
            errors.append(f"Missing test project {project_name}")
            continue
        root = project(path)
        test_graph[project_name] = dependencies(root)
        root_namespaces = [child.text for child in items(root, "RootNamespace")]
        assembly_names = [child.text for child in items(root, "AssemblyName")]
        if root_namespaces != [project_name] or assembly_names != [project_name]:
            errors.append(f"{project_name}: wrong root namespace or assembly identity")
        actual = {p.relative_to(path.parent).as_posix() for p in path.parent.rglob("*.cs")}
        declared = includes(root, "Compile")
        if set(declared) != actual or len(declared) != len(set(declared)):
            errors.append(f"{project_name}: test Compile entries must match physical sources")
        for source in path.parent.rglob("*.cs"):
            if source.name == "AssemblyInfo.cs":
                continue
            data = source.read_text(encoding="utf-8")
            errors.extend(verify_namespace(data, project_name,
                                           str(source.relative_to(ROOT))))
            if project_name == "RecordSift.Domain.Tests" and re.search(
                r"using\s+(?:RecordSift\.Application|RecordSift\.Infrastructure|CsvHelper)\b", data
            ):
                errors.append(f"{source.relative_to(ROOT)}: forbidden Domain test import")
            if project_name == "RecordSift.Application.Tests" and re.search(
                r"using\s+(?:RecordSift\.Infrastructure|CsvHelper)\b", data
            ):
                errors.append(f"{source.relative_to(ROOT)}: forbidden Application test import")
        if project_name != "RecordSift.Infrastructure.Csv.Tests":
            allowed = {"System", "System.Core", "nunit.framework"}
            for item in items(root, "Reference"):
                assembly = item.attrib.get("Include", "").split(",", 1)[0]
                if assembly not in allowed:
                    errors.append(f"{project_name}: forbidden test assembly reference {assembly}")
    errors.extend(verify_test_dependency_graph(test_graph))
    if (ROOT / "tests" / "RecordSift.Tests").exists():
        errors.append("Legacy mixed Domain/Application test project must not exist")
    return errors

def self_test():
    valid = {
        "RecordSift.Domain": [],
        "RecordSift.Application": ["RecordSift.Domain.csproj"],
        "RecordSift.Infrastructure.Csv": [
            "RecordSift.Domain.csproj", "RecordSift.Application.csproj",
        ],
    }
    assert verify_dependency_graph(valid) == [], "Valid graph rejected"
    assert verify_namespace("namespace RecordSift.Domain.Schema\n{ }",
                            "RecordSift.Domain.Schema", "valid") == [], "Valid namespace rejected"
    assert verify_namespace("namespace RecordSift\n{ }",
                            "RecordSift.Domain.Schema", "invalid"), "Flat namespace was accepted"
    assert verify_namespace("namespace RecordSift.Application.Importing.Csv\n{ }",
                            "RecordSift.Domain.Schema", "outward"), "Wrong layer namespace was accepted"
    assert verify_namespace("namespace RecordSift.Application.Abstractions.Csv\n{ }",
                            "RecordSift.Application.Ports.Csv", "legacy"), "Old port namespace was accepted"
    clean_core = ET.fromstring(
        '<Project><Reference Include="System" />'
        '<Reference Include="System.Core" />'
        '<Import Project="$(MSBuildToolsPath)\\Microsoft.CSharp.targets" /></Project>')
    assert verify_core_dependency_metadata(clean_core, "RecordSift.Domain") == [], \
        "Approved framework references rejected"
    forbidden_core_inputs = {
        "vendor reference": '<Reference Include="CsvVendor" />',
        "fake framework reference": '<Reference Include="System.Fake" />',
        "vendor hint path": '<Reference Include="System"><HintPath>packages/Vendor.dll</HintPath></Reference>',
        "nuget package reference": '<PackageReference Include="Vendor" Version="1.0.0" />',
        "nuget download": '<PackageDownload Include="Vendor" Version="[1.0.0]" />',
        "vendor build import": '<Import Project="packages/Vendor/build.targets" />',
        "package manifest": '<None Include="packages.config" />',
        "vendor analyzer": '<Analyzer Include="packages/Vendor/analyzers/a.dll" />',
    }
    for label, markup in forbidden_core_inputs.items():
        root = ET.fromstring("<Project>" + markup + "</Project>")
        for owner in ("RecordSift.Domain", "RecordSift.Application"):
            assert verify_core_dependency_metadata(root, owner), \
                f"{owner} accepted {label}"
    focused = {name: "namespace RecordSift.Infrastructure.Csv\n{ internal sealed class " +
               name[:-3] + " { } }" for name in ADAPTER_FILES}
    assert not verify_adapter_source_ownership(focused), "Focused adapter layout rejected"
    missing = {key: value for key, value in focused.items()
               if key != "CharacterLimitedTextReader.cs"}
    assert verify_adapter_source_ownership(missing), "Missing reader source accepted"
    mixed = dict(focused)
    mixed["CsvRecordReaderAdapter.cs"] = focused["CsvRecordReaderAdapter.cs"].replace(
        " { }", " { } internal sealed class CharacterLimitedTextReader { }")
    assert verify_adapter_source_ownership(mixed), "Multiple classes in adapter accepted"
    legal_tests = {name: list(refs) for name, refs in TEST_PROJECT_REFERENCES.items()}
    assert verify_test_dependency_graph(legal_tests) == [], "Valid test graph rejected"
    broken_tests = {**legal_tests, "RecordSift.Domain.Tests": [
        "RecordSift.Domain.csproj", "RecordSift.Application.csproj",
    ]}
    assert verify_test_dependency_graph(broken_tests), "Domain test outward reference accepted"
    broken_tests = {**legal_tests, "RecordSift.Application.Tests": [
        "RecordSift.Domain.csproj", "RecordSift.Infrastructure.Csv.csproj",
    ]}
    assert verify_test_dependency_graph(broken_tests), "Application test infrastructure accepted"
    broken_tests = {**legal_tests, "RecordSift.Infrastructure.Csv.Tests": [
        "RecordSift.Domain.Tests.csproj",
    ]}
    assert verify_test_dependency_graph(broken_tests), "Adapter test depends on Domain tests"
    broken = {**valid, "RecordSift.Domain": ["RecordSift.Application.csproj"]}
    assert any("RecordSift.Domain" in error for error in verify_dependency_graph(broken)), \
        "Forbidden Domain->Application dependency was not detected"
    broken = {**valid, "RecordSift.Application": ["RecordSift.Infrastructure.Csv.csproj"]}
    assert any("RecordSift.Application" in error for error in verify_dependency_graph(broken)), \
        "Forbidden Application->Infrastructure dependency was not detected"

if __name__ == "__main__":
    self_test()
    violations = verify_layout()
    if violations:
        print("Architecture boundaries FAILED:")
        for problem in violations:
            print(" - " + problem)
        sys.exit(1)
    print("Domain -> Application -> CsvHelper infrastructure boundaries verified; "
          "traditional project references, resources, test isolation and file ownership pass.")
