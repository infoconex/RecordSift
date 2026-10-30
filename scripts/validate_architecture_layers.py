#!/usr/bin/env python3
"""Verify physical RecordSift domain/application/adapter layers and project paths.

Uses only Python's standard library; does not run or load third-party parsers.
"""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
CORE = ROOT / "src" / "RecordSift"
ADAPTER = ROOT / "src" / "RecordSift.CsvHelperAdapter"

# Keep this existing-module location map under review when changing the public
# architectural ownership. New source files must also go into an owned layer.
LAYERS = {
    "Domain/Diagnostics": (
        "DiagnosticCodes", "DiagnosticScope", "DomainMessageResources",
        "ImportDiagnostic",
    ),
    "Domain/Schema": (
        "ExpectedColumn", "ImportSchema", "SchemaBindingResult",
        "SourceColumnBinding", "SourceColumnPosition",
    ),
    "Domain/Results": (
        "ImportResult", "ImportStatus", "ImportSummary",
    ),
    "Application/Abstractions/Csv": (
        "ICsvRecordReader", "CsvReadOptions", "CsvParseResult", "CsvRecord",
    ),
    "Application/Importing/Csv": (
        "CsvDataRow", "CsvHeaderPolicy", "CsvTableReader", "CsvTableResult",
    ),
}
RESOURCE = "Domain/Diagnostics/Resources/DomainMessages.resx"
RESOURCE_NAME = "RecordSift.Resources.DomainMessages.resources"
ADAPTER_IMPLEMENTATION = "Infrastructure/CsvHelper/CsvHelperRecordReader.cs"


def project_items(project, local_tag):
    root = ET.parse(project).getroot()
    return [
        element.attrib.get("Include", "").replace("\\", "/")
        for element in root.iter()
        if element.tag.rsplit("}", 1)[-1] == local_tag
    ]


def check_layout():
    failures = []
    expected = {"Properties/AssemblyInfo.cs"}
    for folder, filenames in LAYERS.items():
        for filename in filenames:
            expected.add(f"{folder}/{filename}.cs")
    actual = {
        str(path.relative_to(CORE)).replace("\\", "/")
        for path in CORE.rglob("*.cs")
    }
    for missing in sorted(expected - actual):
        failures.append(f"Missing core layer source: {missing}")
    for extra in sorted(actual - expected):
        # Future layer types may be added without extending the owned module map,
        # but no C# source may remain loose at the core project root.
        if not (extra.startswith("Domain/") or extra.startswith("Application/")):
            failures.append(f"Core source outside owned layers: {extra}")
    core_includes = project_items(CORE / "RecordSift.csproj", "Compile")
    for missing in sorted(actual - set(core_includes)):
        failures.append(f"Core C# not compiled by csproj: {missing}")
    for missing in sorted(set(core_includes) - actual):
        failures.append(f"Missing declared core Compile item: {missing}")
    if len(core_includes) != len(set(core_includes)):
        failures.append("Duplicate core Compile item")

    adapter_cs = project_items(ADAPTER / "RecordSift.CsvHelperAdapter.csproj", "Compile")
    if adapter_cs != [ADAPTER_IMPLEMENTATION]:
        failures.append("Vendor adapter implementation must be in Infrastructure/CsvHelper")
    for item in adapter_cs:
        if not (ADAPTER / item).is_file():
            failures.append(f"Adapter missing declared source: {item}")

    resources = project_items(CORE / "RecordSift.csproj", "EmbeddedResource")
    if resources != [RESOURCE]:
        failures.append("Domain messages .resx must live inside Domain/Diagnostics/Resources")
    if not (CORE / RESOURCE).is_file():
        failures.append("Domain messages resource missing")
    root = ET.parse(CORE / "RecordSift.csproj").getroot()
    resource_elements = [
        element for element in root.iter()
        if element.tag.rsplit("}", 1)[-1] == "EmbeddedResource"
    ]
    logical_names = [
        child.text for element in resource_elements
        for child in element
        if child.tag.rsplit("}", 1)[-1] == "LogicalName"
    ]
    if logical_names != [RESOURCE_NAME]:
        failures.append("Embedded manifest resource identity changed")

    # Domain logic must not import physical IO, application policies or vendor
    # packages; the application may depend on domain and owned abstractions only.
    forbidden_domain = re.compile(
        r"using\s+(?:System\.IO|CsvHelper|RecordSift\.Adapters)\b|"
        r"\b(?:TextReader|StreamReader|ICsvRecordReader|CsvTableReader)\b"
    )
    forbidden_application = re.compile(
        r"using\s+(?:CsvHelper|RecordSift\.Adapters)\b|"
        r"\b(?:CsvHelperRecordReader|CsvConfiguration)\b"
    )
    for layer, pattern in (("Domain", forbidden_domain), ("Application", forbidden_application)):
        for path in (CORE / layer).rglob("*.cs"):
            source = path.read_text(encoding="utf-8")
            if pattern.search(source):
                failures.append(f"Forbidden outward dependency in {path.relative_to(ROOT)}")
            if not re.search(r"\bnamespace\s+RecordSift\s*\{", source):
                failures.append(f"Existing public namespace changed in {path.relative_to(ROOT)}")

    adapter_project = (ADAPTER / "RecordSift.CsvHelperAdapter.csproj").read_text(
        encoding="utf-8"
    )
    core_project = (CORE / "RecordSift.csproj").read_text(encoding="utf-8")
    if "RecordSift.CsvHelperAdapter" in core_project or "CsvHelper" in core_project:
        failures.append("Core cannot reference vendor adapter")
    if "RecordSift.csproj" not in adapter_project:
        failures.append("Adapter must reference inward core assembly")
    return failures


def self_test():
    assert re.search(r"\bTextReader\b", "public void Read(TextReader source)")
    assert not re.search(r"\bTextReader\b", "public class ImportSchema")
    assert "Domain/Schema/ImportSchema.cs".startswith("Domain/")
    assert not "ImportSchema.cs".startswith("Domain/")


def main():
    self_test()
    failures = check_layout()
    if failures:
        print("Layer boundary verification failed:")
        for failure in failures:
            print(" - " + failure)
        return 1
    print("Layer boundary verification passed: core domain/application owners, "
          "reader ports, isolated infrastructure, explicit project Compile items "
          "and stable resource manifest identity.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
