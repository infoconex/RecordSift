# RecordSift engineering documentation

These pages document the **accepted release 0.1 Windows/.NET Framework 4.8 implementation**, its normative requirements, test evidence, and historical engineering decisions. The release candidate passed 379 NUnit tests, 425 compiled public API baseline entries, seven golden JSON regression fixtures, and independently installed internal prerelease **and stable 0.1.0** NuGet consumers. All 25 Must and eight targeted Should functional requirements and ten Windows-scoped nonfunctional requirements are accepted. Six Future features are deliberately deferred. See [functional completion](engineering/release-0.1-functional-completion.md), [release readiness](engineering/release-0.1-readiness.md), and [release checklist](release/0.1-release-checklist.md). Public package publication is **not** implied by technical acceptance.

**How to read this documentation:** Requirement and scenario documents specify intended behavior; the [traceability matrix](testing/traceability.md) holds current observed execution status. ADRs and early issue/PR evidence document **historical decisions and intermediate milestones**, not the current feature-completion state. Do not interpret the mandatory `Planned` specification label in older FR files as proof that the associated acceptance tests remain unimplemented.

## Requirement catalogs

- [Functional requirements](requirements/functional.md) — `FR-001` through `FR-039`, each with its own GitHub issue and acceptance scenario.
- [Nonfunctional requirements](requirements/non-functional.md) — `NFR-001` through `NFR-010`, each with its own GitHub issue and verification obligation.

## Specifications and verification

- [Acceptance scenarios](scenarios/README.md) — 49 authoritative requirement files and **108 active release scenarios**; scenario-level test mappings and observed Windows execution evidence are maintained in the [traceability matrix](testing/traceability.md). Historical NFR-009-SC02 is retired from release 0.1 acceptance and preserved in the [platform review](engineering/platform-dependencies-net48.md).
- [Accepted release 0.1 import behavior and historical decisions](specifications/record-import.md)
- [Versioned import result JSON contract](specifications/import-result.md) and [JSON Schema 1.0](specifications/schemas/import-result.schema.json) — normative wire contract; seven approved JSON golden fixtures now live in the [reporting test project](../tests/RecordSift.Infrastructure.Reporting.Tests/Fixtures/Compatibility/) and run in Windows CI.
- [TDD and test types](testing/strategy.md)
- [Requirement-to-scenario-to-test matrix](testing/traceability.md)

FR-009 schema binding is implemented and verified in Domain unit and real CSV/XLSX end-to-end acceptance tests.

FR-013 named and positional binding is verified in typed-import integration tests, including headerless CSV acceptance.

FR-001 CSV and FR-002 XLSX typed imports are implemented and acceptance-verified. Domain owns schemas, validation, diagnostics and results; Application owns typed import orchestration and ports; Infrastructure adapters isolate CsvHelper, ExcelDataReader and reporting. Mapping, alias/default/conversion, required/length/range/duplicate/pattern/custom validation, processing controls, JSON/readable/CSV reports, and source-safety boundaries are covered by the [release acceptance inventory](engineering/release-0.1-functional-completion.md).

FR-003 configurable delimiter and FR-004 headed/headerless policy have passing typed-import acceptance; the original implementation decisions remain in [ADR 0009](engineering/decisions/0009-configurable-csv-delimiters.md) and [ADR 0006](engineering/decisions/0006-csv-header-policy.md).

Source-level failures are distinguished from rejected records, and reporting consumes structured results without reopening the input. Validation/diagnostic semantics, row numbering and JSON schema evolution are documented in the [import specification](specifications/record-import.md) and [versioned JSON contract](specifications/import-result.md).

## Explicit source-property mapping (FR-010)

- [ADR 0023 — Explicit string-property mapping](engineering/decisions/0023-explicit-string-property-mapping.md) — format-neutral Domain mapping declarations and Application string destination assignment with strict prevalidation.
- [FR-010 issue #19](https://github.com/infoconex/RecordSift/issues/19) — first string-only implementation through [PR #115](https://github.com/infoconex/RecordSift/pull/115); broader CLR types and conversion are tracked under FR-011.

## Explicit primitive conversion (FR-011)

- [ADR 0024 — Explicit primitive conversion](engineering/decisions/0024-explicit-primitive-conversion.md) — Domain culture-controlled conversions, Application property mapping, and safe source-field diagnostics.
- [FR-011 issue #4](https://github.com/infoconex/RecordSift/issues/4) — test-driven Domain, fake-reader and real CSV integration, verified in [Windows #41150035896](https://github.com/infoconex/RecordSift/actions/runs/41150035896).

## Architecture, code style and dependency isolation

- [Domain-centric architecture and SOLID boundaries](engineering/architecture.md) — ports, adapters, direction of dependencies, mockable seams and security.
- [Microsoft C# conventions](engineering/csharp-style.md) — .NET Framework 4.8 / C# 7.3 compatible code and editor settings.
- [ADR 0007 — Domain-centric ports and adapters](engineering/decisions/0007-domain-ports-and-adapters.md) — accepted design direction.
- [ADR 0008 — Isolated CsvHelper adapter](engineering/decisions/0008-csvhelper-isolated-adapter.md) — RecordSift-owned port and pinned library integration.
- [ADR 0009 — CSV delimiter policy](engineering/decisions/0009-configurable-csv-delimiters.md) — single-character delimiters, default, safe validation and explicit out-of-scope behavior.
- [#84 — Engineering conventions](https://github.com/infoconex/RecordSift/issues/84) and [#85 — Third-party CSV adapter migration](https://github.com/infoconex/RecordSift/issues/85) record completed historical engineering work.

## Domain message contracts and test isolation

- [ADR 0010 — Core-owned resource messages](engineering/decisions/0010-domain-message-resources.md) — invariant diagnostic texts in embedded resources; stable codes and named defaults.
- [Issue #89 — Message resources](https://github.com/infoconex/RecordSift/issues/89) — migration and regression validation.
- [ADR 0016 — Typed Domain resource messages](engineering/decisions/0016-typed-domain-resource-messages.md) — replace public key lookup with named resource-backed Domain argument messages.
- [Issue #101 — Typed message contract](https://github.com/infoconex/RecordSift/issues/101) — independent resource API refactoring and Red/Green contract checks.
- [ADR 0020 — Application-owned CSV validation messages](engineering/decisions/0020-application-csv-message-ownership.md) — preserve text while relocating five CSV-specific precondition messages out of Domain resources.
- [Issue #109](https://github.com/infoconex/RecordSift/issues/109) — own each resource catalog in the layer that defines its validation rules.
- [Issue #91 — Third-party-free unit testing](https://github.com/infoconex/RecordSift/issues/91) — separate independent core unit and real-adapter tests using reusable domain-owned reader conformance fixture.

## Microsoft C# naming enforcement

- [ADR 0012 — Microsoft C# naming analyzer gate](engineering/decisions/0012-microsoft-csharp-naming-ci.md) — enforce Roslyn IDE1006 and PascalCase const identifiers across the traditional net48 solution.
- [Engineering issue #93](https://github.com/infoconex/RecordSift/issues/93) — isolated implementation, Red/Green evidence and exact CI commands.

## Physical domain/application/infrastructure layers

- [ADR 0013 — Independent Domain, Application and Infrastructure assemblies](engineering/decisions/0013-physical-domain-application-infrastructure-assemblies.md) — compiler-enforced inward dependencies and CI layout checks.
- [Issue #95 — Correct the physical architecture](https://github.com/infoconex/RecordSift/issues/95) — new Domain/Application assemblies, isolated CsvHelper infrastructure and independent unit/contract tests.

## First typed CSV import slice

- [ADR 0022 — First caller-mapped typed CSV import](engineering/decisions/0022-first-typed-csv-import.md) — immutable Domain result, injected Application importer, invariant messages and real-adapter tests without claiming generalized mapping/validation completeness.
- [Issue #113](https://github.com/infoconex/RecordSift/issues/113) — independently testable FR-001 implementation slice; [Windows #41145214018](https://github.com/infoconex/RecordSift/actions/runs/41145214018) verified 145 tests before documentation finalization.

## Focused CSV infrastructure implementation

- [ADR 0021 — Single-responsibility CSV infrastructure classes](engineering/decisions/0021-focused-csv-infrastructure-classes.md) — separate the adapter, caller-owned character-limited reader and two internal exception types without changing behavior or package boundaries.
- [Issue #106](https://github.com/infoconex/RecordSift/issues/106) — test-first physical source ownership and unchanged real-parser conformance tests.

## Infrastructure naming and adapter contracts

- [ADR 0014 — Infrastructure CSV adapter naming](engineering/decisions/0014-infrastructure-csv-adapter-naming.md) — name assemblies after their architectural role and source format; reserve `Adapter` for the concrete bridge to the third-party parser.
- [Engineering issue #97](https://github.com/infoconex/RecordSift/issues/97) — consistent assembly, namespaces, tests and CI dependency enforcement.

## Public API namespace ownership

- [ADR 0015 — Layer-aligned public namespaces](engineering/decisions/0015-layer-aligned-public-namespaces.md) — explicit source/binary breaking migration table for domain diagnostics, schemas/results and application CSV contracts, with runtime public-type checks and architecture enforcement.
- [Engineering issue #99](https://github.com/infoconex/RecordSift/issues/99) — namespace migration after the separate assembly and infrastructure naming corrections.
- [ADR 0017 — Application CSV input ports](engineering/decisions/0017-application-ports-namespace.md) — replace the generic `Abstractions/Csv` folder and namespace with role-specific `Ports/Csv` while preserving the vendor-independent port contract.
- [Engineering issue #103](https://github.com/infoconex/RecordSift/issues/103) — dedicated pre-release namespace migration, structure checks and CI verification.

## Core dependency allowlists

- [ADR 0019 — Approved Domain/Application framework dependencies](engineering/decisions/0019-approved-core-framework-dependencies.md) — generic build/assembly/package/HintPath deny-by-default checks with test-first negative fixtures.
- [Issue #108](https://github.com/infoconex/RecordSift/issues/108) — protect the core against unrelated third-party dependencies while preserving the infrastructure integration boundary.

## Isolated Domain, Application and adapter tests

- [ADR 0018 — Separate unit-test assemblies](engineering/decisions/0018-independent-domain-application-tests.md) — compiler-enforced Domain-only and Application-only test dependencies; reusable vendor-neutral port conformance fixture lives with Application tests and runs against the real adapter.
- [Issue #105](https://github.com/infoconex/RecordSift/issues/105) — three separately verified Windows .NET Framework 4.8 NUnit assemblies and architecture guard negative fixtures.

## Independent engineering issues

- [#51 — Behavioral specification](https://github.com/infoconex/RecordSift/issues/51)
- [#52 — TDD and test types](https://github.com/infoconex/RecordSift/issues/52)
- [#53 — Scenario/test traceability](https://github.com/infoconex/RecordSift/issues/53)
- [#54 — Agent development guidance](https://github.com/infoconex/RecordSift/issues/54)
- [#55 — GitHub issue/PR templates](https://github.com/infoconex/RecordSift/issues/55)
- [#56 — Documentation/build CI workflows](https://github.com/infoconex/RecordSift/issues/56)
- [#57 — Functional net48 baseline](https://github.com/infoconex/RecordSift/issues/57)

## Identifiers and status

- `FR-###` and `NFR-###` identify requirements permanently.
- Each requirement has one independent issue, which links to its authoritative scenario Markdown file under `docs/scenarios/`.
- Priority `Must` applies to the release 0.1 scope; `Should` and `Future` remain backlog items unless deliberately moved into the release by a documented change.
- **Traceability** owns scenario execution state: all 77 in-scope functional scenarios and ten scoped NFR requirements are accepted as recorded there; legacy functional-specification `Planned` metadata describes specification planning only. Historical FR-030 unit evidence was later supplemented by end-to-end CSV/XLSX and reporting acceptance. Do not confuse specification metadata with actual test execution status.
- An issue may cross-reference other requirements without becoming responsible for implementing them.
- Requirement, scenario, test, PR, and release evidence are linked in [the traceability matrix](testing/traceability.md).

## Delivery policy

Branch from `release/0.1` only when starting a requirement. Implement it using TDD where appropriate. Open a PR targeting `release/0.1`; review and squash merge. Promote a release to `main` by a **separate, reviewed squash-merge PR** after all gates pass and the owner authorizes promotion. A merge to `main` does **not** authorize tagging, a GitHub release, or public NuGet distribution. The PR template and CI checks are already implemented.
