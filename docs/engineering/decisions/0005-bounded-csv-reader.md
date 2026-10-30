# ADR 0005 — Bounded raw CSV tokenization foundation

**Decision:** Implement a small deterministic comma-separated logical-record reader for FR-001, without prematurely defining the full typed CSV import API.

## Context

FR-001 needs source parsing that respects quoted commas, escaped quotes and embedded newlines. FR-004 owns header interpretation; FR-003 owns configurable delimiters; FR-006 owns byte decoding; FR-010/FR-026 own mapping and typed records. Those dependencies cannot be responsibly implemented by silently expanding FR-001's initial parser slice.

## Decision and contract

- `CsvRecordReader.Read(TextReader, maxCharacters)` parses already-decoded text, with a **mandatory positive caller-supplied character limit**. This avoids an unbounded read in the initial implementation; comprehensive NFR-007 limits and default policies remain separately tracked.
- Comma is the only delimiter for this FR-001 baseline. Quoted fields can contain commas, LF, CRLF or CR and doubled quote escapes. Quoted line breaks are preserved verbatim.
- A source record has immutable ordered fields, a one-based `SourceRecordNumber` (including a future header), and the one-based physical line on which it begins. It is **not** yet a logical data-row ordinal.
- No automatic header inference, header skipping, byte encoding, alternate delimiter choice, or model materialization is performed.
- Unclosed quotes, quotes inside unquoted fields and unexpected text after a closing quote are classified as `MALFORMED_SOURCE`. An IO exception becomes `SOURCE_UNREADABLE` and character-budget failure becomes `SOURCE_LIMIT_EXCEEDED`. They are fixed safe registry templates, without raw text, exception messages, paths or stack traces.
- On a parse failure `CsvParseResult` contains **no partially parsed records**, one source-scoped diagnostic and `IsSuccessful = false`. The importing layer must later determine imported-row counters and `ImportStatus` from actual processing.
- A zero-character source produces zero raw records and no parse errors; FR-004 will decide whether absence of headers is a fatal failure. Line terminators do not generate an extra phantom record at EOF.
- Blank physical lines are currently surfaced as raw single-empty-field records rather than silently discarded. Whitespace-only, blank-line-only and BOM behavior must be reviewed as part of integration; this parser does not claim the corresponding full-import behavior.
- The reader does not dispose the caller's `TextReader`. It has no new dependency, and runs on the existing net48/non-SDK baseline.

## Alternatives

- CSV parsing that infers headers: rejected, would silently discard first rows from headerless feeds.
- Custom delimiters/decoders in this issue: rejected; independently tracked FR-003 and FR-006.
- Treating malformed quoted fields as row validation errors: rejected; structural failures are source-scope.
- Unbounded `ReadAllText` and arbitrary output size: rejected for resource safety.
- Adding a typed generic importer now: rejected until FR-004, FR-010, FR-011 and FR-026 public-contract decisions are available.

## Evidence and remaining requirements

Component tests use in-memory `TextReader` instances to exercise FR-001-SC03 and SC04 parser behavior, negative/security cases and record locations. They **do not** establish end-to-end CSV import acceptance, source IDs, `ImportResult` reporting, typed-record materialization or zero-byte header-policy results. FR-001 remains **open** until all six scenarios can be verified or explicitly re-scoped. Traceability stays Planned at acceptance level.
