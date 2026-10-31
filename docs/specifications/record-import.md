# Record import behavioral specification — release 0.1

**Issue:** [#51](https://github.com/infoconex/RecordSift/issues/51)  
**Status:** Maintained release 0.1 behavior summary, reconciled with accepted CSV/XLSX typed import, mapping, validation, diagnostics and reporting. The original #51 draft and #113 implementation slice are historical milestones; all 25 Must and eight targeted Should requirements have acceptance evidence in [traceability](../testing/traceability.md). The six Future features remain deferred.  
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

FR-010 establishes explicit ordinal source-to-property mapping. FR-011 supplies a Domain-owned culture-controlled primitive converter and Application mapping for strings, integers, decimals, dates, booleans and nullable values, with source-aware conversion errors. Other release 0.1 requirements cover caller-defined conversion, validation rules, cancellation and fail-fast processing; immutable destination constructors remain outside the supported public mapper scope. The **implemented and versioned** JSON report contract is defined in [import-result.md](import-result.md) and [its JSON Schema](schemas/import-result.schema.json).

## Source location

Structured results carry a caller-provided source ID and source or record diagnostics with stable code and safe message. Record-scoped diagnostics use a one-based logical data-row ordinal (excluding headers); field/column metadata is supplied where available. XLSX diagnostics may additionally identify worksheet and physical row; CSV diagnostics may include a reliable physical line number. A quoted multiline CSV field can span lines, so logical row and physical line are distinct. **Release 0.1 has error-only diagnostics; there is no severity field or configurable warning level**.

No raw source value, credential, or full filesystem path is included in diagnostic messages by default. No opt-in API exposing arbitrary raw source values is part of release 0.1. Treat caller-provided source identifiers, worksheet names and field labels as potentially sensitive metadata.

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
| Zero bytes, explicitly headerless | Completed | ProcessedCount = AcceptedCount = RejectedCount = 0; zero validation errors | Informational: source contains no records |
| Zero bytes, required headers | Failed (source/schema) | ProcessedCount = AcceptedCount = RejectedCount = 0; a source/schema diagnostic is present | Error: required CSV header row is missing |
| Nonempty CSV missing a required column | Failed (schema binding) | Do not claim a successful import; counts reflect actual processed records | Error: identify the missing required column |

The messages are intended for humans and may be revised or localized. **A caller must be able to determine completion/failure from machine-readable result state and coded diagnostics without parsing message text.** An informational zero-row summary must not contribute to error or warning counts. A source/schema error must not be represented as a rejected data record that was never processed.

The external JSON report property names, required fields, and versioned status outcomes are implemented as specified in the [versioned JSON contract](import-result.md). The compiled public .NET API is captured in the [net48 compatibility baseline](../compatibility/README.md), and stable diagnostic codes/messages are implemented in Domain/Application resources. All release 0.1 distinctions above are covered by acceptance and regression evidence.

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

## Resolved release 0.1 decisions and future boundaries

- The compiled public API, stable diagnostic codes and JSON contract are verified in [compatibility](../compatibility/README.md) and [reporting tests](../../tests/RecordSift.Infrastructure.Reporting.Tests/).
- CsvHelper 33.1.0 and ExcelDataReader 3.9.0 dependencies and owner-approved MIT notices are recorded in the [dependency review](../release/0.1-dependencies.md).
- CSV/XLSX input limits, source failure behavior, and cooperative processing controls are implemented and covered by scenario-specific tests; callers must configure appropriate resource limits.
- Duplicate identifier equality defaults to the provided typed comparer or `EqualityComparer<TKey>.Default`; callers may explicitly configure an ordinal string comparer.
- Immutable constructor mapping, truly streaming/asynchronous imports, configurable warning severity and custom report formatting remain outside release 0.1.

The authoritative per-scenario execution status and CI evidence are maintained in [traceability](../testing/traceability.md); historical ADRs explain the decisions without overriding current release behavior.
