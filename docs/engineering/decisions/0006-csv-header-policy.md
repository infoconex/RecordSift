# ADR 0006 — Explicit CSV header policy over shared bindings

**Decision:** FR-004 introduces an explicit `CsvHeaderPolicy` (`WithHeader` / `WithoutHeader`) and minimal raw `CsvTableReader` that composes an FR-001 CSV record reader, FR-009 named schema binder and FR-013 positional binder.

**Implementation note:** The original static reader was replaced in [issue #85](https://github.com/infoconex/RecordSift/issues/85) by an injectable `ICsvRecordReader` with a separate CsvHelper adapter; see [ADR 0008](0008-csvhelper-isolated-adapter.md). Header and schema rules below remain unchanged.

## Rules and behavior

- No header inference. In headed mode the **first parsed logical source record** is header metadata. In headerless mode the first record is data.
- `CsvDataRow.Row` is a one-based logical data-row ordinal excluding any header. `SourceRecordNumber` tracks the one-based raw record position (which includes the header), and `PhysicalLine` tracks the true starting physical CSV line; these can differ when quoted cells contain newlines.
- Headed sources use the existing deterministic `ImportSchema.BindHeaders` behavior, including case-sensitive names, optional columns and ambiguity detection. Supplying positional mappings in headed mode is a configuration error.
- Headerless nonempty sources require explicit `SourceColumnPosition` definitions, using `ImportSchema.BindPositions` with the first data row's field count. No implicit positional inference.
- A zero-byte headed source fails with `REQUIRED_HEADER_MISSING`; a zero-byte headerless source completes with zero data records without requiring a position map. A valid header-only source completes with no data records.
- Failure of parsing, schema binding or a later row that lacks a declared bound position yields `Failed`, a source-scope diagnostic, and **no partial raw data rows or bindings**. A short source row is a source-binding failure in this initial layer, not a record validation rejection; deeper per-row semantics remain separate.
- All returned collections are immutable snapshots. This component does not produce `ImportSummary` accepted/rejected counts: raw records have not yet undergone validation.
- The fixed-code registry and safe templates from FR-028 remain in use, without raw field values in messages.
- The layer uses already-decoded text, fixed comma dialect and an explicit caller-supplied character budget. FR-003 delimiter selection, FR-006 encoding and full NFR-007 resource rules remain separately tracked.

## Rationale and alternatives

A separate raw table result preserves parser/header responsibilities without preempting property mapping, validation and accepted-record materialization. Returning an `ImportResult.Completed` here would incorrectly imply every raw field has passed schema and domain validation. Automatic header detection or positional inference could silently drop or misbind the first headerless row. For now, results carry `ImportStatus` but no human-readable report message or JSON serialization.

## Verification and remaining obligations

NUnit component tests exercise actual CSV text, headed/headerless row classification, missing/ambiguous headers, zero-byte handling, quoted multiline row numbering, malformed inputs and positional errors. They demonstrate header policy and shared binding against raw records, **not typed import acceptance**. FR-004-SC01/SC02 stay Planned until the complete public typed CSV importer produces equivalent mapped values and finalized import results.
