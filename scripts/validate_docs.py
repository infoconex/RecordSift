#!/usr/bin/env python3
"""Validate RecordSift requirement catalog, scenario specifications, traceability and links.

No third-party dependencies or .NET SDK required.
Usage: python3 scripts/validate_docs.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parent.parent
ISSUE_RE = re.compile(r"https://github\.com/infoconex/RecordSift/issues/(\d+)")
REQUIREMENT_ROW_RE = re.compile(r"^\|\s*((?:FR|NFR)-\d{3})\s*\|")
TRACE_ROW_RE = re.compile(r"^\|\s*((?:FR|NFR)-\d{3}-SC\d{2})\s*\|")
SCENARIO_HEADER_RE = re.compile(r"^#{2,3}\s+((?:FR|NFR)-\d{3}-SC\d{2})\s+—\s+(.+)$", re.M)
LINK_RE = re.compile(r"(?<!!)\[[^\]]+\]\(([^)]+)\)")
PLACEHOLDERS = {"", "—", "-", "TBD", "Planned", "Not run"}
STATUSES = {"Planned", "Automated", "Manual", "Blocked"}
errors: list[str] = []


def report(message: str) -> None:
    errors.append(message)


def read(relative: str) -> str:
    file = ROOT / relative
    if not file.is_file():
        report(f"Missing required file: {relative}")
        return ""
    return file.read_text(encoding="utf-8")


def markdown_rows(relative: str, pattern: re.Pattern[str]) -> dict[str, list[str]]:
    result: dict[str, list[str]] = {}
    for line in read(relative).splitlines():
        match = pattern.match(line)
        if match is None:
            continue
        key = match.group(1)
        if key in result:
            report(f"{relative}: duplicate row {key}")
        result[key] = [field.strip() for field in line.strip().strip("|").split("|")]
    return result


def require_exact_ids(entries: dict[str, list[str]], prefix: str, count: int, label: str) -> None:
    expected = {f"{prefix}-{i:03d}" for i in range(1, count + 1)}
    actual = {item for item in entries if item.startswith(prefix + "-")}
    for key in sorted(expected - actual):
        report(f"{label}: missing {key}")
    for key in sorted(actual - expected):
        report(f"{label}: unexpected {key}")


def scenario_specifications() -> dict[str, str]:
    """Return scenario ID -> scenario Markdown file path and flag malformed files."""
    scenarios: dict[str, str] = {}
    for prefix, count, category in (("FR", 39, "functional"), ("NFR", 10, "non-functional")):
        folder = ROOT / "docs" / "scenarios" / category
        expected = {f"{prefix}-{i:03d}.md" for i in range(1, count + 1)}
        actual = {f.name for f in folder.glob("*.md")} if folder.exists() else set()
        for missing in sorted(expected - actual):
            report(f"Scenario file missing: {folder.relative_to(ROOT)}/{missing}")
        for unexpected in sorted(actual - expected):
            report(f"Unexpected scenario file: {folder.relative_to(ROOT)}/{unexpected}")

        for filename in sorted(expected & actual):
            requirement = filename[:-3]
            relative = f"docs/scenarios/{category}/{filename}"
            content = read(relative)
            if not content.startswith(f"# {requirement} — "):
                report(f"{relative}: incorrect document title")
            if not re.search(r"^\*\*Verification status:\*\* Planned\b", content, re.M):
                # The documents are planning artifacts; test status is tracked in the matrix.
                report(f"{relative}: document must declare planning-only status")

            if prefix == "FR":
                required_sections = [
                    "Purpose", "Functional requirement", "Inputs and outputs",
                    "Functional rules", "Dependencies and scope",
                    "Open design decisions", "Acceptance scenarios",
                ]
                headings = list(re.finditer(r"^## ([^\n]+)$", content, re.M))
                heading_names = [h.group(1) for h in headings]
                ordered = [x for x in heading_names if x in required_sections]
                if ordered != required_sections:
                    report(f"{relative}: incomplete or incorrectly ordered functional specification sections")
                sections = {}
                for i, heading in enumerate(headings):
                    sections[heading.group(1)] = content[
                        heading.end():headings[i + 1].start() if i + 1 < len(headings) else len(content)
                    ]
                for section in required_sections[:-1]:
                    body = sections.get(section, "")
                    if len(re.findall(r"\b\w+\b", body)) < 8:
                        report(f"{relative}: missing or insufficient detail in section {section}")
                if "**RecordSift shall:**" not in sections.get("Functional requirement", ""):
                    report(f"{relative}: missing normative functional requirement statement")
                for marker in ("- **Inputs:**", "- **Outputs:**"):
                    if marker not in sections.get("Inputs and outputs", ""):
                        report(f"{relative}: missing {marker}")
                if len(re.findall(r"^- ", sections.get("Functional rules", ""), re.M)) < 3:
                    report(f"{relative}: expected at least three explicit functional rules")
                if "- **Related requirements:**" not in sections.get("Dependencies and scope", "") or "- **Boundary:**" not in sections.get("Dependencies and scope", ""):
                    report(f"{relative}: related requirements and boundary must be documented")
                if not re.search(r"^## Acceptance scenarios\s*$", content, re.M):
                    report(f"{relative}: missing acceptance scenarios section")

            found: list[str] = []
            headers = list(SCENARIO_HEADER_RE.finditer(content))
            for i, header in enumerate(headers):
                scenario = header.group(1)
                block = content[header.end():headers[i + 1].start() if i + 1 < len(headers) else len(content)]
                if not scenario.startswith(requirement + "-SC"):
                    report(f"{relative}: foreign scenario {scenario}")
                if scenario in scenarios:
                    report(f"Duplicate scenario across files: {scenario}")
                for keyword in ("**Given**", "**When**", "**Then**"):
                    if keyword not in block:
                        report(f"{relative}: {scenario} missing {keyword}")
                found.append(scenario)
                scenarios[scenario] = relative

            expected_numbering = [f"{requirement}-SC{i:02d}" for i in range(1, len(found) + 1)]
            if found != expected_numbering or len(found) < 2:
                report(f"{relative}: scenarios must be unique, ordered SC01..SCnn, with at least two cases")
            if len(ISSUE_RE.findall(content)) != 1:
                report(f"{relative}: must link exactly one requirement issue")
    return scenarios


def check_links() -> None:
    markdown = [ROOT / "README.md", ROOT / "AGENTS.md"]
    markdown.extend((ROOT / "docs").rglob("*.md"))
    markdown.extend((ROOT / ".github").rglob("*.md"))
    for file in markdown:
        if not file.is_file():
            continue
        for dest in LINK_RE.findall(file.read_text(encoding="utf-8")):
            target = dest.strip().split("#", 1)[0].split("?", 1)[0]
            if not target or re.match(r"^[a-z][a-z0-9+.-]*:", target, re.I) or target.startswith("/"):
                continue
            if not (file.parent / Path(unquote(target))).exists():
                report(f"Broken Markdown link in {file.relative_to(ROOT)}: {dest}")


def validate() -> None:
    required_files = [
        "README.md", "AGENTS.md", "docs/README.md",
        "docs/requirements/functional.md", "docs/requirements/non-functional.md",
        "docs/scenarios/README.md", "docs/specifications/record-import.md",
        "docs/testing/strategy.md", "docs/testing/traceability.md",
        "docs/engineering/workflow.md", ".github/copilot-instructions.md",
        ".github/agents/requirements-reviewer.agent.md",
        ".github/agents/tdd-coach.agent.md",
        ".github/ISSUE_TEMPLATE/requirement.yml",
        ".github/ISSUE_TEMPLATE/bug_report.yml", ".github/pull_request_template.md",
    ]
    for filename in required_files:
        if not (ROOT / filename).is_file():
            report(f"Missing engineering policy file: {filename}")

    functional = markdown_rows("docs/requirements/functional.md", REQUIREMENT_ROW_RE)
    quality = markdown_rows("docs/requirements/non-functional.md", REQUIREMENT_ROW_RE)
    require_exact_ids(functional, "FR", 39, "Functional requirements")
    require_exact_ids(quality, "NFR", 10, "Nonfunctional requirements")
    requirement_catalog = {**functional, **quality}
    specs = scenario_specifications()

    trace_rows = markdown_rows("docs/testing/traceability.md", TRACE_ROW_RE)
    for scenario in sorted(set(specs) - set(trace_rows)):
        report(f"Missing traceability row: {scenario}")
    for scenario in sorted(set(trace_rows) - set(specs)):
        report(f"Traceability row has no specification: {scenario}")

    if len(specs) < 110:
        report(f"Expected at least 110 currently specified scenarios; found {len(specs)}")

    for requirement, catalog in sorted(requirement_catalog.items()):
        if len(catalog) != 4:
            report(f"{requirement}: expected four catalog columns")
            continue
        issues = ISSUE_RE.findall(catalog[3])
        if len(issues) != 1:
            report(f"{requirement}: expected one GitHub issue")
        relative = (f"docs/scenarios/functional/{requirement}.md"
                    if requirement.startswith("FR-")
                    else f"docs/scenarios/non-functional/{requirement}.md")
        spec_text = read(relative)
        if issues and ISSUE_RE.findall(spec_text) != issues:
            report(f"{requirement}: scenario document issue does not match requirement catalog")
        if f"{requirement}.md" not in catalog[3]:
            report(f"{requirement}: catalog must link to its authoritative scenario Markdown")

    for scenario, cells in sorted(trace_rows.items()):
        if len(cells) != 8:
            report(f"{scenario}: expected eight traceability columns, found {len(cells)}")
            continue
        requirement = scenario[:6] if scenario.startswith("FR-") else scenario[:7]
        catalog = requirement_catalog.get(requirement)
        if catalog is None:
            report(f"{scenario}: requirement missing from catalog")
            continue
        priority = catalog[2] if requirement.startswith("FR-") else "Must"
        if cells[1] != requirement or cells[2] != priority:
            report(f"{scenario}: mismatched requirement or priority")
        if ISSUE_RE.findall(cells[3]) != ISSUE_RE.findall(catalog[3]):
            report(f"{scenario}: issue link differs from requirement catalog")
        expected_file = specs.get(scenario)
        if expected_file and f"{requirement}.md" not in cells[4]:
            report(f"{scenario}: incorrect authoritative specification reference")
        if cells[6] not in STATUSES:
            report(f"{scenario}: invalid test status '{cells[6]}'")
        if cells[6] in {"Automated", "Manual"}:
            if cells[5] in PLACEHOLDERS or cells[7] in PLACEHOLDERS:
                report(f"{scenario}: status {cells[6]} requires test/check and observed evidence")
        if cells[6] == "Blocked" and cells[7] in PLACEHOLDERS:
            report(f"{scenario}: Blocked requires an explanation")

    check_links()
    if errors:
        print(f"Documentation validation failed ({len(errors)} findings):")
        for message in errors:
            print(" - " + message)
        sys.exit(1)
    print(f"Documentation validation passed: {len(requirement_catalog)} requirements, "
          f"{len(specs)} scenarios, complete traceability, all links resolved.")


if __name__ == "__main__":
    validate()
