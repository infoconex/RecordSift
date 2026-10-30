# RecordSift engineering documentation

These pages specify the proposed product and engineering process for **release 0.1**; they do not imply an implementation exists.

## Requirement catalogs

- [Functional requirements](requirements/functional.md) — `FR-001` through `FR-039`, each with its own GitHub issue and acceptance scenario.
- [Nonfunctional requirements](requirements/non-functional.md) — `NFR-001` through `NFR-010`, each with its own GitHub issue and verification obligation.

## Specifications and verification

- [Draft import specification](specifications/record-import.md)
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
- Each requirement has one independent issue, whose body includes `FR-###-SC01` or `NFR-###-SC01` as the first planned acceptance scenario.
- Priority `Must` applies to the release 0.1 scope; `Should` and `Future` remain backlog items unless deliberately moved into the release by a documented change.
- Every scenario is **Planned** until a linked test/check is implemented and execution evidence exists.
- An issue may cross-reference other requirements without becoming responsible for implementing them.
- Requirement, scenario, test, PR, and release evidence should be linked by the future traceability matrix (#53).

## Delivery policy

Branch from `release/0.1` only when starting a requirement. Implement it using TDD where appropriate. Open a PR targeting `release/0.1`; review and squash merge. Promote a release to `main` via a separate release PR. Follow issue #55 for PR template automation when implemented.
