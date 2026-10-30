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
CORE = ROOT / "src" / "RecordSift"
RESX = CORE / "Resources" / "DomainMessages.resx"

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

codes = (CORE / "DiagnosticCodes.cs").read_text(encoding="utf-8")
defined_codes = re.findall(r'public const string \w+ = "([A-Z_]+)";', codes)
for code in defined_codes:
    if code not in entries:
        errors.append("Missing diagnostic resource: " + code)

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
      f"{len(entries)} resources, no inline production prose.")
