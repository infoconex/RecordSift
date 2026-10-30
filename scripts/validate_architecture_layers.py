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


def verify_core_dependency_metadata(root, project_name):
    """Reject any non-framework dependency metadata in Domain/Application."""
    # RED: acceptance guard is introduced before a working implementation.
    return []


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
        for element in items(root, "Reference"):
            reference = element.attrib.get("Include", "")
            if name == "RecordSift.Domain" and not reference.startswith("System"):
                errors.append(f"Domain cannot reference an external package/assembly: {reference}")
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
        if includes(root, "Compile") != [ADAPTER_SOURCE]:
            errors.append("CsvHelper implementation must live in the infrastructure adapter project")
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
