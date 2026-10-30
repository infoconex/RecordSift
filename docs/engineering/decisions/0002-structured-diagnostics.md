# ADR 0002 — Release 0.1 structured diagnostic contract

**Decision:** Establish a closed, small registry of error codes and fixed, trusted message templates for FR-028. Keep diagnostic metadata immutable and preserve the already-implemented FR-030 row-counting behavior.

## Context

FR-028 needs stable error classification, optional source locations, distinct per-row findings, and safe default messages. The pre-release FR-030 passive `ImportDiagnostic` accepted arbitrary uppercase codes and caller-provided message strings. Its sample test codes were expressly not a published registry. The JSON v1.0 report specifies `scope`, `row`, `field`, `code`, `message` and optional `worksheet`, `physicalRow`, `physicalLine`; it does **not** specify a source ID on each diagnostic.

## Decisions

- Six published error codes are `SOURCE_UNREADABLE`, `REQUIRED_HEADER_MISSING`, `MALFORMED_SOURCE`, `REQUIRED_FIELD_MISSING`, `CONVERSION_FAILED`, `VALIDATION_FAILED`. Code and scope must match. Adding further codes is an explicit contract change, not arbitrary registration.
- The factories no longer accept a free-form `message`. All messages come from hard-coded, non-interpolated templates. This is an intentional pre-release breaking API change from the FR-030 passive model, covered by updated characterization tests. It reduces the likelihood of leaking untrusted source values through messages.
- Field and worksheet identifiers are provided by the calling reader and **are not sanitized or redacted automatically**. Callers must supply trusted metadata, not raw cell contents, file paths, credentials or secrets. We do not claim the entire diagnostic object is safe against intentionally unsafe identifiers.
- Logical `row` is one-based, excludes the CSV header, and is required for Record / absent for Source. Optional physical row and physical line are one-based, validated but **not inferred**. A CSV record may occupy multiple physical lines.
- Diagnostics retain input order in the existing immutable `ImportResult` snapshot, including multiple failures for one logical record; counting is still based on distinct affected record ordinals.
- There is no `sourceId` on individual diagnostics. The proposed JSON contract locates source identity in the enclosing report. The current in-memory `ImportResult` does not yet carry a source ID; source association and actual diagnostic generation remain integration obligations of importer/report issues.
- No parsing, validation-rule execution, JSON serialization, warnings, or extensibility framework is introduced.

## Alternatives considered

- Arbitrary string codes: rejected due to inconsistent categorization and fragile consumers.
- Generic caller-authored messages: rejected for the release 0.1 public factories, as safe redaction cannot be guaranteed.
- An enum serialized directly: rejected because v1.0 requires uppercase string wire codes and an explicit string registry is a clearer contract.
- Sorting diagnostics: rejected; the caller's deterministic detection order is preserved, rather than guessing source positions or reshuffling multiple findings.
- Per-diagnostic source identifiers: rejected for now; they would diverge from the proposed JSON v1.0 report and overlap report design.

## Verification and remaining obligations

NUnit contract tests cover registry classification, invalid code/scope, locations, immutability, messages, deterministic order and distinct-row counts. Those are **unit-level only**. None of FR-028-SC01–SC03 is fully acceptance-verified until a real reader and validation workflow supply diagnostics with reliable source coordinates. See [traceability](../../testing/traceability.md).

**Related:** [FR-028](../../scenarios/functional/FR-028.md), [FR-030](../../scenarios/functional/FR-030.md), [JSON report 1.0](../../specifications/import-result.md), [ADR 0001](0001-in-memory-import-summary.md).
