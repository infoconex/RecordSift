# Testing strategy and TDD policy

**Issue:** [#52](https://github.com/infoconex/RecordSift/issues/52)  
**Status:** Engineering policy for release 0.1. 16 existing baseline and FR-030 tests, 19 FR-028 unit-contract cases, and 13 FR-009 schema-binding tests 13 FR-013 positional-binding tests and 16 FR-001 CSV parser component tests execute in the historical net48 Windows suite; full importer acceptance remains planned.

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

**FR-030-SC01–SC05 have observed in-memory unit evidence; the remaining 104 requirement scenarios remain Planned.** FR-028, FR-009 and FR-013 have unit-contract evidence but have not demonstrated reader-integrated acceptance. The 77-test Windows suite passed in [run #41073984695](https://github.com/infoconex/RecordSift/actions/runs/41073984695). The CSV parser component is not a typed import acceptance test. Initial .NET Framework 4.8 assembly smoke tests ran on Windows ([run #41017804680](https://github.com/infoconex/RecordSift/actions/runs/41017804680): 3 passed, 0 failed). These are not requirement acceptance tests. Ongoing CI is implemented through issue #64.
