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

## Independent core unit tests and executable adapter contracts

The architecture test-project split in [#91](https://github.com/infoconex/RecordSift/issues/91) makes dependency direction testable, not merely a contributor guideline:

- `tests/RecordSift.Tests` references **only the separate RecordSift.Domain/RecordSift.Application assemblies and NUnit**, with **no CsvHelper or other third-party import parser**. Hand-written fakes test consumer policy, result/domain invariants, message resources and configuration. The test assembly must run independently with no adapter DLL in its output.
- `tests/RecordSift.Tests/Contracts/CsvRecordReaderContractTests.cs` declares **vendor-independent shared behavioral assertions** for the core `ICsvRecordReader` contract: raw records, quoting, line/provenance, invalid source, bounded reads, security, caller ownership and immutable results.
- `tests/RecordSift.Infrastructure.Csv.Tests` references the real infrastructure adapter and **inherits the shared contract suite** through its own `CsvAdapterContractConformanceTests` fixture, alongside additional real-parser CSV header and delimiter integration tests. All future adapters must execute an equivalent inherited contract suite against the real implementation.
- Windows CI builds both test assemblies, runs them **separately** via NUnit and publishes individual NUnit XML results. A dependency-audit guard checks project references and forbidden adapter imports in core tests.
- NUnit itself is a development-only test framework, not an import processing dependency. The requirement is to eliminate **third-party import libraries** from core unit-test authoring, compilation and execution.

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

## Pre-merge quality gate

- A PR must reference one primary issue and exact requirement/scenario IDs.
- New behavior has both success and relevant failure cases.
- All affected tests pass on the supported environments and the PR contains evidence.
- Report any skipped, flaky or platform-dependent tests; do not hide them.
- Coverage is a diagnostic, not an alternative to strong assertions; establish a realistic threshold only after baseline tests exist.
- Security-sensitive input parsing requires malformed-input tests and a dependency/license review.
- No claimed benchmark improvement without reproducible measurements.

## Current status

**FR-030-SC01–SC05 have observed in-memory unit evidence; the remaining 104 requirement scenarios remain Planned.** FR-028, FR-009 and FR-013 have unit-contract evidence but have not demonstrated reader-integrated acceptance. The 94-test Windows suite passed in [run #41075489461](https://github.com/infoconex/RecordSift/actions/runs/41075489461). The CSV parser component is not a typed import acceptance test. Initial .NET Framework 4.8 assembly smoke tests ran on Windows ([run #41017804680](https://github.com/infoconex/RecordSift/actions/runs/41017804680): 3 passed, 0 failed). These are not requirement acceptance tests. Ongoing CI is implemented through issue #64.
