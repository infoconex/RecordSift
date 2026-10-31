#!/usr/bin/env python3
"""Enforce stable domain messages and reject embedded production prose.

Runs with the Python standard library on Linux; no .NET or parser package needed.
This is intentionally a narrow guard, not a general C# lexer/analyzer.
"""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
CORE = ROOT / "src" / "RecordSift.Domain"
RESX = CORE / "Diagnostics" / "Resources" / "DomainMessages.resx"

errors = []
root = ET.parse(RESX).getroot()
entries = {}
for item in root.findall("data"):
    key = item.attrib.get("name", "")
    value = item.findtext("value")
    if key in entries:
        errors.append("Duplicate resource: " + key)
    if not value or not value.strip():
        errors.append("Empty resource: " + key)
    entries[key] = value

codes = (CORE / "Diagnostics" / "DiagnosticCodes.cs").read_text(encoding="utf-8")
defined_codes = re.findall(r'public const string \w+ = "([A-Z_]+)";', codes)
for code in defined_codes:
    if code not in entries:
        errors.append("Missing diagnostic resource: " + code)

# Typed contract policy: the public catalog exposes domain-owned named accessors;
# raw .resx keys remain implementation details of DomainMessageResources.
CATALOG = CORE / "Diagnostics" / "DomainMessageResources.cs"
APPLICATION = ROOT / "src" / "RecordSift.Application"
argument_keys = {key for key in entries if key.startswith("Argument_")}
APPLICATION_CSV_ARGUMENT_KEYS = {
    "Argument_ReaderHeaderConflict",
    "Argument_HeaderlessMissingPositions",
    "Argument_FailedTableNoError",
    "Argument_FailedTableNonSourceError",
    "Argument_ParserErrorScope",
}
APPLICATION_CSV_OUTCOME_KEYS = {
    "Outcome_ImportCompleted",
    "Outcome_ImportCompletedWithErrors",
    "Outcome_ImportNoDataRecords",
    "Outcome_ImportEmptySource",
    "Outcome_ImportMissingHeader",
    "Outcome_ImportFailed",
    "Outcome_ImportStopped",
    "Outcome_ImportCanceled",
}
APPLICATION_CSV_RESX = APPLICATION / "Messages" / "Resources" / "CsvMessages.resx"
if argument_keys.intersection(APPLICATION_CSV_ARGUMENT_KEYS):
    errors.append("Domain resources contain Application-owned CSV validation messages")

application_entries = {}
if not APPLICATION_CSV_RESX.is_file():
    errors.append("Application-owned CSV message resource manifest is missing")
else:
    application_root = ET.parse(APPLICATION_CSV_RESX).getroot()
    for item in application_root.findall("data"):
        key = item.attrib.get("name", "")
        value = item.findtext("value")
        if key in application_entries or not value or not value.strip():
            errors.append("Duplicate or empty Application resource: " + key)
        application_entries[key] = value
    if set(application_entries) != APPLICATION_CSV_ARGUMENT_KEYS | APPLICATION_CSV_OUTCOME_KEYS:
        errors.append("Application CSV argument and outcome resource inventory changed")
if set(entries).intersection(application_entries):
    errors.append("Domain and Application resource catalogs must have disjoint keys")

public_catalog = CATALOG.read_text(encoding="utf-8")

def raw_argument_uses(source):
    return re.findall(r'(?:DomainMessageResources|CsvMessageResources)\.Get\("(Argument_\w+)"\)', source)

def validate_typed_message_contract(catalog, sources):
    violations = []
    if re.search(r'public\s+static\s+string\s+Get\s*\(\s*string', catalog):
        violations.append("Raw resource-key Get(string) must not be public")
    properties = dict(re.findall(
        r'public\s+static\s+string\s+(\w+)\s*=>\s*Get\("([^"]+)"\)\s*;', catalog))
    declared = set(properties.values())
    if declared != argument_keys or len(properties) != len(argument_keys):
        violations.append("Named argument accessors must cover each Argument_ resource exactly once")
    if any(property_name != key[len("Argument_"):] for property_name, key in properties.items()):
        violations.append("Each named argument accessor must match its Argument_ resource")
    for where, source in sources:
        if raw_argument_uses(source):
            violations.append(f"{where}: literal resource lookup outside the catalog")
        if re.search(r'"Argument_\w+"', source):
            violations.append(f"{where}: raw Argument_ resource key outside the catalog")
    return violations

APPLICATION_CATALOG = APPLICATION / "Messages" / "CsvMessageResources.cs"
sources = [(str(path.relative_to(ROOT)), path.read_text(encoding="utf-8"))
           for root in (CORE, APPLICATION)
           for path in root.rglob("*.cs")
           if path not in (CATALOG, APPLICATION_CATALOG) and path.name != "AssemblyInfo.cs"]
# Application-specific CSV validation messages have a separate typed catalog.
if not APPLICATION_CATALOG.is_file():
    errors.append("Application CSV typed resource catalog is missing")
else:
    application_catalog = APPLICATION_CATALOG.read_text(encoding="utf-8")
    if re.search(r'public\s+static\s+string\s+Get\s*\(\s*string', application_catalog):
        errors.append("Application CSV raw resource-key Get(string) must not be public")
    accessor_pairs = re.findall(
        r'public\s+static\s+string\s+(\w+)\s*=>\s*Get\("((?:Argument|Outcome)_\w+)"\)\s*;',
        application_catalog)
    mapping = {key: accessor for accessor, key in accessor_pairs}
    if set(mapping) != APPLICATION_CSV_ARGUMENT_KEYS | APPLICATION_CSV_OUTCOME_KEYS or len(accessor_pairs) != len(APPLICATION_CSV_ARGUMENT_KEYS | APPLICATION_CSV_OUTCOME_KEYS):
        errors.append("Application CSV typed resource accessors must cover all five keys exactly")
    if any(accessor != key.split("_", 1)[1] for accessor, key in accessor_pairs):
        errors.append("Application CSV typed resource accessor does not match its key")

errors.extend(validate_typed_message_contract(public_catalog, sources))
# Independent negative fixtures: each regression is rejected even when the
# currently valid catalog and application sources are otherwise unchanged.
assert not validate_typed_message_contract(public_catalog, []), "Valid typed catalog rejected"
assert "Raw resource-key Get(string) must not be public" in validate_typed_message_contract(
    public_catalog + "\npublic static string Get(string resourceKey) { }", [])
assert any("literal resource lookup" in problem for problem in validate_typed_message_contract(
    public_catalog, [("broken", 'DomainMessageResources.Get("Argument_DiagnosticNull")')]))
assert any("raw Argument_" in problem for problem in validate_typed_message_contract(
    public_catalog, [("broken", 'var key = "Argument_DiagnosticNull";')]))
assert raw_argument_uses('DomainMessageResources.Get("Argument_DiagnosticNull")') == ["Argument_DiagnosticNull"]
assert not APPLICATION_CSV_ARGUMENT_KEYS.intersection(argument_keys), "Domain still owns CSV messages"
assert set(application_entries) == APPLICATION_CSV_ARGUMENT_KEYS | APPLICATION_CSV_OUTCOME_KEYS, "Application CSV resource set changed"
assert not set(application_entries).intersection(entries), "Overlapping catalogs"


# Every explicit resource reference in the core must resolve.
resource_key_re = re.compile(r'(?:DomainMessageResources|CsvMessageResources)\.Get\("([^"]+)"\)')
string_re = re.compile(r'"((?:\\.|[^"\\])*)"')
for file in sorted(list(CORE.rglob("*.cs")) + list(APPLICATION.rglob("*.cs"))):
    if file.name == "AssemblyInfo.cs":
        continue  # Compiler metadata has fixed required assembly strings.
    source = file.read_text(encoding="utf-8")
    for key in resource_key_re.findall(source):
        if key not in entries and key not in application_entries:
            errors.append(f"{file.relative_to(ROOT)}: missing resource key {key}")
    for i, line in enumerate(source.splitlines(), start=1):
        # Guard English sentences in string literals, but allow symbolic resource
        # lookup keys, stable codes and protocol constants without embedded spaces.
        text = line.partition("//")[0]
        for literal in string_re.findall(text):
            if re.search(r"\s", literal) and not literal.startswith("RecordSift.Resources."):
                errors.append(f"{file.relative_to(ROOT)}:{i}: inline prose string literal")

if errors:
    print("Domain message resources validation failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)
print(f"Resource guards passed: Domain {len(defined_codes)} diagnostic codes, "
      f"{len(argument_keys)} typed arguments, {len(entries)} resources; "
      f"Application CSV {len(APPLICATION_CSV_ARGUMENT_KEYS)} typed argument resources; "
      "no cross-layer keys, raw lookups or inline production prose.")
