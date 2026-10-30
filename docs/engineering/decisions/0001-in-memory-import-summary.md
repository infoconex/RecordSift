# ADR 0001 — Minimal in-memory import summary boundary

**Decision:** Adopt a small immutable in-memory result model for [FR-030](../../scenarios/functional/FR-030.md), without implementing readers or report serialization.

## Context

The normative import-result JSON version 1.0 contract fixes three logical-row counters and machine-readable `Completed`/`Failed` states. FR-030's acceptance scenarios require retaining the supplied error diagnostics, distinguishing fatal source errors from rejected data rows, and counting each rejected row only once. Source parsing, validation, property mapping and JSON serialization belong to different requirement issues.

## Options considered

1. Store three independently mutable counters, leaving callers to maintain invariants. Rejected: risks inconsistent and error-count-based summaries.
2. Implement a full generic importer, validator and serializer while building FR-030. Rejected: absorbs FR-001, FR-026–028 and FR-036 responsibilities.
3. **Chosen:** provide immutable `ImportResult` status, `ImportSummary` counters and a copied, read-only sequence of supplied `ImportDiagnostic` values, calculated by distinct logical error-row ordinals.

## Constraints and tradeoffs

- `ImportResult.Completed(processedCount, diagnostics)` accepts validation errors on data rows without failing the overall import. `Failed` requires a source-level diagnostic; a completed result rejects source failures.
- Counts are nonnegative. Data-row diagnostics use one-based ordinals not greater than the number of processed rows. Only `Record` errors affect `RejectedCount`; `AcceptedCount = ProcessedCount - RejectedCount`.
- `ImportDiagnostic` is a minimal **passive error carrier** whose shape mirrors the required JSON diagnostic core. The diagnostic-code registry, source/field location production, redaction, and validation rule behavior remain **FR-028** responsibilities. Factory creation validates the structural code pattern, not code registration. Test identifiers such as `INVALID_FIELD` and `MISSING_HEADER` are not a published registry.
- Report serialization (`schemaVersion`, `sourceId`, `message`, JSON property casing and schema compliance) remains owned by FR-036/FR-037; the present in-memory object is **not** a JSON report implementation.
- Typed accepted-record materialization remains FR-026/FR-027. Inputs to FR-030 are **already classified** record/source errors; an empty diagnostics collection means each processed row is accepted. Missing optional fields do not create implicit warnings.
- Snapshots preserve input diagnostic order, without inferring physical CSV line numbers. Do not put unsanitized source values into diagnostic messages.

## Consequences and verification

FR-030 can be exercised in isolation with real NUnit tests of all five synthetic result/summary scenarios and negative boundaries. Integration tests demonstrating actual CSV/header/validation behavior and their diagnostic generation still belong to FR-001, FR-004, FR-012 and FR-028. Future public API changes require compatibility review and characterization tests.
