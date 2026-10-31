# ADR 0004 — Explicit zero-based source-column positions

**Decision:** FR-013 extends the format-neutral `ImportSchema` with `BindPositions(sourceFieldCount, positions)`, using immutable `SourceColumnPosition` declarations.

## Motivation and scope

Headerless feeds require explicit field coordinates before any source row can be interpreted safely. FR-009 already established that `SourceColumnBinding.SourceIndex` uses a zero-based convention. We reuse that convention rather than offering both zero- and one-based indexes and risking off-by-one errors. Named matching remains the separately existing `BindHeaders` method. Each binding operation is exclusively name-based or position-based; no mixed implicit inference.

## Rules

- A positional declaration references an existing schema column by its exact ordinal, case-sensitive name and a nonnegative zero-based source field index.
- `sourceFieldCount` represents available fields supplied by the reader and must be nonnegative. A mapped index equal to the field count is out of range.
- Missing required mapping emits `REQUIRED_COLUMN_MISSING`; optional unmapped columns are ignored without warnings. A declared out-of-range mapping emits `INVALID_COLUMN_POSITION` even for an optional column.
- Two different declared columns cannot target the same physical source index; the binder emits `AMBIGUOUS_COLUMN` for each, in schema declaration order. Duplicate mappings for the same declared field and unknown field mappings are invalid configuration and throw.
- Binding failure yields no partial mappings, and every diagnostic is source-scoped with null logical row. Rejected-row counters are unaffected.
- Binding and diagnostics use deterministic **schema declaration order**, independent of the order of supplied position entries.
- These are caller-supplied source field coordinates; no actual CSV/XLSX records are parsed or consumed here.
- The fixed safe-message registry gains `INVALID_COLUMN_POSITION`, rather than allowing arbitrary user-supplied diagnostic codes.

## Tradeoffs and dependencies

This small extension avoids a second schema abstraction, mixed named/positional binding mode, reflection/property mappings and reader-specific options. Actual CSV parsing (FR-001), header interpretation (FR-004), mapping to .NET properties (FR-010), and materializing typed values (FR-026) remain separately tracked. Therefore FR-013-SC01/SC02 have **unit-contract evidence only** until integrated typed data imports can be tested. A future reader must supply reliable field count per logical record rather than silently reading the wrong source index.

## Verification

See `PositionalBindingTests` and [scenario traceability](../../testing/traceability.md). At the time of this ADR, scenario evidence was still **Planned** pending end-to-end reader/mapping tests.
