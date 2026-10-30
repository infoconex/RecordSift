# Record import behavioral specification (draft)

**Issue:** [#51](https://github.com/infoconex/RecordSift/issues/51)  
**Status:** Proposed contract; no implementation or passing tests exist yet.  
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

Dependencies, exact API signatures, field conversion edge cases, and default limits remain decisions for implementation issues; this document is not an assertion of production behavior.

## Source location

Diagnostics must identify a logical source ID, source format, logical record ordinal (one-based, excluding headers), field/column ID when applicable, error code, message, and severity. XLSX diagnostics additionally identify worksheet and physical row number where available. CSV diagnostics should include a physical line number only when reliably provided by the chosen parser, because a quoted multiline field may span multiple lines.

No raw source value, credential, or full filesystem path is included in diagnostic messages by default. Original raw values may be exposed through a deliberately separate, opt-in API after privacy review.

## Result invariants

- `Processed = Accepted + Rejected` for completed imports at the logical-record level.
- `ErrorCount` counts individual error diagnostics, not rejected rows.
- `Rejected` counts rows rejected for one or more errors; warnings alone do not reject a row.
- Accepted records preserve input order.
- Record-validation errors can be accumulated while continuing to process subsequent rows (FR-029).
- Source-level fatal errors cannot be misrepresented as a successful empty import.
- Report generation consumes the completed result; it must not re-read input.
- Identical input and explicit settings must give identically ordered diagnostics (NFR-004).

## Empty-source outcomes and messages

A zero-record outcome is **not** automatically a failed import. RecordSift must distinguish import completion from source/schema failure in structured result state, independently of message wording:

| Source and configuration | Outcome | Counts | Result message |
| --- | --- | --- | --- |
| Valid required headers, no data rows | Completed | Processed = Accepted = Rejected = 0; zero validation errors | Informational: no data records found |
| Zero bytes, explicitly headerless | Completed | Processed = Accepted = Rejected = 0; zero validation errors | Informational: source contains no records |
| Zero bytes, required headers | Failed (source/schema) | Processed = Accepted = Rejected = 0; a source/schema error is present | Error: required CSV header row is missing |
| Nonempty CSV missing a required column | Failed (schema binding) | Do not claim a successful import; counts reflect actual processed records | Error: identify the missing required column |

The messages are intended for humans and may be revised or localized. **A caller must be able to determine completion/failure from machine-readable result state and coded diagnostics without parsing message text.** An informational zero-row summary must not contribute to error or warning counts. A source/schema error must not be represented as a rejected data record that was never processed.

The exact public API member names, failure codes, and text localization policy are deferred to their implementation issues. The distinction and semantics above are requirements.

## Format-specific requirements

| Input | Requirements |
| --- | --- |
| CSV | FR-001, FR-003, FR-004, FR-006; delimiter, header policy, text encoding, quoted fields, and configured culture |
| XLSX | FR-002, FR-005; worksheet selection, correct source row references, no Excel installation |
| Common schema | FR-009–FR-013, FR-017–FR-022, FR-026–FR-030 |
| Reporting | FR-035–FR-037 |

The parser library's exceptions should be translated only where doing so improves a documented RecordSift error contract. Do not discard the original exception details from developer-level diagnostics.

## Example scenario (illustrative, not a test result)

Given a source file with three customer data records, where the second row has an invalid integer field, a completed continue-on-error import should have `Processed = 3`, `Accepted = 2`, `Rejected = 1`, and an error diagnostic referring to logical record 2 and that field.

## Open decisions

- Define public API names, models, and error-code registry in their implementation issues.
- Confirm exact supported reader package versions and licensing before pinning dependencies.
- Agree on file size, row count, worksheet limits, and behavior for errors while loading workbooks.
- Confirm JSON serialization contract with snapshot/characterization tests.
- Resolve whether duplicate matching is ordinal, case-insensitive, culture-aware, or configured by field.

Any resolved decision with broader architectural impact should be documented with an ADR, not silently inferred from an example.
