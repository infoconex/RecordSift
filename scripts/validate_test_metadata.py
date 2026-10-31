#!/usr/bin/env python3
"""Check NUnit test levels, requirement/scenario metadata and naming consistency.

Uses the standard library only. This guard validates the repository's deliberately
simple declaration layout, not arbitrary C# syntax. NUnit remains the test runner.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LEVELS = {"Unit", "Component", "Integration"}
VERIFICATIONS = {"Acceptance", "Contract", "Regression", "Characterization"}
FIXTURE = re.compile(
    r"^[ \t]*\[TestFixture\][ \t]*\n"
    r"(?P<attributes>(?:^[ \t]*\[[^\n]+\][ \t]*\n)*)"
    r"^[ \t]*public (?P<kind>sealed|abstract)?[ \t]*class (?P<name>\w+)",
    re.M,
)
TEST_METHOD = re.compile(
    r"(?P<attributes>(?:^[ \t]*\[[^\n]+\][ \t]*\n)+)"
    r"^[ \t]*public void (?P<name>\w+)\(",
    re.M,
)
PROPERTY = re.compile(r'\[Property\("([^"]+)", "([^"]+)"\)\]')
CATEGORY = re.compile(r'\[Category\("([^"]+)"\)\]')
TEST_ATTRIBUTE = re.compile(r"\[(?:Test\]|TestCase(?:Source)?\()")
SCENARIO_NAME = re.compile(
    r"^(FR|NFR)_(\d{3})_((?:SC\d{2}_)+)([A-Za-z][A-Za-z0-9_]*)$"
)
REQUIREMENT_NAME = re.compile(r"^(FR|NFR)_(\d{3})_")
SCENARIO_HEADING = re.compile(r"^#{2,3}\s+((?:FR|NFR)-\d{3}-SC\d{2})\b", re.M)
REQUIREMENT_ROW = re.compile(r"^\|\s*((?:FR|NFR)-\d{3})\s*\|", re.M)


def inspect_source(path: str, source: str, requirements: set[str],
                   scenarios: set[str]) -> tuple[list[str], int]:
    errors: list[str] = []
    fixtures = list(FIXTURE.finditer(source))
    if len(fixtures) != 1:
        errors.append(f"{path}: expected one test fixture declaration; found {len(fixtures)}")
    for fixture in fixtures:
        category = CATEGORY.findall(fixture.group("attributes"))
        if fixture.group("kind") != "abstract":
            if len(category) != 1 or category[0] not in LEVELS:
                errors.append(f"{path}: concrete fixture {fixture.group('name')} must have "
                              "exactly one primary level (Unit, Component, Integration)")
        elif category:
            errors.append(f"{path}: abstract shared-contract fixture must defer its "
                          "test-level category to concrete inheritors")

    count = 0
    for method in TEST_METHOD.finditer(source):
        attrs, name = method.group("attributes"), method.group("name")
        if not TEST_ATTRIBUTE.search(attrs):
            continue
        count += 1
        location = f"{path}:{source.count(chr(10), 0, method.start()) + 1}: {name}"
        if CATEGORY.search(attrs):
            errors.append(f"{location}: test level belongs on the concrete fixture")
        values: dict[str, list[str]] = {}
        for key, value in PROPERTY.findall(attrs):
            values.setdefault(key, []).append(value)
        requirements_on_test = values.get("Requirement", [])
        scenarios_on_test = values.get("Scenario", [])
        purposes = values.get("Verification", [])
        if len(requirements_on_test) > 1:
            errors.append(f"{location}: expected at most one Requirement property")
        if len(purposes) > 1 or any(v not in VERIFICATIONS for v in purposes):
            errors.append(f"{location}: invalid or repeated Verification property")
        for req in requirements_on_test:
            if req not in requirements:
                errors.append(f"{location}: unknown requirement {req}")
        for scenario in scenarios_on_test:
            if scenario not in scenarios:
                errors.append(f"{location}: undocumented scenario {scenario}")
            if not requirements_on_test or not scenario.startswith(requirements_on_test[0] + "-SC"):
                errors.append(f"{location}: scenario {scenario} does not match Requirement")
        if len(set(scenarios_on_test)) != len(scenarios_on_test):
            errors.append(f"{location}: duplicate scenario properties")
        if scenarios_on_test and purposes != ["Acceptance"]:
            errors.append(f"{location}: scenario-linked test requires Verification=Acceptance")
        if purposes == ["Acceptance"] and not scenarios_on_test:
            errors.append(f"{location}: Acceptance purpose requires a scenario identifier")

        req_prefix = REQUIREMENT_NAME.match(name)
        if req_prefix:
            expected_req = f"{req_prefix.group(1)}-{req_prefix.group(2)}"
            if requirements_on_test != [expected_req]:
                errors.append(f"{location}: name requires Requirement={expected_req}")

        scenario_prefix = SCENARIO_NAME.match(name)
        if scenario_prefix:
            expected_scenarios = [
                f"{scenario_prefix.group(1)}-{scenario_prefix.group(2)}-{sc}"
                for sc in re.findall(r"SC\d{2}", scenario_prefix.group(3))
            ]
            if scenarios_on_test != expected_scenarios:
                errors.append(f"{location}: name prefixes {expected_scenarios} "
                              f"do not match Scenario properties {scenarios_on_test}")
        elif scenarios_on_test:
            errors.append(f"{location}: scenario-linked test must start with "
                          "FR_###_SC##_ or NFR_###_SC##_ and include a behavior name")
    if count == 0 and "abstract class" not in source and "ConformanceTests" not in path:
        errors.append(f"{path}: no executable NUnit test declarations found")
    return errors, count


def self_test() -> None:
    source = """using NUnit.Framework;
[TestFixture]
[Category("Unit")]
public sealed class SampleTests
{
    [Test]
    [Property("Verification", "Acceptance")]
    [Property("Requirement", "FR-017")]
    [Property("Scenario", "FR-017-SC01")]
    public void FR_017_SC01_RejectsBlankRequiredValues() {}
}
"""
    requirements = {"FR-017"}
    scenarios = {"FR-017-SC01"}
    errors, count = inspect_source("SampleTests.cs", source, requirements, scenarios)
    assert not errors and count == 1, f"valid fixture rejected: {errors}"
    cases = [
        (source.replace('[Category("Unit")]\n', ""), "primary level"),
        (source.replace('"FR-017-SC01"', '"FR-017-SC02"'), "undocumented scenario"),
        (source.replace("FR_017_SC01_Rejects", "FR_017_SC02_Rejects"), "name prefixes"),
        (source.replace('[Property("Requirement", "FR-017")]', ""), "Requirement"),
        (source.replace("FR_017_SC01_Rejects", "Rejects"), "scenario-linked"),
        (source.replace('[Property("Verification", "Acceptance")]', ""), "Verification"),
        (source.replace('[Category("Unit")]', '[Category("Unit")]\n[Category("Integration")]'),
         "primary level"),
    ]
    for invalid, expected in cases:
        findings, _ = inspect_source("SampleTests.cs", invalid, requirements, scenarios)
        assert any(expected in item for item in findings), (
            f"negative case {expected} was not rejected: {findings}"
        )


def main() -> int:
    self_test()
    requirements: set[str] = set()
    for path in (ROOT / "docs/requirements").glob("*.md"):
        requirements.update(REQUIREMENT_ROW.findall(path.read_text(encoding="utf-8")))
    scenarios: set[str] = set()
    for path in (ROOT / "docs/scenarios").rglob("*.md"):
        scenarios.update(SCENARIO_HEADING.findall(path.read_text(encoding="utf-8")))
    failures: list[str] = []
    total = 0
    fixtures = 0
    for path in sorted((ROOT / "tests").rglob("*Tests.cs")):
        issues, count = inspect_source(str(path.relative_to(ROOT)),
                                       path.read_text(encoding="utf-8"),
                                       requirements, scenarios)
        failures.extend(issues)
        total += count
        fixtures += 1
    if failures:
        print(f"NUnit metadata validation FAILED ({len(failures)} issues):")
        for failure in failures:
            print(" - " + failure)
        return 1
    print(f"NUnit metadata validation passed: {fixtures} fixtures, "
          f"{total} declared test methods; known requirement/scenario IDs "
          "and negative self-tests verified.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
