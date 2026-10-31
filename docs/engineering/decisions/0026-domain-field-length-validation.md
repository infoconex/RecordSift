# ADR 0026 — Inclusive field-length validation in Domain

**Decision:** Implement [FR-019](../../scenarios/functional/FR-019.md) in [issue #17](https://github.com/infoconex/RecordSift/issues/17) using optional inclusive length bounds on Domain-owned `ExpectedColumn` declarations. Application evaluates the Domain rule against source field values before accepting typed records.

## Contract

- `ExpectedColumn(name, isRequired, isValueRequired, minimumLength, maximumLength)` extends the existing declaration without altering its two- and three-parameter constructors.
- A null bound is not enforced; nonnegative integer bounds are inclusive. A minimum greater than the maximum is an invalid schema declaration.
- Length is `.NET string.Length`: UTF-16 code units of the *untrimmed* source field, including whitespace, before CLR property mapping. The same rule can operate on any decoded source format. Unicode scalar count, display width, grapheme count and raw encoded byte count are different concepts and are not silently inferred.
- A missing optional source column or null value does not become required solely because a length constraint exists. A present empty string is measured as length zero. Per-record required values remain governed independently by FR-017 and FR-012.
- Domain returns a record-scoped `FIELD_LENGTH_OUT_OF_RANGE` diagnostic with the declared field name, logical row and reliable physical line, plus a fixed resource-backed message that never exposes source text.
- Application evaluates length after the existing required-value check and before property mapping. Rows with length errors are not mapped or accepted; multiple length errors in one row still count as a single rejected record.
- No validation rules are implemented in CsvHelper or Infrastructure. Existing imports without configured bounds preserve their behavior and performance characteristics except a no-op schema validation pass.

## Evidence

[PR #137](https://github.com/infoconex/RecordSift/pull/137) carries FR-019-SC01 and SC02 real CSV typed-import acceptance, plus Domain negative boundary tests. [Windows TDD RED #41168146162](https://github.com/infoconex/RecordSift/actions/runs/41168146162) demonstrated missing length declaration and validation contracts. Mark the scenarios Automated in [traceability](../../testing/traceability.md) only after the complete Windows suite passes.
