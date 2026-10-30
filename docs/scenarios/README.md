# Acceptance scenario specifications

This directory is the **authoritative source of expected behavior** for RecordSift. The individual `FR-###.md` files now serve as **full functional requirement specifications with embedded acceptance scenarios**. These documents describe intended behavior, **not evidence that functionality or tests currently exist**.

- [Functional scenarios](functional/) — 39 Markdown files, one per `FR-001` through `FR-039`.
- [Nonfunctional scenarios](non-functional/) — 10 Markdown files, one per `NFR-001` through `NFR-010`.
- [Scenario-level traceability](../testing/traceability.md) — 109 specified scenarios, their GitHub issues, test mappings and observed execution evidence (five FR-030 summary-unit scenarios Automated; remaining 104 Planned).
- [Functional requirements](../requirements/functional.md) and [Nonfunctional requirements](../requirements/non-functional.md).
- [TDD and testing strategy](../testing/strategy.md).

## Functional specification standard

Every `docs/scenarios/functional/FR-###.md` file should contain, in order:

1. **Metadata:** requirement ID, GitHub issue, priority, and verification planning status.
2. **Purpose:** the user/developer problem addressed by the requirement.
3. **Functional requirement:** a precise, affirmative "RecordSift shall" contract.
4. **Inputs and outputs:** source information, configuration and observable results/errors.
5. **Functional rules:** at least three concrete rules, including relevant edge and error conditions.
6. **Dependencies and scope:** links to related FR/NFR files and what is explicitly outside this requirement.
7. **Open design decisions:** unresolved choices to settle during the issue, rather than guessing in code.
8. **Acceptance scenarios:** versioned `FR-###-SC##` headings with Given/When/Then clauses.
9. **Verification notes:** how scenarios become executable test evidence.

The index at `docs/requirements/functional.md` stays concise. Avoid duplicating the full requirement text in GitHub issues: link to the authoritative Markdown. Nonfunctional documents retain their current scenario format pending a separate specification review.

## Scenario ID convention

`FR-001-SC01` means Functional Requirement 001, Scenario 01. `NFR-004-SC02` means Nonfunctional Requirement 004, Scenario 02. Numbers are stable and never recycled; add new scenarios with the next available sequence number.

## Authoring and change control

1. Read the detailed requirement, its scope boundaries, and any open design decisions before starting TDD. Work from the requirement-specific GitHub issue. The issue should **link** to the Markdown scenario file, not repeat its canonical Given/When/Then body.
2. Write testable **Given**, **When**, and **Then** clauses. Distinguish success, failure, boundary, privacy, and platform cases where relevant.
3. Record important inputs and measurable outcomes; avoid locking in unspecified implementation details. Resolve open design decisions through the relevant implementation issue or a separate decision record.
4. In the same PR, update the [traceability matrix](../testing/traceability.md) for each added, changed, or deprecated scenario.
5. Start with failing automated tests where feasible. Change status from **Planned** only when linked verification methods and actual observed evidence exist.
6. Follow the one-issue-per-branch workflow; target `release/0.1` and squash merge reviewed changes. A requirement issue remains open until its applicable acceptance and quality checks are met.

## Release scope and deferred scenarios

`Must` requirements define the intended 0.1 baseline; `Should` and `Future` scenarios remain planned backlog. FR-025 severity customization is explicitly Future-only: release 0.1 uses accepted/rejected records with error diagnostics and no warnings. Some quality requirements include later modern-.NET platform checks; these must be explicitly deferred rather than claimed as complete on a .NET Framework 4.8 runner.

## Notes on requirement overlap

A single end-to-end test may verify multiple scenarios if its assertions independently cover each behavior. Every scenario should still have its **own traceability row** and observed evidence. Do not create duplicate requirements to describe the same outcome.
