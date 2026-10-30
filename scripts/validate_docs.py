#!/usr/bin/env python3
"""Validate the RecordSift requirements catalog and repository documentation.

Uses Python's standard library only, so it runs before any .NET project exists.
Run from any directory: python3 scripts/validate_docs.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parent.parent
ISSUE_RE = re.compile(r"https://github\.com/infoconex/RecordSift/issues/(\d+)")
ROW_RE = re.compile(r"^\|\s*((?:FR|NFR)-\d{3})\s*\|", re.M)
LINK_RE = re.compile(r"(?<!!)\[[^\]]+\]\(([^)]+)\)")
PLACEHOLDERS = {"", "—", "-", "TBD", "Planned", "Not run"}

errors: list[str] = []


def error(message: str) -> None:
    errors.append(message)


def read(relative: str) -> str:
    file = ROOT / relative
    if not file.is_file():
        error("Missing required file: " + relative)
        return ""
    return file.read_text(encoding="utf-8")


def parse_table(relative: str, prefix: str) -> dict[str, list[str]]:
    content = read(relative)
    entries: dict[str, list[str]] = {}
    for line in content.splitlines():
        match = ROW_RE.match(line)
        if not match:
            continue
        requirement_id = match.group(1)
        if not requirement_id.startswith(prefix + "-"):
            continue
        fields = [part.strip() for part in line.strip().strip("|").split("|")]
        if requirement_id in entries:
            error(f"{relative}: duplicated {requirement_id}")
        entries[requirement_id] = fields
    return entries


def check_expected(entries: dict[str, list[str]], prefix: str, count: int, source: str) -> None:
    expected = {f"{prefix}-{number:03d}" for number in range(1, count + 1)}
    found = set(entries)
    for missing in sorted(expected - found):
        error(f"{source}: missing {missing}")
    for unexpected in sorted(found - expected):
        error(f"{source}: unexpected {unexpected}")


def check_document_links() -> None:
    files = [ROOT / "README.md", ROOT / "AGENTS.md"]
    files.extend((ROOT / "docs").rglob("*.md"))
    files.extend((ROOT / ".github").rglob("*.md"))
    for file in files:
        if not file.is_file():
            continue
        for target in LINK_RE.findall(file.read_text(encoding="utf-8")):
            raw = target.strip().split("#", 1)[0].split("?", 1)[0]
            if not raw or re.match(r"^[a-z]+:", raw, re.I) or raw.startswith("/"):
                continue
            relative = Path(unquote(raw))
            if not (file.parent / relative).exists():
                error(f"Broken link in {file.relative_to(ROOT)}: {target}")


def validate() -> None:
    required_files = [
        "README.md",
        "AGENTS.md",
        "docs/README.md",
        "docs/requirements/functional.md",
        "docs/requirements/non-functional.md",
        "docs/specifications/record-import.md",
        "docs/testing/strategy.md",
        "docs/testing/traceability.md",
        "docs/engineering/workflow.md",
        ".github/copilot-instructions.md",
        ".github/agents/requirements-reviewer.agent.md",
        ".github/agents/tdd-coach.agent.md",
        ".github/ISSUE_TEMPLATE/requirement.yml",
        ".github/ISSUE_TEMPLATE/bug_report.yml",
        ".github/pull_request_template.md",
    ]
    for name in required_files:
        if not (ROOT / name).is_file():
            error("Missing repository policy file: " + name)

    functional = parse_table("docs/requirements/functional.md", "FR")
    quality = parse_table("docs/requirements/non-functional.md", "NFR")
    trace = read("docs/testing/traceability.md")
    traces = {
        match.group(1): [part.strip() for part in line.strip().strip("|").split("|")]
        for line in trace.splitlines()
        if (match := ROW_RE.match(line))
    }

    check_expected(functional, "FR", 39, "Functional requirements")
    check_expected(quality, "NFR", 10, "Nonfunctional requirements")
    check_expected(traces, "FR", 39, "Traceability")
    check_expected(traces, "NFR", 10, "Traceability")

    ids = set(functional) | set(quality)
    for rid in sorted(ids):
        catalog = functional.get(rid) or quality.get(rid)
        trace_row = traces.get(rid)
        catalog_issues = ISSUE_RE.findall(" ".join(catalog))
        if len(catalog_issues) != 1:
            error(f"{rid}: requirement catalog must link exactly one GitHub issue")
        if not trace_row:
            continue
        if len(trace_row) != 7:
            error(f"{rid}: traceability row must contain seven fields")
            continue
        if trace_row[3] != rid + "-SC01":
            error(f"{rid}: incorrect primary acceptance scenario {trace_row[3]}")
        trace_issues = ISSUE_RE.findall(trace_row[2])
        if trace_issues != catalog_issues:
            error(f"{rid}: issue link does not match requirement catalog")
        priority = catalog[2] if rid.startswith("FR-") else "Must"
        if trace_row[1] != priority:
            error(f"{rid}: inconsistent priority in traceability")
        status = trace_row[5]
        if status not in {"Planned", "Automated", "Manual", "Blocked"}:
            error(f"{rid}: unknown verification status {status}")
        if status in {"Automated", "Manual"}:
            if trace_row[4] in PLACEHOLDERS or trace_row[6] in PLACEHOLDERS:
                error(f"{rid}: status {status} requires a test/check and execution evidence")

    check_document_links()
    if errors:
        print("Documentation validation failed:")
        for message in errors:
            print(" - " + message)
        sys.exit(1)
    print("Documentation validation passed: 39 FR, 10 NFR, 49 traced scenarios; internal links resolved.")


if __name__ == "__main__":
    validate()
