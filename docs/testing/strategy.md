# Testing strategy and TDD policy

**Issue:** [#52](https://github.com/infoconex/RecordSift/issues/52)  
**Status:** Engineering policy for release 0.1. In [issue #91](https://github.com/infoconex/RecordSift/issues/91), Windows [run #41085050502](https://github.com/infoconex/RecordSift/actions/runs/41085050502) observed **71/71 independent domain unit tests and 48/48 real-adapter contract/component tests** (119/119 total). Core test binary is checked for absence of adapter DLLs, and test project dependencies are inspected. Full typed importer acceptance remains planned.

## Test-driven development

For each independently tracked FR/NFR, start from its authoritative `docs/scenarios/<category>/<ID>.md` specification. Every scenario must trace to a test or verification procedure as implementation progresses:

For each independently tracked FR/NFR:

1. **Red:** Add a small, observable acceptance or lower-level test that states the requirement and initially fails for the missing behavior.
2. **Green:** Make the smallest useful production change to pass it.
3. **Refactor:** Improve the design with the tests still green.
4. **Review:** Add negative, boundary, malformed-input, and culture-sensitive cases as appropriate; keep tests deterministic.
5. **Trace:** Reference `FR-###` or `NFR-###`, scenario `FR-###-SC01`/`NFR-###-SC01`, and the GitHub issue in test metadata or method names/comments where practical.
6. **Demonstrate:** Document the exact command, target, test results, and CI run URL in the PR. A planned scenario is not evidence that a test ran.

A requirement may need several tests, but a test should have one clear reason to fail. Maintain functional API behavior independently of package/project-format modernization.

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
- Windows CI builds **all three NUnit test assemblies**, runs Domain, Application and real-adapter suites **separately**, verifies independent output dependencies and publishes distinct NUnit XML files. Architecture validation rejects outward test project references, stale source folders and forbidden imports.
- NUnit itself is a development-only test framework, not an import processing dependency. The requirement is to eliminate **third-party import libraries** from Domain/Application unit-test authoring, compilation and execution.

## Ports, adapters and architecture tests

- Use hand-written fakes or stubs for RecordSift-owned application input ports. Validate import orchestration, schema binding and diagnostic handling without importing parser vendor assemblies into core/unit tests.
- Exercise the real third-party parser **only in adapter/component and end-to-end tests** for quoting, malformed input, newline/row provenance, resource budgets and source failure handling. Faking an adapter does not verify parsing.
- Where a parser or library is swapped, retain characterization tests of the existing observable contract; document and approve intentional changes before asserting substitute implementations satisfy the same contract (Liskov substitution).
- Keep domain and application compilation independent of third-party package references; architectural review and project reference checks are required until automated rules exist. See [architecture](../engineering/architecture.md) and [ADR 0007](../engineering/decisions/0007-domain-ports-and-adapters.md).

## Fixture policy

- Store compact, non-sensitive sample files under `tests/**/Fixtures` when created.
- Never use actual customer, employee, or production data in fixtures.
- Include: valid CSV, quoted comma and newline, alternate delimiter, headerless data, alternate encoding, empty and malformed records, XLSX with multiple sheets, invalid workbook, oversized cases, and mixed valid/invalid rows.
- Use invariant/culture-explicit expectations; never assume machine locale, file iteration order, or local time.
- Avoid brittle snapshots of runtime-generated exception stack traces and absolute file paths.
- The authoritative report format is specified by [import-result.md](../specifications/import-result.md) and [JSON Schema 1.0](../specifications/schemas/import-result.schema.json). Do not maintain duplicate expected JSON documents inside the specifications.
- When contract tests are implemented, place anonymized expected JSON snapshots under `tests/RecordSift.ContractTests/Fixtures/`, alongside executable tests; expected cases include successful import, completed with rejected rows, empty import, and failed import. No warning fixture is part of the initial contract.
- Contract tests must validate report structure against the JSON Schema **and** assert semantic invariants not expressible in JSON Schema, especially row-based counting and source errors. Do not treat a schema passing as sufficient evidence that counters are correct.
- Distinguish logical CSV record ordinal from physical line number in assertions.

## Dependencies and test isolation

Use a maintained test framework and adapter compatible with the historical net48 baseline. NUnit is the proposed default; **pin versions only after a successful clean Windows restore/build**. NSubstitute is optional where genuine collaborators need isolation; avoid gratuitous mocks of plain data and validators. Later test-framework upgrades are isolated from SDK and project-format experiments.

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

Selectors combine with logical AND; `-Where` accepts NUnit's native expression language for complex cases. A selected run with zero tests fails. The Windows GitHub Actions `workflow_dispatch` input `test_filter` provides optional manual targeted execution. **Pull requests and pushes ignore filters, always run all three full suites, and retain the minimum-test-count gate.** NUnit XML evidence is uploaded in either mode; targeted XML is labeled `nunit-targeted.xml`.

## Pre-merge quality gate

- A PR must reference one primary issue and exact requirement/scenario IDs.
- New behavior has both success and relevant failure cases.
- All affected tests pass on the supported environments and the PR contains evidence.
- Report any skipped, flaky or platform-dependent tests; do not hide them.
- Coverage is a diagnostic, not an alternative to strong assertions; establish a realistic threshold only after baseline tests exist.
- Security-sensitive input parsing requires malformed-input tests and a dependency/license review.
- No claimed benchmark improvement without reproducible measurements.

## Current status

As of [Windows run #41156947959](https://github.com/infoconex/RecordSift/actions/runs/41156947959), **172 NUnit cases passed** across Domain (85), Application (31), and real Infrastructure CSV (56). **11 of 109** documented acceptance scenarios have Automated evidence; 98 remain Planned (see [traceability](traceability.md)). FR-001 full import acceptance and other pending requirements are not completed by merely labeling or filtering related tests. Existing adapter contract and fake-port evidence is useful but does not, by itself, prove an end-to-end workflow. The current Windows and docs validation gates are described in the GitHub Actions workflows.
