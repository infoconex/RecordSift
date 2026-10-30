# RecordSift engineering documentation

These pages specify the product and engineering process for **release 0.1**. Some in-memory and raw CSV components are implemented; the complete typed import API and all end-to-end acceptance scenarios are not yet implemented.

## Requirement catalogs

- [Functional requirements](requirements/functional.md) — `FR-001` through `FR-039`, each with its own GitHub issue and acceptance scenario.
- [Nonfunctional requirements](requirements/non-functional.md) — `NFR-001` through `NFR-010`, each with its own GitHub issue and verification obligation.

## Specifications and verification

- [Acceptance scenarios](scenarios/README.md) — 49 authoritative requirement files and 109 scenarios (five FR-030 summary-unit scenarios automated, the remaining 104 planned; FR-028, FR-009 and FR-013 have additional unit tests, and FR-001 has raw CSV parser tests and FR-004 has headed/headerless reader component tests, but all still need full importer acceptance tests).
- [Draft import specification](specifications/record-import.md)
- [Versioned import result JSON contract](specifications/import-result.md) and [JSON Schema 1.0](specifications/schemas/import-result.schema.json) — documentation contract; expected JSON fixtures will live with contract tests when implemented.
- [TDD and test types](testing/strategy.md)
- [Requirement-to-scenario-to-test matrix](testing/traceability.md)

The FR-009 source-column schema binding contract is implemented and unit-tested against supplied header metadata. Real CSV/XLSX reader integration remains an acceptance-test obligation.

The FR-013 explicit positional binding contract is implemented and unit-tested without claiming typed-record or source-reader integration.

The FR-001 raw CSV reader now uses a separately packaged CsvHelper adapter behind `ICsvRecordReader`; the core keeps the parser-independent record and diagnostic contracts. It still does not map CLR types or complete public typed imports. Issue #2 remains open for the full import API and acceptance scenarios.

The FR-003 delimiter policy uses core-owned `CsvReadOptions` and source-scoped configuration diagnostics, with real adapter component tests; the end-to-end typed import scenarios are still Planned. See [ADR 0009](engineering/decisions/0009-configurable-csv-delimiters.md).

The FR-004 header policy integrates an injected CSV reader port with named/positional bindings and keeps logical data-row numbering independent of headers. Complete typed imports and final result reporting remain forthcoming.

## Architecture, code style and dependency isolation

- [Domain-centric architecture and SOLID boundaries](engineering/architecture.md) — ports, adapters, direction of dependencies, mockable seams and security.
- [Microsoft C# conventions](engineering/csharp-style.md) — .NET Framework 4.8 / C# 7.3 compatible code and editor settings.
- [ADR 0007 — Domain-centric ports and adapters](engineering/decisions/0007-domain-ports-and-adapters.md) — accepted design direction.
- [ADR 0008 — Isolated CsvHelper adapter](engineering/decisions/0008-csvhelper-isolated-adapter.md) — RecordSift-owned port and pinned library integration.
- [ADR 0009 — CSV delimiter policy](engineering/decisions/0009-configurable-csv-delimiters.md) — single-character delimiters, default, safe validation and explicit out-of-scope behavior.
- [#84 — Engineering conventions](https://github.com/infoconex/RecordSift/issues/84) and [#85 — Third-party CSV adapter migration](https://github.com/infoconex/RecordSift/issues/85) are separately scoped implementation work.

## Domain message contracts and test isolation

- [ADR 0010 — Core-owned resource messages](engineering/decisions/0010-domain-message-resources.md) — invariant diagnostic texts in embedded resources; stable codes and named defaults.
- [Issue #89 — Message resources](https://github.com/infoconex/RecordSift/issues/89) — migration and regression validation.
- [Issue #91 — Third-party-free unit testing](https://github.com/infoconex/RecordSift/issues/91) — separate independent core unit and real-adapter tests using reusable domain-owned reader conformance fixture.

## Microsoft C# naming enforcement

- [ADR 0012 — Microsoft C# naming analyzer gate](engineering/decisions/0012-microsoft-csharp-naming-ci.md) — enforce Roslyn IDE1006 and PascalCase const identifiers across the traditional net48 solution.
- [Engineering issue #93](https://github.com/infoconex/RecordSift/issues/93) — isolated implementation, Red/Green evidence and exact CI commands.

## Physical domain/application/infrastructure layers

- [ADR 0013 — Independent Domain, Application and Infrastructure assemblies](engineering/decisions/0013-physical-domain-application-infrastructure-assemblies.md) — compiler-enforced inward dependencies and CI layout checks.
- [Issue #95 — Correct the physical architecture](https://github.com/infoconex/RecordSift/issues/95) — new Domain/Application assemblies, isolated CsvHelper infrastructure and independent unit/contract tests.

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
- Scenarios remain **Planned** until a linked test/check has observed execution evidence; FR-030 summary-level unit tests now provide evidence for SC01–SC05, but do not verify end-to-end import behavior.
- An issue may cross-reference other requirements without becoming responsible for implementing them.
- Requirement, scenario, test, PR, and release evidence are linked in [the traceability matrix](testing/traceability.md).

## Delivery policy

Branch from `release/0.1` only when starting a requirement. Implement it using TDD where appropriate. Open a PR targeting `release/0.1`; review and squash merge. Promote a release to `main` via a separate release PR. Follow issue #55 for PR template automation when implemented.
