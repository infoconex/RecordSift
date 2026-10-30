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
INFRASTRUCTURE = ROOT / "src" / "RecordSift.Infrastructure.CsvHelper"

EXPECTED = {
    "RecordSift.Domain": {
        "Diagnostics": ("DiagnosticCodes", "DiagnosticScope", "DomainMessageResources", "ImportDiagnostic"),
        "Schema": ("ExpectedColumn", "ImportSchema", "SchemaBindingResult",
                   "SourceColumnBinding", "SourceColumnPosition"),
        "Results": ("ImportResult", "ImportStatus", "ImportSummary"),
    },
    "RecordSift.Application": {
        "Abstractions/Csv": ("ICsvRecordReader", "CsvReadOptions", "CsvParseResult", "CsvRecord"),
        "Importing/Csv": ("CsvDataRow", "CsvHeaderPolicy", "CsvTableReader", "CsvTableResult"),
    },
}
DOMAIN_RESX = "Diagnostics/Resources/DomainMessages.resx"
RESX_IDENTITY = "RecordSift.Resources.DomainMessages.resources"
ADAPTER_SOURCE = "CsvHelperRecordReader.cs"

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
        "RecordSift.Infrastructure.CsvHelper": [
            "RecordSift.Application.csproj", "RecordSift.Domain.csproj",
        ],
    }
    return [f"{project}: dependencies {sorted(graph.get(project, []))} "
            f"must be {sorted(expected)}"
            for project, expected in correct.items()
            if sorted(graph.get(project, [])) != sorted(expected)]

def verify_layout():
    errors = []
    if (ROOT / "src" / "RecordSift" / "RecordSift.csproj").exists():
        errors.append("Legacy mixed domain/application project still exists")
    if (ROOT / "src" / "RecordSift.CsvHelperAdapter").exists():
        errors.append("Legacy vendor project does not identify the Infrastructure layer")
    if (ROOT / "tests" / "RecordSift.CsvHelperAdapter.Tests").exists():
        errors.append("Legacy vendor test project does not identify the Infrastructure layer")
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
        # Legacy namespace is deliberately retained for source compatibility
        # while CLR assembly/project ownership is strictly split.
        for file in location.rglob("*.cs"):
            if file.name == "AssemblyInfo.cs":
                continue
            source = file.read_text(encoding="utf-8")
            if not re.search(r"\bnamespace\s+RecordSift\s*\{", source):
                errors.append(f"{file.relative_to(ROOT)}: public namespace changed")
            if name == "RecordSift.Domain" and re.search(
                r"using\s+System\.IO\b|using\s+RecordSift\.Adapters\b|"
                r"\b(?:TextReader|StreamReader|ICsvRecordReader|CsvTableReader)\b", source
            ):
                errors.append(f"{file.relative_to(ROOT)}: outward dependency in Domain")
            if name == "RecordSift.Application" and re.search(
                r"using\s+CsvHelper\b|using\s+RecordSift\.Adapters\b|"
                r"\b(?:CsvConfiguration|CsvHelperRecordReader)\b", source
            ):
                errors.append(f"{file.relative_to(ROOT)}: infrastructure reference in Application")
        assembly_names = re.findall(
            r"<AssemblyName>([^<]+)</AssemblyName>", csproj_text
        )
        if assembly_names != [name]:
            errors.append(f"{name}: wrong CLR assembly identity")
    adapter_project = INFRASTRUCTURE / "RecordSift.Infrastructure.CsvHelper.csproj"
    if adapter_project.is_file():
        root = project(adapter_project)
        graph["RecordSift.Infrastructure.CsvHelper"] = dependencies(root)
        if includes(root, "Compile") != [ADAPTER_SOURCE]:
            errors.append("CsvHelper implementation must live in the infrastructure adapter project")
        if not (INFRASTRUCTURE / ADAPTER_SOURCE).is_file():
            errors.append("CsvHelper implementation source missing")
        else:
            adapter_source = (INFRASTRUCTURE / ADAPTER_SOURCE).read_text(encoding="utf-8")
            if not re.search(r"namespace\s+RecordSift\.Infrastructure\.CsvHelper\s*\{", adapter_source):
                errors.append("Infrastructure CsvHelper implementation namespace is inconsistent")
        assembly_names = [child.text for child in root.iter()
                          if child.tag.rsplit("}", 1)[-1] == "AssemblyName"]
        if assembly_names != ["RecordSift.Infrastructure.CsvHelper"]:
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
    # Check test project ownership at the assembly level too.
    for project_name, expected_refs in {
        "RecordSift.Tests": ["RecordSift.Domain.csproj", "RecordSift.Application.csproj"],
        "RecordSift.Infrastructure.CsvHelper.Tests": [
            "RecordSift.Tests.csproj", "RecordSift.Domain.csproj",
            "RecordSift.Application.csproj", "RecordSift.Infrastructure.CsvHelper.csproj",
        ],
    }.items():
        path = ROOT / "tests" / project_name / (project_name + ".csproj")
        if not path.is_file():
            errors.append(f"Missing test project {project_name}")
            continue
        references = dependencies(project(path))
        if sorted(references) != sorted(expected_refs):
            errors.append(f"{project_name}: invalid test dependencies {references}")
    return errors

def self_test():
    valid = {
        "RecordSift.Domain": [],
        "RecordSift.Application": ["RecordSift.Domain.csproj"],
        "RecordSift.Infrastructure.CsvHelper": [
            "RecordSift.Domain.csproj", "RecordSift.Application.csproj",
        ],
    }
    assert verify_dependency_graph(valid) == [], "Valid graph rejected"
    broken = {**valid, "RecordSift.Domain": ["RecordSift.Application.csproj"]}
    assert any("RecordSift.Domain" in error for error in verify_dependency_graph(broken)), \
        "Forbidden Domain->Application dependency was not detected"
    broken = {**valid, "RecordSift.Application": ["RecordSift.Infrastructure.CsvHelper.csproj"]}
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
