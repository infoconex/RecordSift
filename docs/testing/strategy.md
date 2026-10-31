# Testing strategy and TDD policy

**Issue:** [#52](https://github.com/infoconex/RecordSift/issues/52)  
**Status:** Engineering policy for release 0.1. Current acceptance status and observed Windows CI evidence are maintained per scenario in [traceability](traceability.md); earlier milestone test counts below are historical, not the current release total.

## Test-driven development

For each independently tracked FR/NFR, start from its authoritative `docs/scenarios/<category>/<ID>.md` specification. Inspect the actual code, dependencies, existing tests, and last passing CI **before** deciding whether production work is necessary. Record exactly one primary evaluation outcome in the issue and PR:

| Outcome | When to use it | Required evidence |
| --- | --- | --- |
| **Acceptance verification only** | Required behavior already exists; additional independent acceptance or boundary tests establish evidence. | Scenario-linked tests or reviewed procedures, observed passing Windows CI, and a clear statement that no production code change was necessary. Do **not** fabricate a failing RED test. |
| **Defect correction** | Existing behavior violates a scenario or a boundary contract. | Reproducible failing regression against the original implementation, minimum corrective production change, and passing regression plus affected acceptance suite. |
| **New capability** | Required behavior is missing from the public/owned contract. | Genuine TDD RED for absent behavior, smallest compatible implementation, GREEN, relevant negative/boundary tests, and compatibility review. |

When one issue combines acceptance verification and a defect correction, explicitly identify **both** contributions; label the primary outcome by the reason production code changed. A refactor without observable behavior change must be identified as a refactor and supported by the appropriate regression gate, not described as a new capability.

For every outcome:

1. **Classify:** State the existing behavior, expected Given/When/Then behavior, any identified gap, and the chosen outcome with supporting code/test references.
2. **Test:** Add independent scenario-level coverage when existing tests do not establish acceptance. Run relevant Domain/Application/real-adapter checks; include malformed inputs, inclusive boundaries, culture, and negative cases when applicable.
3. **Red/green only when justified:** For a genuine defect or missing capability, capture a meaningful failing pre-fix test and then make the smallest production change to reach GREEN. For verification-only work, keep the existing implementation unchanged.
4. **Refactor and protect:** Preserve the domain-owned contract, dependency direction, resource-backed diagnostic messages, public API/JSON baselines, and release framework/language compatibility.
5. **Trace:** Include `FR-###` / `NFR-###`, `*-SC##`, and issue references in NUnit metadata and tests as applicable; update each scenario row in [traceability](traceability.md) with actual observed evidence.
6. **Demonstrate:** Record the evaluation outcome, changed production/test files, exact CI run URL, outcome, and applicable scope limits in the PR and issue. A scenario is not Automated just because a test was written; a passed net48 gate is not cross-platform certification.

A requirement may require several tests, but each test should have one clear reason to fail. A **Planned** scenario means there is not yet adequate execution evidence; a maintained specification is not evidence of a passing test. Keep architecture and public API modernization separate from release 0.1 behavioral acceptance.
## Test levels

| Level | Purpose | Evidence |
| --- | --- | --- |
| Unit | Schemas, mapping, conversion, validation, aggregate counts | Small tests with no real files when possible |
| Component | Real parser adapter with in-memory input | Parser behavior and source location rules |
| Integration | Real CSV and XLSX fixtures through public API | Records, errors, resource handling |
| Characterization | Freeze pre-modernization observable behavior | Golden JSON reports and semantic comparisons |
| Contract/API | Public API surface, output shape and package compatibility | Compile tests, snapshots/API comparison |
| Negative/security | Malformed input, malicious content, size limits | Controlled fixtures with failure assertions |
| Cross-framework | Same scenarios run on each supported target | Test matrix with exact TFM and SDK |
| Performance | Import throughput, allocations, memory use | Repeated controlled benchmark; regression threshold before gate |

Exploratory and manual tests may supplement automation, but must include a repeatable procedure and observed result. They do not replace automate-able acceptance tests.

## Independent Domain, Application and adapter contract suites

The architecture test-project split in [#91](https://github.com/infoconex/RecordSift/issues/91) makes dependency direction testable, not merely a contributor guideline:

- `tests/RecordSift.Domain.Tests` references **only RecordSift.Domain and NUnit**; it cannot reference Application, Infrastructure or CsvHelper. It tests schema, diagnostics, messages and import-result invariants independently.
- `tests/RecordSift.Application.Tests` references **Application, Domain and NUnit**, never Infrastructure or CsvHelper; hand-written fake ports test source/CSV policy and configuration independently.
- `tests/RecordSift.Application.Tests/Contracts/CsvRecordReaderContractTests.cs` declares **vendor-independent shared behavioral assertions** for the Application-owned `ICsvRecordReader` port contract: raw records, quoting, line/provenance, invalid source, bounded reads, security, caller ownership and immutable results.
- `tests/RecordSift.Infrastructure.Csv.Tests` references the real infrastructure adapter and **inherits the shared contract suite** through its own `CsvAdapterContractConformanceTests` fixture, alongside additional real-parser CSV header and delimiter integration tests. All future adapters must execute an equivalent inherited contract suite against the real implementation.
- Windows CI builds **all five NUnit test assemblies** (Domain, Application, CSV adapter, XLSX adapter and Reporting), runs each suite **separately**, verifies isolated core output dependencies and publishes distinct NUnit XML files. Architecture validation rejects outward test project references, stale source folders and forbidden imports.
- NUnit itself is a development-only test framework, not an import processing dependency. The requirement is to eliminate **third-party import libraries** from Domain/Application unit-test authoring, compilation and execution.

## Ports, adapters and architecture tests

- Use hand-written fakes or stubs for RecordSift-owned application input ports. Validate import orchestration, schema binding and diagnostic handling without importing parser vendor assemblies into core/unit tests.
- Exercise the real third-party parser **only in adapter/component and end-to-end tests** for quoting, malformed input, newline/row provenance, resource budgets and source failure handling. Faking an adapter does not verify parsing.
- Where a parser or library is swapped, retain characterization tests of the existing observable contract; document and approve intentional changes before asserting substitute implementations satisfy the same contract (Liskov substitution).
- Keep domain and application compilation independent of third-party package references; automated architecture-layer and project-reference checks are enforced in CI alongside code review. See [architecture](../engineering/architecture.md) and [ADR 0007](../engineering/decisions/0007-domain-ports-and-adapters.md).

## Fixture policy

- Store compact, non-sensitive sample files under `tests/**/Fixtures` when created.
- Never use actual customer, employee, or production data in fixtures.
- Include: valid CSV, quoted comma and newline, alternate delimiter, headerless data, alternate encoding, empty and malformed records, XLSX with multiple sheets, invalid workbook, oversized cases, and mixed valid/invalid rows.
- Use invariant/culture-explicit expectations; never assume machine locale, file iteration order, or local time.
- Avoid brittle snapshots of runtime-generated exception stack traces and absolute file paths.
- The authoritative report format is specified by [import-result.md](../specifications/import-result.md) and [JSON Schema 1.0](../specifications/schemas/import-result.schema.json). Do not maintain duplicate expected JSON documents inside the specifications.
- Seven approved, anonymized JSON compatibility fixtures **are implemented** in [`tests/RecordSift.Infrastructure.Reporting.Tests/Fixtures/Compatibility/`](../../tests/RecordSift.Infrastructure.Reporting.Tests/Fixtures/Compatibility/). The reporting test project checks complete JSON trees, deterministic serialization and versioned Completed/Failed behavior with Stopped/Canceled extensions. There is no separate `RecordSift.ContractTests` project and no warning fixture in release 0.1.
- Contract tests must validate report structure against the JSON Schema **and** assert semantic invariants not expressible in JSON Schema, especially row-based counting and source errors. Do not treat a schema passing as sufficient evidence that counters are correct.
- Distinguish logical CSV record ordinal from physical line number in assertions.

## Dependencies and test isolation

Use the **pinned NUnit 3.14.0 and NUnit.ConsoleRunner 3.17.0** versions in the current net48 solution; Windows CI restores, builds and executes their five test assemblies. NSubstitute is optional where genuine collaborators need isolation; avoid gratuitous mocks of plain data and validators. Later test-framework upgrades are isolated from SDK and project-format experiments.

## Test naming, metadata and targeted execution

Test **level**, **verification purpose**, and **traceability** are independent dimensions. Use NUnit 3's existing metadata; no additional test framework is required.

- Every concrete NUnit `[TestFixture]` declares exactly one **primary level** at the class level: `[Category("Unit")]`, `[Category("Component")]`, or `[Category("Integration")]`. Use `EndToEnd` only after explicitly extending this policy and introducing a genuinely end-to-end suite. The abstract shared reader-port contract fixture has no category; its concrete Infrastructure implementation declares `Component`.
- `[Property("Verification", "Acceptance")]` indicates a test asserts behavior from a documented acceptance scenario; it is **not** another test level. Other documented purposes may include `Contract`, `Regression`, and `Characterization`. Do not assume all tests supporting a requirement are acceptance tests.
- `[Property("Requirement", "FR-017")]` (or `NFR-###`) establishes the associated requirement. `[Property("Scenario", "FR-017-SC01")]` is for a named scenario, and requires matching Requirement plus `Verification=Acceptance`. Multiple scenario properties are allowed where a focused split is not practical; preserve every actual scenario association.
- Test classes describe the exercised component/behavior and end in `Tests` rather than a requirement number. Ordinary methods describe the observable outcome in PascalCase; scenario-linked tests start with the normalized scenario ID, e.g. `FR_017_SC01_RejectsBlankRequiredValues`. Multiple scenarios can use `FR_017_SC01_SC02_...` when necessary. The suffix should describe behavior, not repeat the class name.
- `scripts/validate_test_metadata.py` verifies fixture-level category, documented requirement/scenario IDs, scenario-name/metadata agreement, and verification purpose. It has positive and negative self-tests. Metadata is **not execution evidence**: a scenario remains Planned in `traceability.md` until complete relevant behavior has observed passing evidence.

With Release test assemblies already built on Windows and NuGet packages restored, use `scripts/run_tests.ps1` for focused execution:

```powershell
.\scripts\run_tests.ps1 -Level Unit
.\scripts\run_tests.ps1 -Verification Acceptance
.\scripts\run_tests.ps1 -Requirement FR-017
.\scripts\run_tests.ps1 -Scenario FR-017-SC01
.\scripts\run_tests.ps1 -Level Integration -Requirement FR-017 -Verification Acceptance
.\scripts\run_tests.ps1 -Where "Requirement == 'FR-017' || Requirement == 'FR-018'"
```

Selectors combine with logical AND; `-Where` accepts NUnit's native expression language for complex cases. A selected run with zero tests fails. The Windows GitHub Actions `workflow_dispatch` input `test_filter` provides optional manual targeted execution. **Pull requests and pushes ignore filters, always run all five full suites, and retain the minimum-test-count gate.** NUnit XML evidence is uploaded in either mode; targeted XML is labeled `nunit-targeted.xml`.

## Pre-merge quality gate

- A PR must reference one primary issue and exact requirement/scenario IDs.
- New behavior has both success and relevant failure cases.
- All affected tests pass on the supported environments and the PR contains evidence.
- Report any skipped, flaky or platform-dependent tests; do not hide them.
- Coverage is a diagnostic, not an alternative to strong assertions; establish a realistic threshold only after baseline tests exist.
- Security-sensitive input parsing requires malformed-input tests and a dependency/license review.
- No claimed benchmark improvement without reproducible measurements.

## Current status

See [scenario-to-test traceability](traceability.md) for current Automated/Planned/Manual/Blocked evidence and the specific Windows CI runs that established it. Older milestone totals are historical and must not be repeated as the current suite count. A source specification, test name, or merged PR alone does **not** prove a scenario passed; mark it verified only after observing the relevant Windows CI and documenting remaining scope limits. The accepted release 0.1 baseline is **379 passing NUnit cases across five assemblies** with 77 accepted functional scenarios and all ten Windows-scoped nonfunctional requirements verified. In particular, .NET Framework 4.8 acceptance cannot establish the behavior of an unbuilt modern target.
