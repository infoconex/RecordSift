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
public_catalog = CATALOG.read_text(encoding="utf-8")

def raw_argument_uses(source):
    return re.findall(r'DomainMessageResources\.Get\("(Argument_\w+)"\)', source)

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

sources = [(str(path.relative_to(ROOT)), path.read_text(encoding="utf-8"))
           for root in (CORE, APPLICATION)
           for path in root.rglob("*.cs")
           if path != CATALOG and path.name != "AssemblyInfo.cs"]
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

# Every explicit resource reference in the core must resolve.
resource_key_re = re.compile(r'DomainMessageResources\.Get\("([^"]+)"\)')
string_re = re.compile(r'"((?:\\.|[^"\\])*)"')
for file in sorted(CORE.rglob("*.cs")):
    if file.name == "AssemblyInfo.cs":
        continue  # Compiler metadata has fixed required assembly strings.
    source = file.read_text(encoding="utf-8")
    for key in resource_key_re.findall(source):
        if key not in entries:
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
print(f"Domain message guard passed: {len(defined_codes)} diagnostic codes, "
      f"{len(argument_keys)} typed argument message accessors, {len(entries)} resources, "
      "no raw argument keys outside catalog and no inline production prose.")
