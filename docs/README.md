# RecordSift engineering documentation

These pages specify the proposed product and engineering process for **release 0.1**; they do not imply an implementation exists.

## Requirement catalogs

- [Functional requirements](requirements/functional.md) — `FR-001` through `FR-039`, each with its own GitHub issue and acceptance scenario.
- [Nonfunctional requirements](requirements/non-functional.md) — `NFR-001` through `NFR-010`, each with its own GitHub issue and verification obligation.

## Specifications and verification

- [Acceptance scenarios](scenarios/README.md) — 49 authoritative requirement files and 109 scenarios (five FR-030 summary-unit scenarios automated, the remaining 104 planned; FR-028 has additional unit-contract tests but still needs importer acceptance tests).
- [Draft import specification](specifications/record-import.md)
- [Versioned import result JSON contract](specifications/import-result.md) and [JSON Schema 1.0](specifications/schemas/import-result.schema.json) — documentation contract; expected JSON fixtures will live with contract tests when implemented.
- [TDD and test types](testing/strategy.md)
- [Requirement-to-scenario-to-test matrix](testing/traceability.md)

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
