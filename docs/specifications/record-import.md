# Record import behavioral specification (draft)

**Issue:** [#51](https://github.com/infoconex/RecordSift/issues/51)  
**Status:** Draft behavioral contract. An in-memory summary and supplied diagnostic carrier exist for FR-030. The raw CSV reader/table is implemented with a separately injected CsvHelper adapter and FR-003 delimiter options (component tests observed); issue #113 adds a narrowly scoped typed CSV API with caller-supplied conversion and real-adapter integration tests; generalized validation, XLSX and complete FR-001 acceptance remain outstanding; explicit string and primitive CLR conversion now have real CSV integration evidence.  
**Applies to:** Release 0.1 Must requirements.

## Intent and boundaries

RecordSift accepts CSV or XLSX input, maps fields to a declared schema, performs conversion and validation, and returns accepted typed records plus structured diagnostics. Import failures (unreadable files) are distinct from invalid records.

The initial baseline is synchronous and may materialize results, with explicit input/resource limits. Asynchronous and streaming support are deferred (FR-033 and FR-034).

## Processing sequence

1. Open the specified source using the configured CSV reader or XLSX reader.
2. Validate source-level options, column header/position bindings, and worksheet selection.
3. Enumerate logical source records and retain their provenance.
4. Convert fields using explicitly configured culture and parsing rules.
5. Run required, length, range, uniqueness, and registered custom validation rules.
6. Return only accepted typed records. Accumulate diagnostics for rejected records.
7. Compute an internally consistent summary and expose JSON reporting from this in-memory result.

FR-010 establishes explicit ordinal source-to-property mapping. FR-011 now adds a Domain-owned culture-controlled primitive converter and Application mapping for strings, integers, decimals, dates, booleans and nullable values, with source-aware conversion errors. Further validation policy, custom conversion and immutable destination constructors remain separate FRs; this is not evidence of complete FR-001 acceptance. The proposed JSON report contract is defined separately in [import-result.md](import-result.md) and [its JSON Schema](schemas/import-result.schema.json).

## Source location

Diagnostics must identify a logical source ID, source format, logical record ordinal (one-based, excluding headers), field/column ID when applicable, error code, message, and severity. XLSX diagnostics additionally identify worksheet and physical row number where available. CSV diagnostics should include a physical line number only when reliably provided by the chosen parser, because a quoted multiline field may span multiple lines.

No raw source value, credential, or full filesystem path is included in diagnostic messages by default. Original raw values may be exposed through a deliberately separate, opt-in API after privacy review.

## Result invariants

- `Processed = Accepted + Rejected` for completed imports at the logical-record level.
- `RejectedCount` is the number of distinct processed data rows with at least one error-level record diagnostic (error-bearing rows), not the number of error messages.
- `ProcessedCount = AcceptedCount + RejectedCount` for completed imports; each processed data row is either accepted or rejected due to one or more record errors.
- An absent optional value does not cause an error by itself; advisory warnings and configurable severity are deferred beyond release 0.1.
- Individual **error** diagnostics retain code, source and row information and can be inspected independently of aggregate row counts.
- File- or schema-level failures do not contribute to row-level rejection counts; they are represented through machine-readable outcome state and source-level error diagnostics.
- Avoid ambiguous summary `errorCount` fields: individual errors remain in the diagnostics collection. No warning-related fields appear in JSON contract version 1.0.
- Accepted records preserve input order.
- Record-validation errors can be accumulated while continuing to process subsequent rows (FR-029).
- Source-level fatal errors cannot be misrepresented as a successful empty import.
- Report generation consumes the completed result; it must not re-read input.
- Identical input and explicit settings must give identically ordered diagnostics (NFR-004).

## Empty-source outcomes and messages

A zero-record outcome is **not** automatically a failed import. RecordSift must distinguish import completion from source/schema failure in structured result state, independently of message wording:

| Source and configuration | Outcome | Counts | Result message |
| --- | --- | --- | --- |
| Valid required headers, no data rows | Completed | ProcessedCount = AcceptedCount = RejectedCount = 0; zero validation errors | Informational: no data records found |
| Zero bytes, explicitly headerless | Completed | ProcessedCount = AcceptedCount = RejectedCount = WarningRowCount = 0; zero validation errors | Informational: source contains no records |
| Zero bytes, required headers | Failed (source/schema) | ProcessedCount = AcceptedCount = RejectedCount = WarningRowCount = 0; a source/schema diagnostic is present | Error: required CSV header row is missing |
| Nonempty CSV missing a required column | Failed (schema binding) | Do not claim a successful import; counts reflect actual processed records | Error: identify the missing required column |

The messages are intended for humans and may be revised or localized. **A caller must be able to determine completion/failure from machine-readable result state and coded diagnostics without parsing message text.** An informational zero-row summary must not contribute to error or warning counts. A source/schema error must not be represented as a rejected data record that was never processed.

The external JSON report property names, required fields and initial status values are proposed in the [versioned JSON contract](import-result.md). The internal public .NET API member names, complete diagnostic-code registry, and text localization policy are deferred to their implementation issues. The distinction and semantics above are requirements.

## Format-specific requirements

| Input | Requirements |
| --- | --- |
| CSV | FR-001, FR-003, FR-004, FR-006; delimiter, header policy, text encoding, quoted fields, and configured culture |
| XLSX | FR-002, FR-005; worksheet selection, correct source row references, no Excel installation |
| Common schema | FR-009–FR-013, FR-017–FR-022, FR-026–FR-030 |
| Reporting | FR-035–FR-037 |

The parser library's exceptions should be translated only where doing so improves a documented RecordSift error contract. Do not discard the original exception details from developer-level diagnostics.

## Example scenario (illustrative, not a test result)

Given five source data rows, one rejected row with three validation errors and a second rejected row with one error, a completed continue-on-error import yields `ProcessedCount = 5`, `AcceptedCount = 3`, and `RejectedCount = 2`. Four individual error diagnostics remain available, but do not inflate the row counters.

## Open decisions

- Define public API names, models, and error-code registry in their implementation issues.
- Confirm exact supported reader package versions and licensing before pinning dependencies.
- Agree on file size, row count, worksheet limits, and behavior for errors while loading workbooks.
- Confirm JSON serialization contract with snapshot/characterization tests.
- Resolve whether duplicate matching is ordinal, case-insensitive, culture-aware, or configured by field.

Any resolved decision with broader architectural impact should be documented with an ADR, not silently inferred from an example.
